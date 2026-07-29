Đòn phân tích kiến trúc: Các truy vấn liên quan đến Ca làm việc phải đạt tốc độ $\mathcal{O}(1)$ hoặc $\mathcal{O}(\log N)$ thông qua Index, vì mỗi giao dịch (Checkout) của thu ngân đều phải được "móc" vào một Ca làm iệc đang mở (Active Shift).

BUỔI 12: XÂY DỰNG PHÂN HỆ QUẢN LÝ CA LÀM VIỆC VÀ KẾT TOÁN DÒNG TIỀN (SHIFT & CASH MANAGEMENT)
Bước 1: Quản trị Quầy thu ngân (Counters) tại CMS
Tương tự bảng Branches và Suppliers, bảng Counters là Master Data tĩnh. Sếp tiếp tục sử dụng Scaffolding để tạo CountersController trong Areas/Admin/Controllers.

Mỗi quầy sẽ đại diện cho một máy PoS vật lý (Ví dụ: Máy PoS 01 - Tầng 1, Máy PoS 02 - Quầy thịt cá). Chúng bắt buộc phải được gắn vào một BranchId.
(Tài liệu bỏ qua phần code CRUD cơ bản này để dồn lực vào API lõi).

Bước 2: Thiết kế DTOs (Data Transfer Objects) cho luồng Giao ca
Máy PoS (Frontend) sẽ không gửi nguyên Object Database lên Server mà phải thông qua DTO để bảo mật dữ liệu nhạy cảm.

Tạo file ShiftDtos.cs trong thư mục Models/DTOs/:

C#
using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Web.Models.DTOs
{
    public class OpenShiftRequest
    {
        [Required]
        public int CounterId { get; set; }
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền đầu ca không hợp lệ.")]
        public decimal StartCash { get; set; }
    }

    public class CloseShiftRequest
    {
        [Required]
        public decimal DeclaredEndCash { get; set; } // Tiền mặt thực tế thu ngân đếm được trong két
    }
}
Bước 3: Xây dựng RESTful API Quản lý Ca làm việc (ShiftsApiController)
Đây là trung tâm điều phối dòng tiền. Yêu cầu nghiệp vụ bắt buộc:

Một nhân viên không thể mở 2 ca cùng lúc.

Tiền cuối ca (EndCash) phải được tính tự động dựa trên: Tiền đầu ca + Tổng doanh thu tiền mặt bán được trong ca.

Tạo file ShiftsApiController.cs trong thư mục Controllers/Api/:

C#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Web.Models;
using DigiPOSE.Web.Models.DTOs;

namespace DigiPOSE.Web.Controllers.Api
{
    [Route("api/v1/shifts")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Bắt buộc dùng JWT của máy PoS
    public class ShiftsApiController : ControllerBase
    {
        private readonly DigiPoseDbContext _context;

        public ShiftsApiController(DigiPoseDbContext context)
        {
            _context = context;
        }

        // 1. MỞ CA LÀM VIỆC (OPEN SHIFT)
        [HttpPost("open")]
        public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request)
        {
            // Trích xuất ID nhân viên thu ngân từ Token
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int currentUserId = int.Parse(userIdStr);

            // RÀNG BUỘC 1: Kiểm tra xem nhân viên này có đang mở ca nào khác không?
            var activeShift = await _context.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == currentUserId && s.Status == "Open");

            if (activeShift != null)
                return BadRequest(new { Message = "Bạn đang có một ca làm việc chưa đóng. Vui lòng đóng ca cũ trước.", ShiftId = activeShift.ShiftId });

            // RÀNG BUỘC 2: Kiểm tra xem Quầy (Counter) này có ai khác đang dùng không?
            var counterInUse = await _context.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CounterId == request.CounterId && s.Status == "Open");
                
            if (counterInUse != null)
                return BadRequest(new { Message = "Quầy máy PoS này đang được sử dụng bởi nhân viên khác." });

            // Khởi tạo Ca mới
            var newShift = new Shift
            {
                UserId = currentUserId,
                CounterId = request.CounterId,
                StartTime = DateTime.Now,
                StartCash = request.StartCash,
                EndCash = request.StartCash, // Khởi tạo bằng tiền đầu ca, sẽ cộng dồn khi có Order
                Status = "Open"
            };

