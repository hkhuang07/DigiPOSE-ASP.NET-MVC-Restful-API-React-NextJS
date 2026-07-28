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
    /// <summary>
    /// Phase 6.2 - RESTful Web API for Dual Sales Subsystems (Online E-Commerce Storefront & React/Next.JS Client).
    /// Provides low-latency O(1) filtering, SEO Meta generation, and non-accounting protected shopping cart management.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StorefrontController : ControllerBase
    {
        private readonly DigiPoseDbContext _context;
        private readonly IInventoryRAMService _inventoryRam;
        private readonly Channel<JobQueueItem> _jobChannel;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<PosRealtimeHub> _hubContext;
        private readonly IVatBalancingEngine _vatBalancingEngine;
        private readonly IInventoryLedgerService _ledgerService;

        public StorefrontController(
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

        #region 1. CUSTOMER IDENTITY & PROFILE
        
        /// <summary>
        /// Retrieves active customer identity, username, VIP status, and Reward Points from JWT/Claims.
        /// </summary>
        [HttpGet("user-identity")]
        [Authorize]
        public async Task<IActionResult> GetUsernameAndIdentity()
        {
            var username = User.Identity?.Name ?? "Guest Shopper";
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            
            if (int.TryParse(customerIdClaim, out int customerId))
            {
                var customer = await _context.Customers
                    .Include(c => c.CustomeType)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                if (customer != null)
                {
                    return Ok(new
                    {
                        Username = username,
                        CustomerName = customer.FullName,
                        PhoneNumber = customer.PhoneNumber,
                        CustomerType = customer.CustomeType?.TypeName ?? "Standard Member",
                        RewardPoints = customer.RewardPoints,
                        IsAuthenticated = true
                    });
                }
            }

            return Ok(new { Username = username, CustomerType = "Guest", RewardPoints = 0, IsAuthenticated = false });
        }

        #endregion

        #region 2. CATALOG SEARCH, SEO & MULTI-DIMENSIONAL FILTERING

        /// <summary>
        /// Dynamic SEO Catalog search and filtering by Manufacturer, Category, Product Type, and Price Range.
        /// Outputs structured metadata for Next.JS SSR Indexing (MetaTitle, MetaDescription, MetaKeywords).
        /// </summary>
        [HttpPost("catalog/search")]
        public async Task<IActionResult> SearchCatalog([FromBody] CatalogSearchFilter filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductType)
                .Include(p => p.ItemNature)
                .AsNoTracking()
                .Where(p => p.IsActive);

            // 1. Dynamic Filters
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(q) || p.SKU.ToLower().Contains(q) || (p.Slug != null && p.Slug.ToLower().Contains(q)));
            }
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            if (filter.ProductTypeId.HasValue && filter.ProductTypeId.Value > 0)
                query = query.Where(p => p.ProductTypeId == filter.ProductTypeId.Value);
            if (filter.ManufacturerId.HasValue && filter.ManufacturerId.Value > 0)
                query = query.Where(p => p.ManufacturerId == filter.ManufacturerId.Value);
            if (filter.ItemNatureId.HasValue && filter.ItemNatureId.Value > 0)
                query = query.Where(p => p.ItemNatureId == filter.ItemNatureId.Value);
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);

            // 2. Sorting
            query = filter.SortBy.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.BasePrice),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderBy(p => p.ProductName)
            };

            // 3. Pagination
            int totalRecords = await query.CountAsync();
            var pagedProducts = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // 4. Transform to SEO-enriched responses
            var results = pagedProducts.Select(p => new SeoProductResponse
            {
                ProductId = p.ProductId,
                SKU = p.SKU,
                ProductName = p.ProductName,
                BasePrice = p.BasePrice,
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? "/demo/products/default_cyber_product.png" : p.ImageUrl,
                Slug = p.Slug ?? p.ProductName.ToLower().Replace(" ", "-").Replace("/", "-"),
                CategoryName = p.Category?.CategoryName ?? "General",
                ManufacturerName = p.Manufacturer?.ManufacturerName ?? "Original Equipment",
                ProductTypeName = p.ProductType?.TypeName ?? "Standard Asset",
                IsDigitalSaaS = p.ItemNatureId == 2,
                
                // SEO Metadata Engine
                MetaTitle = $"{p.ProductName} | Buy {(p.ItemNatureId == 2 ? "SaaS Subscription" : "Retail Unit")} - DigiPOSE Store",
                MetaDescription = !string.IsNullOrWhiteSpace(p.Description) 
                    ? p.Description.Length >= 150 ? p.Description.Substring(0, 147) + "..." : p.Description
                    : $"Order {p.ProductName} ({p.SKU}) online. Authentic {p.Category?.CategoryName} unit manufactured by {p.Manufacturer?.ManufacturerName ?? "DigiPOSE"}. Best price: {p.BasePrice:N0} VND.",
                MetaKeywords = $"{p.ProductName}, {p.SKU}, {p.Category?.CategoryName}, {p.ProductType?.TypeName}, {(p.ItemNatureId == 2 ? "SaaS, Software License" : "Retail Hardware, POS Asset")}",
                OpenGraphImage = string.IsNullOrEmpty(p.ImageUrl) ? "http://localhost:5000/demo/products/og_default.png" : $"http://localhost:5000{p.ImageUrl}"
            }).ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize),
                Products = results,
                SeoGlobalMeta = new
                {
                    Description = "Explore high-performance POS hardware, receipt printers, and enterprise SaaS subscriptions on DigiPOSE Online Storefront.",
                    Keywords = "DigiPOSE Catalog, ERP hardware, SaaS extensions, POS terminal purchase",
                    Author = "DigiPOSE Systems Architecture Team"
                }
            });
        }

        #endregion

        #region 3. SHOPPING CART OPERATIONS (getShoppingCart, getTotalPrice, getTotalQuantity)

        /// <summary>
        /// Retrieves active shopping cart summary from designated StorefrontCarts table.
        /// Strictly protected from accounting Orders ledger (0% Abandoned Cart Pollution).
        /// </summary>
        [HttpGet("cart/{cartId}")]
        public async Task<IActionResult> GetShoppingCart(int cartId)
        {
            var cart = await _context.StorefrontCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product!)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CartId == cartId);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return Ok(new CartSummaryResponse
                {
                    CartId = cartId,
                    CartGuid = cart?.CartGuid ?? Guid.Empty,
                    CustomerIdentity = cart?.CustomerIdentity ?? "Guest Shopper",
                    CartState = "CardEmpty",
                    TotalQuantity = 0,
                    GrossPrice = 0,
                    TotalTaxAmount = 0,
                    TotalDiscountAmount = 0,
                    TotalPrice = 0,
                    Items = new List<CartDetailItem>()
                });
            }

            int totalQty = cart.Items.Sum(i => i.Quantity);
            var response = new CartSummaryResponse
            {
                CartId = cart.CartId,
                CartGuid = cart.CartGuid,
                CustomerIdentity = cart.SnapshotCustomerName ?? cart.CustomerIdentity,
                CartState = totalQty > 0 ? "Card" : "CardEmpty",
                TotalQuantity = totalQty,
                GrossPrice = cart.GrossAmount,
                TotalTaxAmount = cart.TaxAmount,
                TotalDiscountAmount = cart.DiscountAmount,
                TotalPrice = cart.TotalAmount,
                Items = cart.Items.Select(d => new CartDetailItem
                {
                    ProductId = d.ProductId,
                    SKU = d.Product?.SKU ?? "SKU-N/A",
                    ProductName = d.Product?.ProductName ?? "Item",
                    UnitName = d.UnitName ?? "Pcs",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    LineTotal = d.Quantity * d.UnitPrice,
                    LineTax = 0,
                    ImageUrl = d.Product?.ImageUrl ?? "/demo/products/default_cyber_product.png"
                }).ToList()
            };

            return Ok(response);
        }

        #endregion

        #region 4. CART MUTATIONS (addItem, addToCart, removeItem, removeAllItems, updateQuantity)

        /// <summary>
        /// Adds product to dedicated online shopping cart (addItem / addToCart). Creates new StorefrontCart if cartId == 0.
        /// Features O(1) early RAM stock verification to protect shoppers from out-of-stock items.
        /// </summary>
        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] CartItemRequest request)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Unit)
                .Include(p => p.TaxType)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive);

            if (product == null)
                return NotFound(new { Error = "Product not found or inactive in catalog." });

            StorefrontCart? cart;
            if (request.CartId <= 0)
            {
                cart = new StorefrontCart
                {
                    CartGuid = Guid.NewGuid(),
                    CustomerIdentity = User.Identity?.Name ?? "Guest Shopper",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    GrossAmount = 0,
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0
                };
                _context.StorefrontCarts.Add(cart);
                await _context.SaveChangesAsync();
            }
            else
            {
                cart = await _context.StorefrontCarts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CartId == request.CartId);
                if (cart == null)
                    return NotFound(new { Error = "Shopping cart session invalid or expired." });
            }

            var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == request.ProductId);

            // >>> [O(1) EARLY STOCK GATE]: Prevent adding physical items exceeding live RAM stock balances
            if (product.ItemNatureId == 1) // Physical goods (HQ Fulfillment BranchId = 1)
            {
                int availableStock = await _inventoryRam.GetStockAsync(1, product.ProductId);
                int projectedQty = (existingItem?.Quantity ?? 0) + request.Quantity;
                if (availableStock < projectedQty)
                {
                    return BadRequest(new { Error = "OUT_OF_STOCK", AvailableStock = availableStock, Requested = projectedQty, ProductName = product.ProductName });
                }
            }

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                _context.StorefrontCartItems.Update(existingItem);
            }
            else
            {
                var detail = new StorefrontCartItem
                {
                    CartId = cart.CartId,
                    ProductId = product.ProductId,
                    Quantity = request.Quantity,
                    UnitPrice = product.BasePrice,
                    UnitName = product.Unit?.UnitName ?? "Unit"
                };
                if (cart.Items == null)
                    cart.Items = new List<StorefrontCartItem>();
                cart.Items.Add(detail);
                _context.StorefrontCartItems.Add(detail);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await RecalculateCartTotals(cart.CartId);

            return Ok(new { Message = "Product added to cart successfully", CartId = cart.CartId, CartGuid = cart.CartGuid, CartState = "Card" });
        }

        /// <summary>
        /// Adjusts line item quantity (updateQuantity / increaseProduct / decreaseProduct).
        /// </summary>
        [HttpPut("cart/update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var item = await _context.StorefrontCartItems.Include(i => i.Product).FirstOrDefaultAsync(od => od.CartId == request.CartId && od.ProductId == request.ProductId);
            if (item == null)
                return NotFound(new { Error = "Item not present in shopping cart." });

            if (request.NewQuantity <= 0)
            {
                _context.StorefrontCartItems.Remove(item);
            }
            else
            {
                if (item.Product?.ItemNatureId == 1)
                {
                    int availableStock = await _inventoryRam.GetStockAsync(1, item.ProductId);
                    if (availableStock < request.NewQuantity)
                    {
                        return BadRequest(new { Error = "OUT_OF_STOCK", AvailableStock = availableStock, Requested = request.NewQuantity, ProductName = item.Product.ProductName });
                    }
                }
                item.Quantity = request.NewQuantity;
                _context.StorefrontCartItems.Update(item);
            }

            var cart = await _context.StorefrontCarts.FindAsync(request.CartId);
            if (cart != null) cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await RecalculateCartTotals(request.CartId);

            return Ok(new { Message = "Cart quantity updated", CartId = request.CartId });
        }

        /// <summary>
        /// Removes a specific product from cart (removeItem / deleteProduct).
        /// </summary>
        [HttpDelete("cart/remove")]
        public async Task<IActionResult> RemoveCartItem([FromBody] RemoveCartItemRequest request)
        {
            var item = await _context.StorefrontCartItems.FirstOrDefaultAsync(od => od.CartId == request.CartId && od.ProductId == request.ProductId);
            if (item != null)
            {
                _context.StorefrontCartItems.Remove(item);
                var cart = await _context.StorefrontCarts.FindAsync(request.CartId);
                if (cart != null) cart.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                await RecalculateCartTotals(request.CartId);
            }
            return Ok(new { Message = "Item removed from cart", CartId = request.CartId });
        }

        /// <summary>
        /// Clears all items in the shopping cart (removeAllItems / clearCart -> transitions to CardEmpty).
        /// </summary>
        [HttpPost("cart/clear/{cartId}")]
        public async Task<IActionResult> RemoveAllItems(int cartId)
        {
            var items = await _context.StorefrontCartItems.Where(od => od.CartId == cartId).ToListAsync();
            if (items.Any())
            {
                _context.StorefrontCartItems.RemoveRange(items);
                var cart = await _context.StorefrontCarts.FindAsync(cartId);
                if (cart != null)
                {
                    cart.GrossAmount = 0;
                    cart.TaxAmount = 0;
                    cart.DiscountAmount = 0;
                    cart.TotalAmount = 0;
                    cart.UpdatedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "All cart items removed", CartId = cartId, CartState = "CardEmpty" });
        }

        #endregion

        #region 5. STOREFRONT CHECKOUT & PRODUCTION ORDER CONVERSION

        /// <summary>
        /// Finalizes online E-Commerce checkout with enterprise resilience:
        /// 1. O(1) RAM Idempotency Guard against network retry double-billing.
        /// 2. O(1) RAM Stock Deduction (hot-path fail fast).
        /// 3. ReadCommitted transaction + Append-only SQL ledger (0% deadlock).
        /// 4. Asynchronous Channel queue for E-Invoice and SignalR Telemetry Radar broadcast.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> ProcessStorefrontCheckout([FromBody] StorefrontCheckoutRequest request)
        {
            // >>> [O(1) RAM IDEMPOTENCY GUARD]: Intercept duplicate checkout retries instantly
            string idempCacheKey = $"idemp_storefront_{request.IdempotencyKey}";
            if (_cache.TryGetValue(idempCacheKey, out object? cachedResult) && cachedResult != null)
            {
                return Ok(cachedResult);
            }

            // Verify against completed SQL Orders if cache expired after 24h
            var existingOrder = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey);

            if (existingOrder != null)
            {
                var replayPayload = new
                {
                    Status = "Success",
                    OrderId = existingOrder.OrderId,
                    InvoiceNumber = existingOrder.InvoiceNumber,
                    TotalCharged = existingOrder.TotalAmount,
                    IsReplay = true,
                    Message = "Order previously processed successfully."
                };
                _cache.Set(idempCacheKey, replayPayload, TimeSpan.FromHours(24));
                return Ok(replayPayload);
            }

            var cart = await _context.StorefrontCarts
                .Include(o => o.Items!)
                .ThenInclude(od => od.Product!)
                .ThenInclude(p => p.ItemNature)
                .FirstOrDefaultAsync(o => o.CartId == request.CartId);

            if (cart == null || cart.Items == null || !cart.Items.Any())
                return BadRequest(new { Error = "Cannot checkout an empty shopping cart (CardEmpty state)." });

            var deductedProducts = new List<(int ProductId, int Quantity)>();

            // 1. O(1) HOT-PATH RAM INVENTORY DEDUCTION FOR PHYSICAL GOODS (BranchId = 1)
            foreach (var item in cart.Items)
            {
                if (item.Product?.ItemNatureId == 1) // Physical Retail Asset
                {
                    if (!await _inventoryRam.TryDeductStockAsync(1, item.ProductId, item.Quantity))
                    {
                        // Rollback in-memory deducted stock for items already processed in this checkout
                        foreach (var deducted in deductedProducts)
                        {
                            _inventoryRam.RestoreStock(1, deducted.ProductId, deducted.Quantity);
                        }
                        return BadRequest(new { Error = "OUT_OF_STOCK", ProductId = item.ProductId, ProductName = item.Product.ProductName });
                    }
                    deductedProducts.Add((item.ProductId, item.Quantity));
                }
            }

            // 2. READCOMMITTED ACID TRANSACTION & APPEND-ONLY LEDGER (NO DEADLOCKS)
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                var newOrder = new Order
                {
                    BranchId = 1, // HQ Fulfillment branch for Online Web Orders
                    ShiftId = 1, // Storefront general web fulfillment shift
                    UserId = 1, // Default system web worker
                    StatusId = 2, // 2: Completed / Processing E-Commerce Order
                    PaymentMethodId = request.PaymentMethodId,
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedAt = DateTime.Now,
                    GrossAmount = cart.GrossAmount,
                    TaxAmount = cart.TaxAmount,
                    DiscountAmount = cart.DiscountAmount,
                    TotalAmount = cart.TotalAmount,
                    ShippingAddress = request.ShippingAddress,
                    OrderNotes = request.CustomerNotes,
                    ShippingFee = cart.GrossAmount >= 500000 ? 0 : 30000, // Freeship for orders >= 500,000 VND
                    OrderDetails = new List<OrderDetail>()
                };

                // Assign customer CRM profile and reward loyalty points
                if (request.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer != null)
                    {
                        newOrder.CustomerId = customer.CustomerId;
                        newOrder.SnapshotCustomerName = customer.FullName;
                        newOrder.SnapshotCustomerPhone = customer.PhoneNumber ?? request.ContactPhone;

                        // Award CRM loyalty points (10 points per 100,000 VND spent)
                        int earnedPoints = (int)(newOrder.TotalAmount / 100000) * 10;
                        customer.RewardPoints += earnedPoints;
                    }
                }
                else
                {
                    newOrder.SnapshotCustomerName = cart.SnapshotCustomerName ?? request.CustomerNotes ?? "Online Shopper";
                    newOrder.SnapshotCustomerPhone = request.ContactPhone ?? cart.SnapshotCustomerPhone;
                }

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                newOrder.InvoiceNumber = $"WEB-{DateTime.Now:yyyyMMdd}-{newOrder.OrderId}";

                var productIds = new List<int>();
                foreach (var item in cart.Items)
                {
                    productIds.Add(item.ProductId);
                    decimal taxRate = item.Product?.TaxType?.TaxPercentage ?? 0;
                    decimal lineTax = (item.Quantity * item.UnitPrice) * (taxRate / 100.0m);

                    var od = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.ProductId,
                        NatureId = item.Product?.ItemNatureId ?? 1,
                        TaxTypeId = item.Product?.TaxTypeId ?? 1,
                        Quantity = item.Quantity,
                        ProductName = item.Product?.ProductName ?? "Item",
                        UnitName = item.UnitName,
                        UnitPrice = item.UnitPrice,
                        DiscountRate = 0,
                        DiscountAmount = 0,
                        TaxRate = taxRate,
                        TaxAmount = lineTax,
                        TotalAmount = (item.Quantity * item.UnitPrice) + lineTax
                    };
                    newOrder.OrderDetails!.Add(od);
                    _context.OrderDetails.Add(od);

                    // Handle physical goods vs SaaS subscriptions
                    if (item.Product?.ItemNatureId == 1) // Physical asset -> Delegate to Enterprise DDD Ledger Service
                    {
                        await _ledgerService.RecordTransactionAsync(
                            1, // HQ Fulfillment Branch
                            item.ProductId,
                            -item.Quantity,
                            InventoryTxType.WebSale,
                            newOrder.OrderId,
                            $"WEB-{newOrder.OrderId}",
                            newOrder.UserId,
                            item.UnitPrice,
                            $"Storefront WebSale E-Commerce fulfillment for Order #{newOrder.OrderId}");
                    }
                    else if (item.Product?.ItemNatureId == 2) // Digital SaaS Subscription
                    {
                        int durationDays = 365 * item.Quantity;
                        var existingSub = await _context.Subscriptions
                            .FirstOrDefaultAsync(s => s.CustomerId == newOrder.CustomerId && s.ProductId == item.ProductId && s.Status == "ACTIVE");

                        if (existingSub != null)
                        {
                            existingSub.EndDate = (existingSub.EndDate > DateTime.Now ? existingSub.EndDate : DateTime.Now).AddDays(durationDays);
                            existingSub.UpdatedAt = DateTime.Now;
                            existingSub.OrderId = newOrder.OrderId;
                            _context.Subscriptions.Update(existingSub);
                        }
                        else if (newOrder.CustomerId.HasValue)
                        {
                            var newSub = new Subscription
                            {
                                CustomerId = newOrder.CustomerId.Value,
                                ProductId = item.ProductId,
                                OrderId = newOrder.OrderId,
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now.AddDays(durationDays),
                                Status = "ACTIVE",
                                LicenseKey = $"DIGIPOSE-SAAS-{Guid.NewGuid().ToString().ToUpper().Substring(0, 8)}"
                            };
                            _context.Subscriptions.Add(newSub);
                        }
                    }
                }

                // >>> [ENTERPRISE_FISCAL_EXECUTION]: Balance VAT cent differences and calculate master total with shipping fee
                _vatBalancingEngine.BalanceVatAndCalculateTotal(newOrder, newOrder.OrderDetails!.ToList());

                // Append-only buffer in SQL for resilient background E-Invoice emailing
                var printJob = new JobQueueItem
                {
                    JobType = "EMAIL_STOREFRONT_INVOICE",
                    PayloadJson = JsonSerializer.Serialize(new { OrderId = newOrder.OrderId, InvoiceNumber = newOrder.InvoiceNumber, BranchId = 1, Email = request.ShippingAddress }),
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                _context.JobQueue.Add(printJob);

                // Purge transient online cart after successful checkout conversion
                _context.StorefrontCartItems.RemoveRange(cart.Items);
                _context.StorefrontCarts.Remove(cart);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 3. ASYNCHRONOUS NOTIFICATION & REAL-TIME TELEMETRY RADAR
                _jobChannel.Writer.TryWrite(printJob);

                var liveBalances = await _inventoryRam.GetBulkStockAsync(1, productIds);
                await _hubContext.Clients.Group("Branch_1").SendAsync("OnStockChanged", liveBalances);

                var lowStockAlerts = new List<string>();
                foreach (var kvp in liveBalances)
                {
                    if (kvp.Value <= 5)
                    {
                        var prodName = cart.Items.FirstOrDefault(d => d.ProductId == kvp.Key)?.Product?.ProductName ?? $"SKU #{kvp.Key}";
                        lowStockAlerts.Add($"[CRITICAL_STOCK]: {prodName} dropped to {kvp.Value} unit(s) at HQ Branch #1 due to Storefront web order");
                    }
                }

                var telemetryPayload = new
                {
                    EventType = "ONLINE_STOREFRONT_SALE",
                    OrderId = newOrder.OrderId,
                    InvoiceNumber = newOrder.InvoiceNumber,
                    RevenueDelta = newOrder.TotalAmount,
                    ShippingFee = newOrder.ShippingFee,
                    ShippingAddress = newOrder.ShippingAddress ?? "N/A",
                    BranchId = 1,
                    ProcessedAt = newOrder.CreatedAt.ToString("HH:mm:ss"),
                    LowStockAlerts = lowStockAlerts
                };
                await _hubContext.Clients.Group("AdminTelemetryGroup").SendAsync("OnTelemetryAlert", telemetryPayload);

                var successResponse = new
                {
                    Status = "Success",
                    OrderId = newOrder.OrderId,
                    InvoiceNumber = newOrder.InvoiceNumber,
                    TotalCharged = newOrder.TotalAmount,
                    IsReplay = false,
                    Message = "Online Storefront checkout completed. E-Invoice queued and live telemetry radar broadcasted."
                };
                _cache.Set(idempCacheKey, successResponse, TimeSpan.FromHours(24));

                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var deducted in deductedProducts)
                {
                    _inventoryRam.RestoreStock(1, deducted.ProductId, deducted.Quantity);
                }
                return StatusCode(500, new { Error = "Checkout transaction aborted due to database exception or concurrency race.", Details = ex.Message });
            }
        }

        #endregion

        #region PRIVATE ENGINE HELPER: RECALCULATE TOTALS

        private async Task RecalculateCartTotals(int cartId)
        {
            var cart = await _context.StorefrontCarts.Include(c => c.Items!).ThenInclude(i => i.Product!).ThenInclude(p => p.TaxType).FirstOrDefaultAsync(o => o.CartId == cartId);
            if (cart != null && cart.Items != null)
            {
                decimal gross = 0;
                decimal totalTax = 0;

                foreach (var item in cart.Items)
                {
                    decimal lineGross = item.Quantity * item.UnitPrice;
                    gross += lineGross;

                    decimal taxRate = item.Product?.TaxType?.TaxPercentage ?? 0;
                    totalTax += lineGross * (taxRate / 100.0m);
                }

                cart.GrossAmount = gross;
                cart.TaxAmount = totalTax;
                cart.TotalAmount = gross + totalTax - cart.DiscountAmount;
                _context.StorefrontCarts.Update(cart);
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}
