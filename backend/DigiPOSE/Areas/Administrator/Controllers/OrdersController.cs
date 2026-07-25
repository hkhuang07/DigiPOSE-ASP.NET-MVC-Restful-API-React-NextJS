using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

using System.Linq.Dynamic.Core;


namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class OrdersController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public OrdersController(DigiPoseDbContext context) { _context = context; }

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
            var item = await _context.Orders.FindAsync(id);
            if (item != null)
            {
                _context.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Deleted successfully." });
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
