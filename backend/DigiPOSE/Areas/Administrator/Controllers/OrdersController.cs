using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using DigiPOSE.Services;
using DigiPOSE.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class OrdersController : Controller
    {
        private readonly DigiPoseDbContext _context;
        private readonly IInventoryRAMService _inventoryRam;
        private readonly IHubContext<PosRealtimeHub> _hubContext;

        public OrdersController(DigiPoseDbContext context, IInventoryRAMService inventoryRam, IHubContext<PosRealtimeHub> hubContext) 
        { 
            _context = context; 
            _inventoryRam = inventoryRam;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index_LoadData()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                var query = _context.Orders
                    .Include(x => x.Shift)
                    .Include(x => x.User)
                    .Include(x => x.Customer)
                    .Include(x => x.PaymentMethod)
                    .Include(x => x.OrderStatus)
                    .AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        m.OrderId.ToString().Contains(searchValue) ||
                        (m.SnapshotCustomerName != null && m.SnapshotCustomerName.Contains(searchValue)) ||
                        (m.Customer != null && m.Customer.FullName != null && m.Customer.FullName.Contains(searchValue)) ||
                        (m.OrderStatus != null && m.OrderStatus.StatusName != null && m.OrderStatus.StatusName.Contains(searchValue)));
                }

                int filterRecords = query.Count();

                // Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    query = query.OrderBy(sortColumn + " " + sortColumnDirection);
                }
                else
                {
                    query = query.OrderByDescending(v => v.CreatedAt);
                }

                // Paging & Mapping
                var dataList = query.Skip(skip).Take(pageSize).Select(m => new {
                    OrderId = m.OrderId,
                    CustomerName = m.SnapshotCustomerName != null ? m.SnapshotCustomerName : (m.Customer != null ? m.Customer.FullName : "Walk-in"),
                    StatusName = m.OrderStatus != null ? m.OrderStatus.StatusName : "",
                    TotalAmount = m.TotalAmount,
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                return Json(new { draw = draw, recordsFiltered = filterRecords, recordsTotal = totalRecords, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while loading data. Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? searchValue)
        {
            var query = _context.Orders
                .Include(x => x.User)
                .Include(x => x.Customer)
                .Include(x => x.PaymentMethod)
                .Include(x => x.OrderStatus)
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(m =>
                    (m.InvoiceNumber != null && m.InvoiceNumber.Contains(searchValue)) ||
                    (m.Customer != null && m.Customer.FullName != null && m.Customer.FullName.Contains(searchValue)) ||
                    (m.User != null && m.User.UserName != null && m.User.UserName.Contains(searchValue)));
            }
            var list = await query.Select(m => new {
                m.OrderId,
                InvoiceNumber = m.InvoiceNumber ?? $"ORD-{m.OrderId:D6}",
                Customer = m.SnapshotCustomerName != null ? m.SnapshotCustomerName : (m.Customer != null ? m.Customer.FullName : "Walk-in"),
                Staff = m.User != null ? m.User.UserName : "",
                Status = m.OrderStatus != null ? m.OrderStatus.StatusName : "",
                Payment = m.PaymentMethod != null ? m.PaymentMethod.MethodName : "",
                CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                m.GrossAmount,
                m.DiscountAmount,
                m.TaxAmount,
                m.TotalAmount
            }).ToListAsync();

            var bytes = DigiPOSE.Services.CyberExcelExportService.ExportToExcel(list, "Orders", "Sales Orders Ledger Export");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Orders
                .Include(x => x.Shift)
                .Include(x => x.User)
                .Include(x => x.Customer)
                .Include(x => x.PaymentMethod)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create()
        {
            LoadViewBags();
            return PartialView("_CreateOrEditPartial", new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order model)
        {
            model.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try {
                _context.Add(model);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); 
                } catch { 
                    await transaction.RollbackAsync(); 
                    return Json(new { success = false, message="Transaction Failed" }); 
                }
                return Json(new { success = true, message = "Created successfully." });
            }
            LoadViewBags(model.BranchId, model.ShiftId, model.UserId, model.CustomerId, model.StatusId, model.PaymentMethodId);
            return PartialView("_CreateOrEditPartial", model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Orders.FindAsync(id);
            if (item == null) return NotFound();
            LoadViewBags(item.BranchId, item.ShiftId, item.UserId, item.CustomerId, item.StatusId, item.PaymentMethodId);
            return PartialView("_CreateOrEditPartial", item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order model)
        {
            if (id != model.OrderId) return Json(new { success = false, message = "ID mismatch." });

            if (ModelState.IsValid)
            {
                try
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync(); 
                    } catch { 
                        await transaction.RollbackAsync(); 
                        return Json(new { success = false, message="Transaction Failed" }); 
                    }
                    return Json(new { success = true, message = "Updated successfully." });
                }
                catch (DbUpdateConcurrencyException) { }
            }
            LoadViewBags(model.BranchId, model.ShiftId, model.UserId, model.CustomerId, model.StatusId, model.PaymentMethodId);
            return PartialView("_CreateOrEditPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (item != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var restoredProducts = new List<int>();
                    if (item.OrderDetails != null && item.StatusId != 4) // Only restore inventory if order was active/completed, not a raw draft
                    {
                        foreach (var detail in item.OrderDetails)
                        {
                            if (detail.NatureId == 1) // Physical good -> restore stock to shelves
                            {
                                var txLog = new InventoryTransaction
                                {
                                    ProductId = detail.ProductId,
                                    BranchId = item.BranchId,
                                    QuantityDelta = detail.Quantity, // Positive delta to put items back on shelves
                                    TxType = InventoryTxType.Adjustment,
                                    ReferenceOrderId = item.OrderId,
                                    CreatedAt = DateTime.Now
                                };
                                _context.InventoryTransactions.Add(txLog);
                                _inventoryRam.RestoreStock(item.BranchId, detail.ProductId, detail.Quantity);
                                restoredProducts.Add(detail.ProductId);
                            }
                        }
                    }

                    if (item.OrderDetails != null) _context.OrderDetails.RemoveRange(item.OrderDetails);
                    _context.Orders.Remove(item);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // >>> [REALTIME SIGNALR HUD BROADCAST]: Immediately notify POS terminals of restored physical inventory (<1ms)
                    if (restoredProducts.Any())
                    {
                        var liveBalances = await _inventoryRam.GetBulkStockAsync(item.BranchId, restoredProducts);
                        await _hubContext.Clients.Group($"Branch_{item.BranchId}").SendAsync("OnStockChanged", liveBalances);
                    }

                    return Json(new { success = true, message = "Order deleted and stock restored successfully." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Failed to delete order: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Not found." });
        }

        private void LoadViewBags(int? val_BranchId = null, int? val_ShiftId = null, int? val_UserId = null, int? val_CustomerId = null, int? val_StatusId = null, int? val_PaymentMethodId = null)
        {
            ViewBag.BranchId = new SelectList(_context.Branches, "BranchId", "BranchName", val_BranchId);
            ViewBag.ShiftId = new SelectList(_context.Shifts, "ShiftId", "ShiftId", val_ShiftId);
            ViewBag.UserId = new SelectList(_context.Users, "UserId", "UserName", val_UserId);
            ViewBag.CustomerId = new SelectList(_context.Customers, "CustomerId", "FullName", val_CustomerId);
            ViewBag.StatusId = new SelectList(_context.OrderStatuses, "StatusId", "StatusName", val_StatusId);
            ViewBag.PaymentMethodId = new SelectList(_context.PaymentMethods, "PaymentMethodId", "PaymentMethodName", val_PaymentMethodId);
        }
    }
}
