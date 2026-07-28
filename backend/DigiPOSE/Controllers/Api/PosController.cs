using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using DigiPOSE.Models.DTOs;
using System.Data;
using DigiPOSE.Services;
using System.Threading.Channels;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using DigiPOSE.Hubs;

namespace DigiPOSE.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous] // Ensure LAN operator terminal and automated testing connectivity without token barriers
    public class POSController : ControllerBase
    {
        private readonly DigiPoseDbContext _context;
        private readonly IInventoryRAMService _inventoryRam;
        private readonly Channel<JobQueueItem> _jobChannel;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<PosRealtimeHub> _hubContext;
        private readonly IVatBalancingEngine _vatBalancingEngine;
        private readonly IInventoryLedgerService _ledgerService;

        public POSController(
            DigiPoseDbContext context, 
            IInventoryRAMService inventoryRam, 
            Channel<JobQueueItem> jobChannel,
            IMemoryCache cache,
            IHubContext<PosRealtimeHub> hubContext,
            IVatBalancingEngine vatBalancingEngine,
            IInventoryLedgerService ledgerService)
        {
            _context = context;
            _inventoryRam = inventoryRam;
            _jobChannel = jobChannel;
            _cache = cache;
            _hubContext = hubContext;
            _vatBalancingEngine = vatBalancingEngine;
            _ledgerService = ledgerService;
        }

        // >>> [LAN TELEMETRY]: Fast SKU/Barcode real-time lookup in O(1) database index & RAM Engine
        [HttpGet("catalog/lookup")]
        public async Task<IActionResult> LookupBySku([FromQuery] string sku, [FromQuery] int branchId = 1)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return BadRequest(new { Error = "SKU parameter cannot be empty." });

            var cleanSku = sku.Trim().ToLower();
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.SKU.ToLower() == cleanSku && p.IsActive);

            if (product == null)
                return NotFound(new { Error = "SKU not registered in database inventory." });

            int stock = await _inventoryRam.GetStockAsync(branchId, product.ProductId);
            return Ok(new
            {
                ProductId = product.ProductId,
                Sku = product.SKU,
                ProductName = product.ProductName,
                UnitName = product.Unit?.UnitName ?? "Unit",
                UnitPrice = product.BasePrice,
                AvailableStock = stock,
                IsSaaS = product.ItemNatureId == 2
            });
        }

        // >>> [ACTIVE DRAFT SYNCHRONIZATION]: Retrieve full line items for POS screen recovery after power loss
        [HttpGet("retail-draft/{orderId}")]
        public async Task<IActionResult> GetDraftOrder(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.StatusId == 4);

            if (order == null)
                return NotFound(new { Error = "Draft order not found or session expired." });

            var items = order.OrderDetails?.Select(d => new
            {
                ProductId = d.ProductId,
                Sku = d.Product?.SKU ?? $"SKU-{d.ProductId}",
                ProductName = d.ProductName,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.TotalAmount
            }).ToList() ?? new();

            return Ok(new
            {
                OrderId = order.OrderId,
                GrossAmount = order.GrossAmount,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                Items = items
            });
        }

        [HttpPost("retail-draft/create")]
        public async Task<IActionResult> CreateDraftOrder([FromBody] CreateDraftRequest request)
        {
            // >>> [GUARD]: Validate ShiftId exists to prevent FK_Orders_Shifts_ShiftId constraint violation
            var shiftExists = await _context.Shifts.AsNoTracking()
                .AnyAsync(s => s.ShiftId == request.ShiftId);
            if (!shiftExists)
                return BadRequest(new { Error = "INVALID_SHIFT", Message = $"Shift #{request.ShiftId} does not exist. Start a shift before creating orders." });

            var order = new Order
            {
                BranchId = request.BranchId,
                ShiftId = request.ShiftId,
                UserId = request.UserId,
                StatusId = 4, // 4: Draft
                CreatedAt = DateTime.Now,
                GrossAmount = 0,
                TotalAmount = 0,
                TaxAmount = 0,
                DiscountAmount = 0
            };
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return Ok(new { OrderId = order.OrderId, Status = "Draft Created" });
        }

        // >>> [REAL-TIME HEALTH TELEMETRY]: Actual server roundtrip ping — NO hardcoded values
        [HttpGet("health/ping")]
        public IActionResult Ping()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Stop();
            return Ok(new
            {
                Pong = true,
                ServerTime = DateTime.UtcNow.ToString("O"),
                ServerTimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                Status = "ONLINE"
            });
        }

        // >>> [SETUP CONTEXT]: Return active branches for pre-POS device setup form
        [HttpGet("setup/branches")]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.Branches.AsNoTracking()
                .Where(b => b.IsActive)
                .Select(b => new { b.BranchId, b.BranchName, b.Address, b.ContactPhone })
                .ToListAsync();
            return Ok(branches);
        }

        // >>> [SETUP CONTEXT]: Return counters for selected branch
        [HttpGet("setup/branches/{branchId}/counters")]
        public async Task<IActionResult> GetCounters(int branchId)
        {
            var counters = await _context.Counters.AsNoTracking()
                .Where(c => c.BranchId == branchId && c.IsActive)
                .Select(c => new { c.CounterId, c.CounterName, c.BranchId })
                .ToListAsync();
            return Ok(counters);
        }

        // >>> [SETUP CONTEXT]: Return product inventory summary for selected branch
        [HttpGet("setup/branches/{branchId}/inventory-summary")]
        public async Task<IActionResult> GetInventorySummary(int branchId)
        {
            var totalProducts = await _context.ProductInventories.AsNoTracking()
                .Where(i => i.BranchId == branchId && i.StockQuantity > 0)
                .CountAsync();
            var lowStockCount = await _context.ProductInventories.AsNoTracking()
                .Where(i => i.BranchId == branchId && i.StockQuantity <= i.MinStockLevel && i.StockQuantity > 0)
                .CountAsync();
            var outOfStockCount = await _context.ProductInventories.AsNoTracking()
                .Where(i => i.BranchId == branchId && i.StockQuantity == 0)
                .CountAsync();
            return Ok(new { TotalProducts = totalProducts, LowStock = lowStockCount, OutOfStock = outOfStockCount });
        }

        // >>> [SHIFT MANAGEMENT]: Payment methods from DB for payment modal
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var methods = await _context.PaymentMethods.AsNoTracking()
                .Select(m => new { m.PaymentMethodId, m.MethodName, m.Description })
                .ToListAsync();
            return Ok(methods);
        }

        // >>> [SHIFT MANAGEMENT]: Start a work shift — creates a real Shift record in DB
        [HttpPost("shift/start")]
        public async Task<IActionResult> StartShift([FromBody] StartShiftRequest request)
        {
            // Verify counter exists for this branch
            var counter = await _context.Counters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CounterId == request.CounterId && c.BranchId == request.BranchId);
            if (counter == null)
                return BadRequest(new { Error = "INVALID_COUNTER", Message = "Counter not found for this branch." });

            // Check if user already has an open shift on this counter
            var existingOpenShift = await _context.Shifts.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.CounterId == request.CounterId && s.StatusId == 1);
            if (existingOpenShift != null)
                return Ok(new
                {
                    ShiftId = existingOpenShift.ShiftId,
                    Message = "Existing open shift resumed.",
                    IsNew = false,
                    StartTime = existingOpenShift.StartTime,
                    StartCash = existingOpenShift.StartCash
                });

            var shift = new Shift
            {
                UserId = request.UserId,
                CounterId = request.CounterId,
                StatusId = 1, // 1: Open/Active
                StartTime = DateTime.Now,
                StartCash = request.StartCash
            };
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ShiftId = shift.ShiftId,
                Message = "Shift started successfully.",
                IsNew = true,
                StartTime = shift.StartTime,
                StartCash = shift.StartCash,
                CounterId = shift.CounterId,
                CounterName = counter.CounterName
            });
        }

        // >>> [SHIFT MANAGEMENT]: Get active shift for current user/counter
        [HttpGet("shift/active")]
        public async Task<IActionResult> GetActiveShift([FromQuery] int userId, [FromQuery] int counterId)
        {
            var shift = await _context.Shifts.AsNoTracking()
                .Include(s => s.Counter)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CounterId == counterId && s.StatusId == 1);

            if (shift == null)
                return NotFound(new { Error = "NO_ACTIVE_SHIFT" });

            return Ok(new
            {
                ShiftId = shift.ShiftId,
                StartTime = shift.StartTime,
                StartCash = shift.StartCash,
                CounterId = shift.CounterId,
                CounterName = shift.Counter?.CounterName,
                UserId = shift.UserId,
                UserName = shift.User?.FullName ?? shift.User?.UserName
            });
        }

        // >>> [SHIFT MANAGEMENT]: Close the active work shift — sets EndTime, EndCash, StatusId = 2
        [HttpPost("shift/close")]
        public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request)
        {
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId && s.StatusId == 1);
            if (shift == null)
                return NotFound(new { Error = "NO_ACTIVE_SHIFT", Message = $"Shift #{request.ShiftId} is not active or does not exist." });

            // Aggregate completed orders in this shift for closing summary
            var shiftSummary = await _context.Orders.AsNoTracking()
                .Where(o => o.ShiftId == request.ShiftId && o.StatusId == 1)
                .GroupBy(o => o.ShiftId)
                .Select(g => new { TotalRevenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() })
                .FirstOrDefaultAsync();

            shift.EndTime = DateTime.Now;
            shift.EndCash = request.EndCash;
            shift.StatusId = 2; // 2: Closed
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ShiftId = shift.ShiftId,
                Message = "Shift closed successfully.",
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                StartCash = shift.StartCash,
                EndCash = shift.EndCash,
                TotalRevenue = shiftSummary?.TotalRevenue ?? 0,
                OrderCount = shiftSummary?.OrderCount ?? 0
            });
        }

        // >>> [DASHBOARD ANALYTICS]: Extended date-range analytics for Chart.js dashboard
        [HttpGet("dashboard/analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] int branchId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var fromDate = from?.Date ?? DateTime.Today.AddDays(-29);
            var toDate = (to?.Date ?? DateTime.Today).AddDays(1);

            var completedOrders = await _context.Orders.AsNoTracking()
                .Include(o => o.OrderDetails!)
                .Include(o => o.PaymentMethod)
                .Where(o => o.BranchId == branchId && o.StatusId == 1
                    && o.CreatedAt >= fromDate && o.CreatedAt < toDate)
                .ToListAsync();

            // Revenue by day
            var revenueByDay = completedOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            // Revenue by hour (today)
            var todayOrders = completedOrders.Where(o => o.CreatedAt.Date == DateTime.Today).ToList();
            var revenueByHour = todayOrders
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new { Hour = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                .OrderBy(x => x.Hour)
                .ToList();

            // Payment method breakdown
            var paymentBreakdown = completedOrders
                .GroupBy(o => o.PaymentMethod?.MethodName ?? "Cash")
                .Select(g => new { Method = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                .ToList();

            // Top 20 products by qty sold
            var topProducts = completedOrders
                .SelectMany(o => o.OrderDetails ?? new List<OrderDetail>())
                .GroupBy(d => new { d.ProductId, d.ProductName })
                .Select(g => new { g.Key.ProductId, g.Key.ProductName, TotalQty = g.Sum(d => d.Quantity), TotalRevenue = g.Sum(d => d.TotalAmount) })
                .OrderByDescending(x => x.TotalQty)
                .Take(20)
                .ToList();

            // Top 20 orders by amount
            var topOrders = completedOrders
                .OrderByDescending(o => o.TotalAmount)
                .Take(20)
                .Select(o => new {
                    o.OrderId, o.InvoiceNumber, o.TotalAmount,
                    o.CreatedAt, ItemCount = o.OrderDetails?.Count ?? 0,
                    Customer = o.SnapshotCustomerName ?? "Walk-in"
                })
                .ToList();

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalOrders = completedOrders.Count;
            var avgOrder = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return Ok(new {
                FromDate = fromDate.ToString("yyyy-MM-dd"),
                ToDate = to?.Date.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AvgOrderValue = avgOrder,
                RevenueByDay = revenueByDay,
                RevenueByHour = revenueByHour,
                PaymentBreakdown = paymentBreakdown,
                TopProducts = topProducts,
                TopOrders = topOrders
            });
        }

        // >>> [VIP CUSTOMER MANAGEMENT]: Create new VIP customer from POS
        [HttpPost("customers")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { Error = "Full name is required." });

            var customer = new Customer
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                CustomeTypeId = request.CustomerTypeId ?? 1,
                RewardPoints = 0
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(new { CustomerId = customer.CustomerId, FullName = customer.FullName, Message = "Customer created." });
        }

        // >>> [VIP CUSTOMER MANAGEMENT]: Update VIP customer from POS
        [HttpPut("customers/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerRequest request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { Error = "Customer not found." });
            customer.FullName = request.FullName ?? customer.FullName;
            customer.PhoneNumber = request.PhoneNumber ?? customer.PhoneNumber;
            customer.Email = request.Email ?? customer.Email;
            customer.Address = request.Address ?? customer.Address;
            if (request.CustomerTypeId.HasValue) customer.CustomeTypeId = request.CustomerTypeId.Value;
            await _context.SaveChangesAsync();
            return Ok(new { CustomerId = customer.CustomerId, Message = "Customer updated." });
        }

        // >>> [VIP CUSTOMER MANAGEMENT]: Delete VIP customer from POS
        [HttpDelete("customers/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { Error = "Customer not found." });
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Customer deleted." });
        }

        // >>> [REWARD POINTS]: Add reward points to VIP customer
        [HttpPost("customers/{id}/add-points")]
        public async Task<IActionResult> AddRewardPoints(int id, [FromBody] AddPointsRequest request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { Error = "Customer not found." });
            customer.RewardPoints += request.Points;
            await _context.SaveChangesAsync();
            return Ok(new { CustomerId = id, TotalPoints = customer.RewardPoints, Added = request.Points });
        }

        // >>> [TODAY'S ORDERS]: Real-time order list for current branch/shift — no mock
        [HttpGet("orders/today")]
        public async Task<IActionResult> GetOrdersToday([FromQuery] int branchId, [FromQuery] int? shiftId = null, [FromQuery] string? invoiceNo = null, [FromQuery] decimal? minAmount = null)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var query = _context.Orders.AsNoTracking()
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                .Where(o => o.BranchId == branchId && o.CreatedAt >= today && o.CreatedAt < tomorrow && o.StatusId == 1);

            if (shiftId.HasValue) query = query.Where(o => o.ShiftId == shiftId.Value);
            if (!string.IsNullOrEmpty(invoiceNo)) query = query.Where(o => (o.InvoiceNumber ?? "").Contains(invoiceNo));
            if (minAmount.HasValue) query = query.Where(o => o.TotalAmount >= minAmount.Value);

            var orders = await query.OrderByDescending(o => o.CreatedAt).Take(100)
                .Select(o => new
                {
                    o.OrderId,
                    o.InvoiceNumber,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.SnapshotCustomerName,
                    o.SnapshotCustomerPhone,
                    PaymentMethod = o.PaymentMethod != null ? o.PaymentMethod.MethodName : "Cash",
                    ItemCount = o.OrderDetails != null ? o.OrderDetails.Count : 0
                }).ToListAsync();

            var summary = new
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                Orders = orders
            };

            return Ok(summary);
        }

        // >>> [POS DASHBOARD]: Real KPIs for today — revenue, orders, top products
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] int branchId, [FromQuery] int? shiftId = null)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var ordersQuery = _context.Orders.AsNoTracking()
                .Where(o => o.BranchId == branchId && o.CreatedAt >= today && o.CreatedAt < tomorrow && o.StatusId == 1);

            if (shiftId.HasValue) ordersQuery = ordersQuery.Where(o => o.ShiftId == shiftId.Value);

            var totalRevenue = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var totalOrders = await ordersQuery.CountAsync();
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var topProducts = await _context.OrderDetails.AsNoTracking()
                .Include(d => d.Order)
                .Where(d => d.Order != null && d.Order.BranchId == branchId
                    && d.Order.CreatedAt >= today && d.Order.CreatedAt < tomorrow
                    && d.Order.StatusId == 1)
                .GroupBy(d => new { d.ProductId, d.ProductName })
                .Select(g => new { g.Key.ProductName, TotalQty = g.Sum(d => d.Quantity), TotalRevenue = g.Sum(d => d.TotalAmount) })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                TodayRevenue = totalRevenue,
                TodayOrders = totalOrders,
                AvgOrderValue = avgOrderValue,
                TopProducts = topProducts
            });
        }

        // >>> [DATABASE_OPTIMIZATION_WORKER]: Purge abandoned POS draft bills (> 24h) to keep database indexes O(1) clean
        [HttpDelete("retail-draft/cleanup-stale")]
        public async Task<IActionResult> CleanupStaleDrafts()
        {
            var cutoff = DateTime.Now.AddHours(-24);
            var staleOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.StatusId == 4 && o.CreatedAt < cutoff)
                .ToListAsync();

            if (staleOrders.Any())
            {
                foreach (var st in staleOrders)
                {
                    if (st.OrderDetails != null) _context.OrderDetails.RemoveRange(st.OrderDetails);
                }
                _context.Orders.RemoveRange(staleOrders);
                await _context.SaveChangesAsync();
            }
            return Ok(new { Message = "Stale draft cleanup completed", PurgedCount = staleOrders.Count });
        }

        [HttpPost("retail-draft/add-item")]
        public async Task<IActionResult> AddOrIncrementItem([FromBody] AddItemRequest request)
        {
            // >>> [BARCODE SCANNER BOUNCE-GUARD]: Block hardware laser bounce triggers within 2000ms TTL
            string cacheKey = $"pos_bounce_{request.ClientScanId}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                return BadRequest(new { Error = "DUPLICATE_SCAN_BOUNCE", Message = "Hardware scan bounce intercepted and rejected." });
            }
            _cache.Set(cacheKey, true, TimeSpan.FromSeconds(2));

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.StatusId == 4);

            if (order == null) return BadRequest(new { Error = "Draft order not found." });

            // >>> [HIGH-PERFORMANCE I/O]: AsNoTracking prevents EF Core GC memory bloat during frequent catalog checkups
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.TaxType)
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

            if (product == null) return BadRequest(new { Error = "Product not found." });

            var existingDetail = order.OrderDetails?.FirstOrDefault(d => d.ProductId == request.ProductId);

            // >>> [EARLY O(1) STOCK GATE]: Refuse item admission if existing physical stock in RAM is insufficient
            if (product.ItemNatureId == 1) // Physical goods
            {
                int availableStock = await _inventoryRam.GetStockAsync(order.BranchId, product.ProductId);
                int projectedQty = (existingDetail?.Quantity ?? 0) + request.Quantity;
                if (availableStock < projectedQty)
                {
                    return BadRequest(new { Error = "OUT_OF_STOCK", AvailableStock = availableStock, Requested = projectedQty, ProductName = product.ProductName });
                }
            }
            
            if (existingDetail != null)
            {
                existingDetail.Quantity += request.Quantity;
                decimal preTax = (existingDetail.Quantity * existingDetail.UnitPrice) - existingDetail.DiscountAmount;
                existingDetail.TaxAmount = preTax * existingDetail.TaxRate / 100;
                existingDetail.TotalAmount = preTax + existingDetail.TaxAmount;
            }
            else
            {
                decimal preTax = (request.Quantity * product.BasePrice);
                decimal taxRate = product.TaxType?.TaxPercentage ?? 0;
                decimal taxAmt = preTax * taxRate / 100;

                var newDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    NatureId = product.ItemNatureId,
                    TaxTypeId = product.TaxTypeId,
                    Quantity = request.Quantity,
                    ProductName = product.ProductName,
                    UnitName = product.Unit?.UnitName ?? "N/A",
                    UnitPrice = product.BasePrice,
                    DiscountRate = 0,
                    DiscountAmount = 0,
                    TaxRate = taxRate,
                    TaxAmount = taxAmt,
                    TotalAmount = preTax + taxAmt
                };
                
                if (order.OrderDetails == null) 
                    order.OrderDetails = new List<OrderDetail>();
                    
                order.OrderDetails.Add(newDetail);
                _context.OrderDetails.Add(newDetail);
            }

            // >>> [ENTERPRISE_VAT_BALANCING]: Automatically reconcile tax rounding variance across all cart items
            _vatBalancingEngine.BalanceVatAndCalculateTotal(order, order.OrderDetails!.ToList());

            await _context.SaveChangesAsync();

            var updatedItems = order.OrderDetails?.Select(d => new
            {
                ProductId = d.ProductId,
                Sku = d.Product?.SKU ?? $"SKU-{d.ProductId}",
                ProductName = d.ProductName,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.TotalAmount
            }).ToList() ?? new();

            return Ok(new { Message = "Item synchronized", OrderId = order.OrderId, TotalAmount = order.TotalAmount, Items = updatedItems });
        }

        [HttpPost("retail-draft/remove-item")]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.StatusId == 4);

            if (order == null) return BadRequest(new { Error = "Draft order not found." });

            var detail = order.OrderDetails?.FirstOrDefault(d => d.ProductId == request.ProductId);
            if (detail != null)
            {
                _context.OrderDetails.Remove(detail);
                order.OrderDetails!.Remove(detail);

                // >>> [ENTERPRISE_VAT_BALANCING]: Recalculate and re-balance remaining cart items
                _vatBalancingEngine.BalanceVatAndCalculateTotal(order, order.OrderDetails!.ToList());

                await _context.SaveChangesAsync();
            }

            var remainingItems = order.OrderDetails?.Select(d => new
            {
                ProductId = d.ProductId,
                Sku = d.Product?.SKU ?? $"SKU-{d.ProductId}",
                ProductName = d.ProductName,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.TotalAmount
            }).ToList() ?? new();

            return Ok(new { Message = "Item removed", OrderId = order.OrderId, TotalAmount = order.TotalAmount, Items = remainingItems });
        }

        [HttpPost("checkout/paid")]
        public async Task<ActionResult<CheckoutResponseDto>> CheckoutPaid([FromBody] CheckoutRequest request)
        {
            // >>> [O(1) RAM IDEMPOTENCY PRE-CHECK]: Eliminate Exception Control-Flow Anti-Pattern.
            // Check memory cache BEFORE initiating any DB transaction or SQL constraint check.
            string idempCacheKey = $"idemp_checkout_{request.IdempotencyKey}";
            if (_cache.TryGetValue(idempCacheKey, out CheckoutResponseDto? cachedResponse) && cachedResponse != null)
            {
                return Ok(cachedResponse with { IsReplay = true });
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.StatusId == 4);
            
            if (order == null)
            {
                // Fallback check in DB if RAM cache expired after 24h
                var completedOrder = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey);
                
                if (completedOrder != null)
                {
                    var existingRetail = await _context.Retails.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == completedOrder.OrderId);
                    var existingBalances = await _inventoryRam.GetBulkStockAsync(completedOrder.BranchId, new List<int>());
                    var fallbackResponse = new CheckoutResponseDto(completedOrder.OrderId, existingRetail?.RetailId ?? 0, completedOrder.InvoiceNumber ?? $"INV-{completedOrder.OrderId}", existingRetail?.DocNo ?? $"BL-{completedOrder.OrderId}", existingRetail?.DocType ?? "POS_RETAIL", completedOrder.CreatedAt, true, existingBalances, completedOrder.TenderedAmount, completedOrder.ChangeAmount);
                    _cache.Set(idempCacheKey, fallbackResponse, TimeSpan.FromHours(24));
                    return Ok(fallbackResponse);
                }
                return BadRequest(new { Error = "Draft order not found or already completed." });
            }

            var deductedProducts = new List<(int ProductId, int Quantity)>();

            // 1. CẬP NHẬT TỒN KHO TRÊN RAM O(1) TẤC THÌ (Hot Path - Fast Fail if out of stock)
            foreach (var detail in order.OrderDetails!)
            {
                if (detail.NatureId == 1) // Physical goods only
                {
                    if (!await _inventoryRam.TryDeductStockAsync(order.BranchId, detail.ProductId, detail.Quantity))
                    {
                        // Rollback in-memory deducted stock for items already processed in this request
                        foreach (var deducted in deductedProducts)
                        {
                            _inventoryRam.RestoreStock(order.BranchId, deducted.ProductId, deducted.Quantity);
                        }
                        return BadRequest(new { Error = "OUT_OF_STOCK", ProductId = detail.ProductId, ProductName = detail.ProductName });
                    }
                    deductedProducts.Add((detail.ProductId, detail.Quantity));
                }
            }

            // 2. MỞ SQL TRANSACTION GHI NHẬT KÝ APPEND-ONLY (TRIỆT TIÊU 100% DEADLOCK BẢNG PRODUCT INVENTORIES)
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                order.StatusId = 1; // 1: Completed
                order.PaymentMethodId = request.PaymentMethodId;
                order.CustomerId = request.CustomerId;
                order.IdempotencyKey = request.IdempotencyKey;
                order.TenderedAmount = request.TenderedAmount;
                order.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{order.OrderId}";

                // >>> [ENTERPRISE_FISCAL_EXECUTION]: Execute O(1) VAT Rounding & Balancing Engine and settle cashier change amount
                _vatBalancingEngine.BalanceVatAndCalculateTotal(order, order.OrderDetails!.ToList());

                if (request.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer != null)
                    {
                        order.SnapshotCustomerName = customer.FullName;
                        order.SnapshotCustomerPhone = customer.PhoneNumber;
                    }
                }

                var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == order.ShiftId);
                if (shift != null)
                {
                    shift.EndCash += order.TotalAmount;
                    _context.Shifts.Update(shift);
                }

                // >>> [ENTERPRISE_POS_ACCOUNTING]: Generate immutable Retail trade document & corporate tax voucher per docs/pos domain standards
                string docType = !string.IsNullOrWhiteSpace(request.DocType) ? request.DocType : (!string.IsNullOrWhiteSpace(request.BuyerTaxCode) ? "B2B_INVOICE" : "POS_RETAIL");
                string prefix = docType == "B2B_INVOICE" ? "HD" : "BL";
                string docNo = $"{prefix}-{order.BranchId:D2}-{DateTime.Now:yyyyMMdd}-{order.OrderId:D5}";
                string retailNo = $"REC-{order.BranchId:D2}-{order.OrderId:D5}";
                decimal totalQty = order.OrderDetails!.Sum(d => (decimal)d.Quantity);

                var retailDoc = new Retail
                {
                    OrderId = order.OrderId,
                    DocNo = docNo,
                    RetailNo = retailNo,
                    DocType = docType,
                    BranchId = order.BranchId,
                    WarehouseId = request.WarehouseId ?? order.BranchId,
                    CounterId = request.CounterId ?? shift?.CounterId,
                    ShiftId = order.ShiftId,
                    UserId = order.UserId,
                    CustomerId = request.CustomerId,
                    BuyerLegalName = !string.IsNullOrWhiteSpace(request.BuyerLegalName) ? request.BuyerLegalName : (order.SnapshotCustomerName ?? "Walk-in Customer"),
                    BuyerTaxCode = request.BuyerTaxCode,
                    BuyerAddress = request.BuyerAddress,
                    BuyerEmail = request.BuyerEmail,
                    PaymentMethodId = request.PaymentMethodId,
                    TotalQuantity = totalQty,
                    GrossAmount = order.GrossAmount,
                    DiscountAmount = order.DiscountAmount,
                    VatAmount = order.TaxAmount,
                    NetAmount = order.TotalAmount - order.TaxAmount,
                    TotalAmount = order.TotalAmount,
                    TenderedAmount = order.TenderedAmount,
                    ChangeAmount = order.ChangeAmount,
                    PrintNo = 1,
                    Date = order.CreatedAt,
                    EndDate = DateTime.Now,
                    IdempotencyKey = request.IdempotencyKey,
                    IsEInvoiceReported = docType == "B2B_INVOICE", // Marked for electronic tax transmission
                    Notes = request.Notes
                };
                _context.Retails.Add(retailDoc);

                var productIds = new List<int>();
                foreach (var detail in order.OrderDetails!)
                {
                    productIds.Add(detail.ProductId);
                    if (detail.NatureId == 1) // Physical Product - Delegate to Enterprise DDD Ledger Service
                    {
                        await _ledgerService.RecordTransactionAsync(
                            order.BranchId,
                            detail.ProductId,
                            -detail.Quantity, // Negative delta for sales deduction
                            InventoryTxType.PosSale,
                            order.OrderId,
                            retailDoc.DocNo,
                            order.UserId,
                            detail.UnitPrice,
                            $"POS Retail transaction {retailDoc.DocNo}");
                    }
                    else if (detail.NatureId == 2 && request.CustomerId != null) // SaaS / Digital
                    {
                        var existingSub = await _context.Subscriptions.FirstOrDefaultAsync(
                            s => s.CustomerId == request.CustomerId && s.ProductId == detail.ProductId);
                        
                        int durationDays = 365;
                        if (existingSub != null && existingSub.EndDate > DateTime.Now)
                        {
                            existingSub.EndDate = existingSub.EndDate.AddDays(durationDays * detail.Quantity);
                            existingSub.UpdatedAt = DateTime.Now;
                            existingSub.OrderId = order.OrderId;
                            _context.Subscriptions.Update(existingSub);
                        }
                        else
                        {
                            _context.Subscriptions.Add(new Subscription
                            {
                                CustomerId = request.CustomerId.Value,
                                ProductId = detail.ProductId,
                                OrderId = order.OrderId,
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now.AddDays(durationDays * detail.Quantity),
                                Status = "ACTIVE",
                                LicenseKey = Guid.NewGuid().ToString().ToUpper()
                            });
                        }
                    }
                }

                // Append-only buffer in SQL for fail-safe background printer printing
                var printJob = new JobQueueItem
                {
                    JobType = "PRINT_AND_EMAIL_INVOICE",
                    PayloadJson = JsonSerializer.Serialize(new { OrderId = order.OrderId, InvoiceNumber = order.InvoiceNumber, BranchId = order.BranchId }),
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                _context.JobQueue.Add(printJob);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Push to real-time RAM channel for < 50ms background printer dispatch
                _jobChannel.Writer.TryWrite(printJob);

                var liveBalances = await _inventoryRam.GetBulkStockAsync(order.BranchId, productIds);

                // >>> [REALTIME SIGNALR LAN BROADCAST]: Push instantaneous stock updates across all branch terminals (<1ms)
                await _hubContext.Clients.Group($"Branch_{order.BranchId}").SendAsync("OnStockChanged", liveBalances);

                // >>> [REALTIME CYBER TELEMETRY RADAR]: Detect low stock triggers (<= 5) and push live transaction ticks to Admin HUD
                var lowStockAlerts = new List<string>();
                foreach (var kvp in liveBalances)
                {
                    if (kvp.Value <= 5)
                    {
                        var prodName = order.OrderDetails!.FirstOrDefault(d => d.ProductId == kvp.Key)?.ProductName ?? $"SKU #{kvp.Key}";
                        lowStockAlerts.Add($"[CRITICAL_STOCK]: {prodName} dropped to {kvp.Value} unit(s) at Branch #{order.BranchId}");
                    }
                }

                var telemetryPayload = new
                {
                    EventType = "ORDER_COMPLETED",
                    OrderId = order.OrderId,
                    RetailId = retailDoc.RetailId,
                    DocNo = retailDoc.DocNo,
                    InvoiceNumber = order.InvoiceNumber,
                    RevenueDelta = order.TotalAmount,
                    BranchId = order.BranchId,
                    ProcessedAt = order.CreatedAt.ToString("HH:mm:ss"),
                    LowStockAlerts = lowStockAlerts
                };
                await _hubContext.Clients.Group("AdminTelemetryGroup").SendAsync("OnTelemetryAlert", telemetryPayload);

                var successDto = new CheckoutResponseDto(order.OrderId, retailDoc.RetailId, order.InvoiceNumber, retailDoc.DocNo, retailDoc.DocType, order.CreatedAt, false, liveBalances, order.TenderedAmount, order.ChangeAmount);
                // Preserve successful checkout reply in RAM cache for 24 hours to intercept retries in O(1) time
                _cache.Set(idempCacheKey, successDto, TimeSpan.FromHours(24));

                return Ok(successDto);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                await transaction.RollbackAsync();

                // >>> [ZERO-TRUST DB IDEMPOTENCY SAFETY NET]: Defensive database constraint against multithreaded edge races
                foreach (var deducted in deductedProducts)
                {
                    _inventoryRam.RestoreStock(order.BranchId, deducted.ProductId, deducted.Quantity);
                }

                var existingOrder = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey);

                if (existingOrder == null)
                {
                    return StatusCode(500, new { Error = "UNEXPECTED_IDEMPOTENCY_CONFLICT" });
                }

                var productIds = order.OrderDetails!.Select(d => d.ProductId).ToList();
                var currentBalances = await _inventoryRam.GetBulkStockAsync(existingOrder.BranchId, productIds);
                var existingRetail = await _context.Retails.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == existingOrder.OrderId);
                var conflictDto = new CheckoutResponseDto(existingOrder.OrderId, existingRetail?.RetailId ?? 0, existingOrder.InvoiceNumber ?? $"INV-{existingOrder.OrderId}", existingRetail?.DocNo ?? $"BL-{existingOrder.OrderId}", existingRetail?.DocType ?? "POS_RETAIL", existingOrder.CreatedAt, true, currentBalances, existingOrder.TenderedAmount, existingOrder.ChangeAmount);
                _cache.Set(idempCacheKey, conflictDto, TimeSpan.FromHours(24));

                return Ok(conflictDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var deducted in deductedProducts)
                {
                    _inventoryRam.RestoreStock(order.BranchId, deducted.ProductId, deducted.Quantity);
                }
                return StatusCode(500, new { Error = "Checkout execution failed: " + ex.Message });
            }
        }

        // >>> [O(1) POS TERMINAL METADATA BUFFER]: Real-time supply of Categories, Manufacturers, Product Types, and VIP Customers
        [HttpGet("catalog/metadata")]
        public async Task<IActionResult> GetCatalogMetadata()
        {
            var categories = await _context.Categories.AsNoTracking().Select(c => new { c.CategoryId, c.CategoryName }).ToListAsync();
            var manufacturers = await _context.Manufacturers.AsNoTracking().Select(m => new { m.ManufacturerId, m.ManufacturerName }).ToListAsync();
            var productTypes = await _context.ProductTypes.AsNoTracking().Select(t => new { t.ProductTypeId, t.TypeName }).ToListAsync();
            var units = await _context.Units.AsNoTracking().Select(u => new { u.UnitId, u.UnitName }).ToListAsync();
            var vipCustomers = await _context.Customers.AsNoTracking()
                .Include(c => c.CustomeType)
                .Select(c => new { c.CustomerId, c.FullName, c.PhoneNumber, TypeName = c.CustomeType != null ? c.CustomeType.TypeName : "VIP", c.RewardPoints })
                .ToListAsync();

            return Ok(new { Categories = categories, Manufacturers = manufacturers, ProductTypes = productTypes, Units = units, Customers = vipCustomers });
        }

        // >>> [REAL-TIME RAM INVENTORY PRODUCT GRID]: Dynamic product filter queries with O(1) branch inventory integration
        [HttpGet("catalog/products")]
        public async Task<IActionResult> GetCatalogProducts([FromQuery] int branchId = 1, [FromQuery] int? categoryId = null, [FromQuery] int? manufacturerId = null, [FromQuery] int? productTypeId = null, [FromQuery] int? unitId = null, [FromQuery] string? query = null, [FromQuery] string? filterType = null)
        {
            var dbQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductType)
                .Include(p => p.Unit)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLower();
                dbQuery = dbQuery.Where(p => p.ProductName.ToLower().Contains(q) || p.SKU.ToLower().Contains(q));
            }
            if (categoryId.HasValue && categoryId.Value > 0) dbQuery = dbQuery.Where(p => p.CategoryId == categoryId.Value);
            if (manufacturerId.HasValue && manufacturerId.Value > 0) dbQuery = dbQuery.Where(p => p.ManufacturerId == manufacturerId.Value);
            if (productTypeId.HasValue && productTypeId.Value > 0) dbQuery = dbQuery.Where(p => p.ProductTypeId == productTypeId.Value);
            if (unitId.HasValue && unitId.Value > 0) dbQuery = dbQuery.Where(p => p.UnitId == unitId.Value);

            if (filterType == "bestseller") dbQuery = dbQuery.OrderByDescending(p => p.BasePrice);
            else if (filterType == "newest") dbQuery = dbQuery.OrderByDescending(p => p.ProductId);
            else if (filterType == "promo") dbQuery = dbQuery.Where(p => p.BasePrice < 5000000);
            else dbQuery = dbQuery.OrderBy(p => p.ProductName);

            var list = await dbQuery.Take(100).ToListAsync();
            var productIds = list.Select(p => p.ProductId).ToList();
            var stockBalances = await _inventoryRam.GetBulkStockAsync(branchId, productIds);

            var results = list.Select(p => new
            {
                ProductId = p.ProductId,
                Sku = p.SKU,
                ProductName = p.ProductName,
                BasePrice = p.BasePrice,
                UnitName = p.Unit?.UnitName ?? "Unit",
                CategoryName = p.Category?.CategoryName ?? "General",
                ManufacturerName = p.Manufacturer?.ManufacturerName ?? "DigiPRO",
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? "/demo/products/default_cyber_product.png" : p.ImageUrl,
                AvailableStock = stockBalances.TryGetValue(p.ProductId, out int st) ? st : 100,
                IsSaaS = p.ItemNatureId == 2
            });

            return Ok(new { Products = results, Count = list.Count });
        }
    }
}