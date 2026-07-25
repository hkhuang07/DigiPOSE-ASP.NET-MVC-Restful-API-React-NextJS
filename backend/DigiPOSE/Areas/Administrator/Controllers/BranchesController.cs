using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using DigiPOSE.Web.Helpers;

using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager, POS Operator, Warehouse, Catalog, Accountant")]
    public class BranchesController : Controller
    {
        
        private readonly DigiPoseDbContext _context;
        public BranchesController(DigiPoseDbContext context) { _context = context; }

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

                var query = _context.Branches.AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.BranchName != null && m.BranchName.Contains(searchValue)) ||
                        (m.Address != null && m.Address.Contains(searchValue)) ||
                        (m.ContactPhone != null && m.ContactPhone.Contains(searchValue)) ||
                        (m.Email != null && m.Email.Contains(searchValue)));
                }

                int filterRecords = query.Count();

                // Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    query = query.OrderBy(sortColumn + " " + sortColumnDirection);
                }

                // Paging & Mapping
                var dataList = query.Skip(skip).Take(pageSize).Select(m => new {
                    BranchId = m.BranchId,
                    BranchName = m.BranchName,
                    Slug = m.Slug,
                    Address = m.Address,
                    ContactPhone = m.ContactPhone,
                    Email = m.Email,
                    IsActive = m.IsActive
                }).ToList();

                return Json(new { draw = draw, recordsFiltered = filterRecords, recordsTotal = totalRecords, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while loading data. Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> Details(int? id) //Lấy thông tin chi tiết 1 sản phẩm
        {
            if (id == null) 
                return NotFound();
            var item = await _context.Branches.FirstOrDefaultAsync(m => m.BranchId == id);
            if (item == null) 
                return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create()
            => PartialView("_CreateOrEditPartial", new Branch());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch model)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.BranchName);
            ModelState.Remove("Slug");

            if (!ModelState.IsValid)
                return PartialView("_CreateOrEditPartial", model);

            _context.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Branch created successfully." });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Branches.FindAsync(id);
            if (item == null) return NotFound();
            return PartialView("_CreateOrEditPartial", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch model)
        {
            if (id != model.BranchId) return Json(new { success = false, message = "ID mismatch." });

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.BranchName);
            ModelState.Remove("Slug");

            if (!ModelState.IsValid)
                return PartialView("_CreateOrEditPartial", model);

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Branch updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Concurrency error." });
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Branches.FirstOrDefaultAsync(m => m.BranchId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Branches.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Record not found." });
            _context.Branches.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Branch permanently deleted." });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Branches.FindAsync(id);
            if (item == null) return Json(new { success = false });
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = item.IsActive, message = item.IsActive ? "Activated." : "Deactivated." });
        }
    }
}
