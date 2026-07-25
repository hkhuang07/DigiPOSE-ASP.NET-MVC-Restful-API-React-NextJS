using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class ProductInventoriesController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public ProductInventoriesController(DigiPoseDbContext context) { _context = context; }

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

                var query = _context.ProductInventories.Include(p => p.Branch).Include(p => p.Product).AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.Product != null && m.Product.ProductName != null && m.Product.ProductName.Contains(searchValue)) ||
                        (m.Branch != null && m.Branch.BranchName != null && m.Branch.BranchName.Contains(searchValue)));
                }

                int filterRecords = query.Count();

                // Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    query = query.OrderBy(sortColumn + " " + sortColumnDirection);
                }

                // Paging & Mapping
                var dataList = query.Skip(skip).Take(pageSize).Select(m => new {
                    InventoryId = m.InventoryId,
                    ProductName = m.Product != null ? m.Product.ProductName : "",
                    BranchName = m.Branch != null ? m.Branch.BranchName : "",
                    StockQuantity = m.StockQuantity,
                    MinStockLevel = m.MinStockLevel
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
            var item = await _context.ProductInventories.Include(p => p.Branch).Include(p => p.Product).FirstOrDefaultAsync(m => m.InventoryId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create()
        {
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName");
            ViewBag.ProductId = new SelectList(_context.Products.Where(p => p.IsActive), "ProductId", "ProductName");
            return PartialView("_CreateOrEditPartial", new ProductInventory());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductInventory model)
        {
            if (ModelState.IsValid) { _context.Add(model); await _context.SaveChangesAsync(); return Json(new { success = true, message = "Inventory record created." }); }
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
            ViewBag.ProductId = new SelectList(_context.Products.Where(p => p.IsActive), "ProductId", "ProductName", model.ProductId);
            return PartialView("_CreateOrEditPartial", model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ProductInventories.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", item.BranchId);
            ViewBag.ProductId = new SelectList(_context.Products.Where(p => p.IsActive), "ProductId", "ProductName", item.ProductId);
            return PartialView("_CreateOrEditPartial", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductInventory model)
        {
            if (id != model.InventoryId) return Json(new { success = false, message = "ID mismatch." });
            if (ModelState.IsValid)
            {
                try { _context.Update(model); await _context.SaveChangesAsync(); return Json(new { success = true, message = "Inventory updated." }); }
                catch (DbUpdateConcurrencyException) { }
            }
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
            ViewBag.ProductId = new SelectList(_context.Products.Where(p => p.IsActive), "ProductId", "ProductName", model.ProductId);
            return PartialView("_CreateOrEditPartial", model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ProductInventories.Include(p => p.Branch).Include(p => p.Product).FirstOrDefaultAsync(m => m.InventoryId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ProductInventories.FindAsync(id);
            if (item != null) { _context.ProductInventories.Remove(item); await _context.SaveChangesAsync(); }
            return Json(new { success = true, message = "Inventory record deleted." });
        }
    }
}
