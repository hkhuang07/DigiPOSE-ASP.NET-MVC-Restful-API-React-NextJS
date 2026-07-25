using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using DigiPOSE.Models.DTOs;
using System.Data;

namespace DigiPOSE.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class POSController : ControllerBase
    {
        private readonly DigiPoseDbContext _context;

        public POSController(DigiPoseDbContext context)
        {
            _context = context;
        }

        [HttpPost("retail-draft/create")]
        public async Task<IActionResult> CreateDraftOrder([FromBody] CreateDraftRequest request)
        {
            var order = new Order
            {
                BranchId = request.BranchId,
                ShiftId = request.ShiftId,
                UserId = request.UserId, // Cashier 
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

        [HttpPost("retail-draft/add-item")]
        public async Task<IActionResult> AddOrIncrementItem([FromBody] AddItemRequest request)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.StatusId == 4);

            if (order == null) return BadRequest(new { Error = "Draft order not found." });

            var product = await _context.Products
                .Include(p => p.TaxType)
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

            if (product == null) return BadRequest(new { Error = "Product not found." });

            var existingDetail = order.OrderDetails?.FirstOrDefault(d => d.ProductId == request.ProductId);

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

            // Recalculate Order totals
            order.GrossAmount = order.OrderDetails!.Sum(d => d.Quantity * d.UnitPrice);
            order.TaxAmount = order.OrderDetails!.Sum(d => d.TaxAmount);
            order.DiscountAmount = order.OrderDetails!.Sum(d => d.DiscountAmount);
            order.TotalAmount = order.OrderDetails!.Sum(d => d.TotalAmount);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Item added successfully", OrderId = order.OrderId, TotalAmount = order.TotalAmount });
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

                order.GrossAmount = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                order.TaxAmount = order.OrderDetails.Sum(d => d.TaxAmount);
                order.DiscountAmount = order.OrderDetails.Sum(d => d.DiscountAmount);
                order.TotalAmount = order.OrderDetails.Sum(d => d.TotalAmount);

                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Item removed", OrderId = order.OrderId });
        }

        [HttpPost("checkout/paid")]
        public async Task<IActionResult> CheckoutPaid([FromBody] CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails!)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.StatusId == 4);
                
                if(order == null)
                {
                    return BadRequest(new { Error = "Draft order not found or already completed" });
                }

                order.StatusId = 1; // 1: Completed
                order.PaymentMethodId = request.PaymentMethodId;
                order.CustomerId = request.CustomerId;

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
                if(shift != null)
                {
                    shift.EndCash += order.TotalAmount;
                    _context.Shifts.Update(shift);
                }

                foreach(var detail in order.OrderDetails!)
                {
                    var product = detail.Product;
                    if (product == null) continue;

                    if (detail.NatureId == 1) // Physical
                    {
                        var inventory = await _context.ProductInventories
                            .FirstOrDefaultAsync(pi => pi.ProductId == product.ProductId && pi.BranchId == order.BranchId);
                            
                        if (inventory != null)
                        {
                            inventory.StockQuantity -= detail.Quantity;
                            _context.ProductInventories.Update(inventory);
                        }
                    }
                    else if (detail.NatureId == 2 && request.CustomerId != null) // Digital / SaaS
                    {
                        var existingSub = await _context.Subscriptions.FirstOrDefaultAsync(
                            s => s.CustomerId == request.CustomerId && 
                            s.ProductId == product.ProductId
                        );
                        
                        int durationDays = 365; // Theo yêu cầu, mặc định 365 ngày
                        
                        if (existingSub != null && existingSub.EndDate > DateTime.Now)
                        {
                            existingSub.EndDate = existingSub.EndDate.AddDays(durationDays * detail.Quantity);
                            existingSub.UpdatedAt = DateTime.Now;
                            existingSub.OrderId = order.OrderId;
                            _context.Subscriptions.Update(existingSub);
                        }
                        else
                        {
                            var newSub = new Subscription {
                                CustomerId = request.CustomerId.Value,
                                ProductId = product.ProductId,
                                OrderId = order.OrderId,
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now.AddDays(durationDays * detail.Quantity),
                                Status = "ACTIVE",
                                LicenseKey = Guid.NewGuid().ToString().ToUpper()
                            };
                            _context.Subscriptions.Add(newSub);
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Checkout successful", OrderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Error = "Checkout failed: " + ex.Message });
            }
        }
    }
}