            _context.Shifts.Add(newShift);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mở ca thành công.", ShiftId = newShift.ShiftId, StartTime = newShift.StartTime });
        }

        // 2. ĐÓNG CA VÀ KẾT TOÁN (CLOSE SHIFT)
        [HttpPost("close/{shiftId}")]
        public async Task<IActionResult> CloseShift(int shiftId, [FromBody] CloseShiftRequest request)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            int currentUserId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);

            // BẮT ĐẦU TRANSACTION ĐỂ ĐẢM BẢO CHỐT SỔ AN TOÀN
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shift = await _context.Shifts
                    .Include(s => s.Counter)
                    .FirstOrDefaultAsync(s => s.ShiftId == shiftId && s.Status == "Open");

                if (shift == null)
                    return NotFound(new { Message = "Không tìm thấy ca làm việc hoặc ca đã được đóng." });

                // Quyền: Chỉ người mở ca hoặc Admin mới được đóng ca
                if (shift.UserId != currentUserId && !User.IsInRole("Admin"))
                    return StatusCode(403, new { Message = "Bạn không có quyền đóng ca của nhân viên khác." });

                // Tính tổng doanh thu từ các Hóa đơn (Orders) thuộc Ca này
                var totalSales = await _context.Orders
                    .Where(o => o.ShiftId == shiftId && o.Status == "Completed")
                    .SumAsync(o => o.FinalTotal); // Sử dụng hàm SumAsync của SQL Server để xử lý O(1) tại tầng DB

                // Tính toán tiền mặt dự kiến phải có trên hệ thống
                decimal expectedEndCash = shift.StartCash + totalSales;
                decimal cashDiscrepancy = request.DeclaredEndCash - expectedEndCash; // Độ lệch tiền (Âm = Thiếu, Dương = Dư)

                // Cập nhật trạng thái đóng ca
                shift.EndTime = DateTime.Now;
                shift.EndCash = expectedEndCash;
                shift.Status = "Closed";

                _context.Shifts.Update(shift);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new 
                { 
                    Message = "Đóng ca và kết toán thành công.", 
                    ShiftId = shift.ShiftId,
                    TotalSales = totalSales,
                    ExpectedCash = expectedEndCash,
                    DeclaredCash = request.DeclaredEndCash,
                    Discrepancy = cashDiscrepancy // Trả về độ lệch để Frontend hiển thị cảnh báo
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "Lỗi kết toán: " + ex.Message });
            }
        }
        
        // 3. TRUY VẤN TRẠNG THÁI CA HIỆN TẠI (Dành cho Frontend khôi phục phiên)
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentShift()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int currentUserId = int.Parse(userIdStr);

            var activeShift = await _context.Shifts
                .Include(s => s.Counter)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == currentUserId && s.Status == "Open");

            if (activeShift == null) return NotFound(new { Message = "Bạn chưa mở ca làm việc nào." });

            return Ok(new 
            {
                ShiftId = activeShift.ShiftId,
                CounterName = activeShift.Counter!.CounterName,
                StartTime = activeShift.StartTime,
                StartCash = activeShift.StartCash,
                CurrentCash = activeShift.EndCash
            });
        }
    }
}
Bước 4: Tích hợp Ràng buộc Ca làm việc vào Quá trình Thanh toán (Checkout)
Tại tài liệu Buổi 6, chúng ta đã viết hàm Checkout trong PosController.cs. Giờ đây, ta cần chỉnh sửa lại để hệ thống tự động cộng dồn doanh thu vào ca làm việc hiện tại.

Mở file Controllers/Api/PosController.cs, tìm hàm Checkout và nâng cấp đoạn Cập nhật doanh thu Ca làm việc:

C#
// (Trích xuất từ hàm Checkout trong PosController.cs)

// ... [Code xử lý Order, Payment và Trừ tồn kho] ...

// Cập nhật doanh thu Ca làm việc (Shift) tự động và an toàn (Optimistic Concurrency)
var currentShift = await _context.Shifts.FindAsync(order.ShiftId);
if (currentShift != null) 
{
    if (currentShift.Status != "Open")
    {
        throw new Exception("Ca làm việc của bạn đã bị đóng. Không thể thanh toán hóa đơn này.");
    }
    // Ghi nhận dòng tiền thanh toán vào Két (EndCash)
    currentShift.EndCash += order.FinalTotal; 
    _context.Shifts.Update(currentShift);
}

// ... [Code SaveChangesAsync và Commit Transaction] ...