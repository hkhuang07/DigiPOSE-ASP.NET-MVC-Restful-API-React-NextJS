using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class CountersController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public CountersController(DigiPoseDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewData["BranchId"] = new SelectList(_context.Branches, "BranchId", "BranchName");
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

                var query = _context.Counters.Include(c => c.Branch).AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.CounterName != null && m.CounterName.Contains(searchValue)) ||
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
                    CounterId = m.CounterId,
                    CounterName = m.CounterName,
                    BranchName = m.Branch != null ? m.Branch.BranchName : "",
                    IsActive = m.IsActive
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
            var item = await _context.Counters.Include(c => c.Branch).FirstOrDefaultAsync(m => m.CounterId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create()
        {
            ViewBag.BranchId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName");
            return PartialView("_CreateOrEditPartial", new Counter());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Counter model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BranchId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
                return PartialView("_CreateOrEditPartial", model);
            }
            _context.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Counter created successfully." });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Counters.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.BranchId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", item.BranchId);
            return PartialView("_CreateOrEditPartial", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Counter model)
        {
            if (id != model.CounterId) return Json(new { success = false, message = "ID mismatch." });
            if (!ModelState.IsValid)
            {
                ViewBag.BranchId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
                return PartialView("_CreateOrEditPartial", model);
            }
            try { _context.Update(model); await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Json(new { success = false, message = "Concurrency error." }); }
            return Json(new { success = true, message = "Counter updated successfully." });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Counters.Include(c => c.Branch).FirstOrDefaultAsync(m => m.CounterId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Counters.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Record not found." });
            _context.Counters.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Counter permanently deleted." });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Counters.FindAsync(id);
            if (item == null) return Json(new { success = false });
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = item.IsActive, message = item.IsActive ? "Activated." : "Deactivated." });
        }
    }
}
