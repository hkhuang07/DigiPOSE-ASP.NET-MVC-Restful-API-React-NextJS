using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class StockVouchersController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public StockVouchersController(DigiPoseDbContext context) { _context = context; }

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

                var query = _context.StockVouchers
                    .Include(v => v.Branch).Include(v => v.User).Include(v => v.Supplier)
                    .AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.VoucherType != null && m.VoucherType.Contains(searchValue)) ||
                        (m.Branch != null && m.Branch.BranchName != null && m.Branch.BranchName.Contains(searchValue)) ||
                        (m.User != null && m.User.UserName != null && m.User.UserName.Contains(searchValue)) ||
                        (m.Supplier != null && m.Supplier.SupplierName != null && m.Supplier.SupplierName.Contains(searchValue)));
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
                    VoucherId = m.VoucherId,
                    VoucherType = m.VoucherType,
                    BranchName = m.Branch != null ? m.Branch.BranchName : "",
                    UserName = m.User != null ? m.User.UserName : "",
                    SupplierName = m.Supplier != null ? m.Supplier.SupplierName : "---",
                    TotalValue = m.TotalValue,
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
            var item = await _context.StockVouchers.Include(v => v.Branch).Include(v => v.User).Include(v => v.Supplier)
                .FirstOrDefaultAsync(m => m.VoucherId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create() { LoadViewBags(); return PartialView("_CreateOrEditPartial", new StockVoucher { CreatedAt = DateTime.Now }); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockVoucher model)
        {
            if (ModelState.IsValid) { _context.Add(model); await _context.SaveChangesAsync(); return Json(new { success = true, message = "Stock voucher created." }); }
            LoadViewBags(model.BranchId, model.UserId, model.SupplierId);
            return PartialView("_CreateOrEditPartial", model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.StockVouchers.FindAsync(id);
            if (item == null) return NotFound();
            LoadViewBags(item.BranchId, item.UserId, item.SupplierId);
            return PartialView("_CreateOrEditPartial", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StockVoucher model)
        {
            if (id != model.VoucherId) return Json(new { success = false, message = "ID mismatch." });
            if (ModelState.IsValid)
            {
                try { _context.Update(model); await _context.SaveChangesAsync(); return Json(new { success = true, message = "Stock voucher updated." }); }
                catch (DbUpdateConcurrencyException) { }
            }
            LoadViewBags(model.BranchId, model.UserId, model.SupplierId);
            return PartialView("_CreateOrEditPartial", model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.StockVouchers.Include(v => v.Branch).Include(v => v.User).Include(v => v.Supplier)
                .FirstOrDefaultAsync(m => m.VoucherId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.StockVouchers.FindAsync(id);
            if (item != null) { _context.StockVouchers.Remove(item); await _context.SaveChangesAsync(); }
            return Json(new { success = true, message = "Stock voucher deleted." });
        }

        private void LoadViewBags(int? branchId = null, int? userId = null, int? supplierId = null)
        {
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", branchId);
            ViewBag.UserId = new SelectList(_context.Users.Where(u => u.IsActive), "UserId", "UserName", userId);
            ViewBag.SupplierId = new SelectList(_context.Suppliers.Where(s => s.IsActive), "SupplierId", "SupplierName", supplierId);
        }
    }
}
