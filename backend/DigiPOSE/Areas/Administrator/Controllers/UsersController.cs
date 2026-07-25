using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using BC = BCrypt.Net.BCrypt;

using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Branch Manager")]
    public class UsersController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public UsersController(DigiPoseDbContext context) { _context = context; }

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

                var query = _context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Role)
                    .AsQueryable();

                int totalRecords = query.Count();

                // Searching
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.UserName != null && m.UserName.Contains(searchValue)) ||
                        (m.FullName != null && m.FullName.Contains(searchValue)) ||
                        (m.Email != null && m.Email.Contains(searchValue)) ||
                        (m.Branch != null && m.Branch.BranchName.Contains(searchValue)) ||
                        (m.Role != null && m.Role.RoleName.Contains(searchValue)));
                }

                int filterRecords = query.Count();

                // Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    query = query.OrderBy(sortColumn + " " + sortColumnDirection);
                }

                // Paging & Mapping
                var dataList = query.Skip(skip).Take(pageSize).Select(m => new {
                    UserId = m.UserId,
                    UserName = m.UserName,
                    FullName = m.FullName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    RoleName = m.Role != null ? m.Role.RoleName : "",
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
            var item = await _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public IActionResult Create()
        {
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName");
            ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName");
            return PartialView("_CreatePartial", new User());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            ModelState.Remove("PasswordHash");
            if (!ModelState.IsValid)
            {
                ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
                ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", model.RoleId);
                return PartialView("_CreatePartial", model);
            }

            // Hash the raw password before saving
            if (!string.IsNullOrWhiteSpace(model.PasswordHash))
                model.PasswordHash = BC.HashPassword(model.PasswordHash);

            model.IsActive = true;
            _context.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "User created successfully." });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", item.BranchId);
            ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", item.RoleId);
            return PartialView("_EditPartial", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model, string? NewPassword)
        {
            if (id != model.UserId) 
                return Json(new { success = false, message = "ID mismatch." });

            ModelState.Remove("PasswordHash");
            ModelState.Remove("NewPassword");

            if (!ModelState.IsValid)
            {
                ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
                ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", model.RoleId);
                return PartialView("_EditPartial", model);
            }

            try
            {
                var existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
                if (existing == null) 
                    return Json(new { success = false, message = "User not found." });

                // Only re-hash password if a new one was provided
                model.PasswordHash = !string.IsNullOrWhiteSpace(NewPassword)
                    ? BC.HashPassword(NewPassword)
                    : existing.PasswordHash;

                _context.Update(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully." });
            }
            catch (DbUpdateConcurrencyException) { }

            ViewBag.BranchId = new SelectList(_context.Branches.Where(b => b.IsActive), "BranchId", "BranchName", model.BranchId);
            ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", model.RoleId);
            return PartialView("_EditPartial", model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Users
                .Include(u => u.Branch).Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Record not found." });
            _context.Users.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "User permanently deleted." });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return Json(new { success = false });
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = item.IsActive, message = item.IsActive ? "Activated." : "Deactivated." });
        }
    }
}
