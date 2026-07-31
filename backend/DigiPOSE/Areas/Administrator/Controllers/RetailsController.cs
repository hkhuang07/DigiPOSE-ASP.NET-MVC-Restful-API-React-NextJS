using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using System.Linq.Dynamic.Core;

namespace DigiPOSE.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Authorize(Roles = "Super Admin, Administrator, Tenant Manager, Accountant")]
    public class RetailsController : Controller
    {
        private readonly DigiPoseDbContext _context;
        public RetailsController(DigiPoseDbContext context) { _context = context; }

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

                var query = _context.Retails
                    .Include(r => r.Tenant)
                    .Include(r => r.User)
                    .Include(r => r.PaymentMethod)
                    .AsQueryable();

                int totalRecords = query.Count();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        m.DocNo.Contains(searchValue) ||
                        (m.RetailNo != null && m.RetailNo.Contains(searchValue)) ||
                        (m.BuyerLegalName != null && m.BuyerLegalName.Contains(searchValue)) ||
                        (m.Tenant != null && m.Tenant.TenantName != null && m.Tenant.TenantName.Contains(searchValue)));
                }

                int filterRecords = query.Count();

                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                    query = query.OrderBy(sortColumn + " " + sortColumnDirection);
                else
                    query = query.OrderByDescending(r => r.EndDate);

                var dataList = query.Skip(skip).Take(pageSize).Select(m => new {
                    RetailId = m.RetailId,
                    DocNo = m.DocNo,
                    RetailNo = m.RetailNo ?? "",
                    DocType = m.DocType,
                    TenantName = m.Tenant != null ? m.Tenant.TenantName : "",
                    BuyerName = m.BuyerLegalName ?? "Walk-in",
                    PaymentMethod = m.PaymentMethod != null ? m.PaymentMethod.MethodName : "",
                    TotalAmount = m.TotalAmount,
                    EndDate = m.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsEInvoice = m.IsEInvoiceReported
                }).ToList();

                return Json(new { draw = draw, recordsFiltered = filterRecords, recordsTotal = totalRecords, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? searchValue)
        {
            var query = _context.Retails
                .Include(r => r.Tenant).Include(r => r.User).Include(r => r.PaymentMethod)
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchValue))
                query = query.Where(m => m.DocNo.Contains(searchValue) || (m.BuyerLegalName != null && m.BuyerLegalName.Contains(searchValue)));

            var list = await query.OrderByDescending(r => r.EndDate).Select(m => new {
                m.RetailId, m.DocNo, m.RetailNo, m.DocType,
                Tenant = m.Tenant != null ? m.Tenant.TenantName : "",
                Cashier = m.User != null ? m.User.UserName : "",
                BuyerName = m.BuyerLegalName ?? "Walk-in",
                m.BuyerTaxCode,
                Payment = m.PaymentMethod != null ? m.PaymentMethod.MethodName : "",
                m.TotalQuantity, m.GrossAmount, m.DiscountAmount, m.VatAmount,
                m.TotalAmount, m.TenderedAmount, m.ChangeAmount,
                Date = m.Date.ToString("yyyy-MM-dd"),
                EndDate = m.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                m.IsEInvoiceReported, m.PrintNo
            }).ToListAsync();

            var bytes = DigiPOSE.Services.CyberExcelExportService.ExportToExcel(list, "Retails", "Retail Trade Documents Export");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Retails_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Retails
                .Include(r => r.Order).ThenInclude(o => o!.OrderDetails!)
                .Include(r => r.Tenant).Include(r => r.Counter).Include(r => r.Shift)
                .Include(r => r.User).Include(r => r.Customer).Include(r => r.PaymentMethod)
                .FirstOrDefaultAsync(m => m.RetailId == id);
            if (item == null) return NotFound();
            return PartialView("_DetailsPartial", item);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Retails
                .Include(r => r.Tenant).Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RetailId == id);
            if (item == null) return NotFound();
            return PartialView("_DeletePartial", item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Retails.FindAsync(id);
            if (item != null) { _context.Retails.Remove(item); await _context.SaveChangesAsync(); }
            return Json(new { success = true, message = "Retail document deleted." });
        }
    }
}
