# BUỔI 6: XÂY DỰNG RESTFUL API CHO MÁY POS, E-INVOICE & QUẢN LÝ SUBSCRIPTION (SAAS)

## 1. TỔNG QUAN KIẾN TRÚC (API-FIRST & DATABASE-BACKED)

Trong môi trường Production chuẩn ERP/POS, việc sử dụng "Session" (như ở ITShop) để lưu giỏ hàng là không an toàn. Nếu trình duyệt crash hoặc mất điện, dữ liệu giỏ hàng sẽ mất.

**Kiến trúc mới (Dựa trên RetailTemp API):**
1. **Đơn Nháp (Retail Temp / Draft Order):** Mọi thao tác thêm/bớt món, sửa giá, chiết khấu trên màn hình POS đều bắn API (e.g. `/api/v1/pos/retail-temp/...`) và lưu trực tiếp xuống Database. Trạng thái đơn lúc này là `Draft`.
2. **Thanh Toán Nguyên Khối (Pos Paid):** Khi bấm thanh toán (`/api/v1/pos/checkout/paid`), hệ thống sẽ mở một **Database Transaction (ACID)** để:
   - Đổi trạng thái đơn từ `Draft` -> `Completed`.
   - Cập nhật số dư Ca làm việc (Shift).
   - Khấu trừ tồn kho (Với hàng Vật lý).
   - Sinh ra/Gia hạn `Subscriptions` (Với hàng Dịch vụ Digital - SaaS).
   - Gửi Email Biên lai điện tử (E-Invoice).

> **Lưu ý:** Giao diện Thu ngân (Frontend POS) thường là ứng dụng Single Page Application (SPA - React/Vue/Razor SPA) full-screen. Trong tài liệu này, chúng ta tập trung xây dựng hoàn chỉnh **Backend API**. Frontend có thể chỉ là trang HTML trơn gọi API bằng `fetch` ở giai đoạn này.

---

## 2. THỰC HÀNH: THIẾT KẾ MODEL SUBSCRIPTIONS (BÁN PHẦN MỀM)

Bên cạnh hàng hóa vật lý, DigiPOSE cho phép bán các gói phần mềm. Chúng ta cần bảng `Subscriptions` để theo dõi thời hạn sử dụng.

**Chuẩn Production (Gia hạn nối tiếp):** Khi khách hàng mua thêm 1 năm trong khi gói cũ còn 2 tháng, hệ thống sẽ tự động cộng dồn (Extend) tạo thành 1 năm 2 tháng.

**Bước 2.1: Tạo File `Models/Subscription.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int OrderId { get; set; } // Gắn với Order gốc để Audit (Truy xuất lịch sử mua)
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        [StringLength(500)]
        public string? LicenseKey { get; set; }
        
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, CANCELED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Customer? Customer { get; set; }
        public Product? Product { get; set; }
        public Order? Order { get; set; }
    }
}
```

**Bước 2.2: Cập nhật `DigiPoseDbContext.cs`**
- Thêm `public DbSet<Subscription> Subscriptions { get; set; }`
- Thêm Index trong `OnModelCreating` để tăng tốc độ tìm kiếm:
  `modelBuilder.Entity<Subscription>().HasIndex(s => new { s.CustomerId, s.ProductId });`

---

## 3. THỰC HÀNH: XÂY DỰNG POS CONTROLLER (RETAIL TEMP & PAID)

Tạo Controller mới `Controllers/Api/PosController.cs`.

### 3.1. Nhóm API Quản lý Đơn Nháp (Retail Temp)

Đây là các API tương tác liên tục khi thu ngân thao tác trên màn hình.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

namespace DigiPOSE.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PosController : ControllerBase
    {
        private readonly DigiPoseDbContext _context;

        public PosController(DigiPoseDbContext context)
        {
            _context = context;
        }

        // 1. Tạo đơn nháp (Khởi tạo phiên bán hàng)
        [HttpPost("retail-temp/create")]
        public async Task<IActionResult> CreateTempOrder([FromBody] CreateTempRequest req)
        {
            var order = new Order
            {
                BranchId = req.BranchId,
                ShiftId = req.ShiftId,
                UserId = req.UserId, // Thu ngân
                StatusId = 2, // 2: DRAFT
                CreatedAt = DateTime.Now
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return Ok(new { OrderId = order.OrderId, Status = "Draft Created" });
        }

        // 2. Thêm hoặc Tăng số lượng món (Add / Increment Item)
        [HttpPost("retail-temp/add-item")]
        public async Task<IActionResult> AddOrIncrementItem([FromBody] AddItemRequest req)
        {
            // Logic: Kiểm tra xem OrderId đã có ProductId này chưa.
            // Nếu có -> Cộng dồn Quantity.
            // Nếu chưa -> Insert mới vào bảng OrderDetails.
            // Sau đó tính toán lại TotalAmount của bảng Order.
            return Ok(new { Message = "Sẽ được tự động hóa bằng AI" });
        }
        
        // 3. Xóa món (Remove Item)
        [HttpPost("retail-temp/remove-item")]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest req)
        {
            return Ok(new { Message = "Sẽ được tự động hóa bằng AI" });
        }
```

### 3.2. API Thanh toán Nguyên khối (Pos Paid / Checkout)

Đảm bảo tính ACID, mọi thứ phải hoàn tất hoặc không có gì thay đổi.

```csharp
        [HttpPost("checkout/paid")]
        public async Task<IActionResult> CheckoutPaid([FromBody] CheckoutRequest req)
        {
            // 1. Bắt đầu Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == req.OrderId);
                
                if (order == null || order.StatusId != 2) // Không phải Draft
                    return BadRequest("Đơn hàng không hợp lệ.");

                // 2. Chốt thông tin thanh toán
                order.StatusId = 1; // 1: COMPLETED
                order.PaymentMethodId = req.PaymentMethodId;
                order.CustomerId = req.CustomerId; // Nếu có khách hàng
                
                // 3. Cập nhật Doanh thu vào Ca (Shift)
                var shift = await _context.Shifts.FindAsync(order.ShiftId);
                shift!.EndCash += order.TotalAmount;

                // 4. Khấu trừ tồn kho & Xử lý Subscriptions
                foreach(var detail in order.OrderDetails!)
                {
                    var product = detail.Product;
                    if (product.ItemNatureId == 1) // 1: PHYSICAL
                    {
                        // Giảm số lượng trong ProductInventories
                    }
                    else if (product.ItemNatureId == 2 && req.CustomerId != null) // 2: DIGITAL (SAAS)
                    {
                        // CHUẨN PRODUCTION: CỘNG DỒN NGÀY GIA HẠN
                        var existingSub = await _context.Subscriptions
                            .FirstOrDefaultAsync(s => s.CustomerId == req.CustomerId && s.ProductId == product.ProductId);

                        int durationDays = 365; // Lấy từ cấu hình Product (VD: 1 Năm)

                        if (existingSub != null && existingSub.EndDate > DateTime.Now)
                        {
                            // Khách đang còn hạn -> Cộng dồn
                            existingSub.EndDate = existingSub.EndDate.AddDays(durationDays);
                            existingSub.UpdatedAt = DateTime.Now;
                            existingSub.OrderId = order.OrderId; // Cập nhật Audit mới nhất
                        }
                        else
                        {
                            // Khách mua mới hoặc đã hết hạn
                            var newSub = new Subscription
                            {
                                CustomerId = req.CustomerId.Value,
                                ProductId = product.ProductId,
                                OrderId = order.OrderId,
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now.AddDays(durationDays),
                                Status = "ACTIVE",
                                LicenseKey = Guid.NewGuid().ToString().ToUpper() // Tự sinh key
                            };
                            _context.Subscriptions.Add(newSub);
                        }
                    }
                }

                // 5. Lưu xuống DB và Commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                // 6. Gửi Hóa Đơn Điện Tử (Email) bất đồng bộ
                // await _mailService.SendReceiptEmailAsync(order, customer.Email);

                return Ok(new { Message = "Thanh toán thành công!", OrderId = order.OrderId });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi xảy ra ở bất kỳ dòng code nào -> Phục hồi DB (Rollback)
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}

// Data Transfer Objects (DTO)
public class CreateTempRequest { public int BranchId { get; set; } public int ShiftId { get; set; } public int UserId { get; set; } }
public class AddItemRequest { public int OrderId { get; set; } public int ProductId { get; set; } public int Quantity { get; set; } }
public class RemoveItemRequest { public int OrderId { get; set; } public int ProductId { get; set; } }
public class CheckoutRequest { public int OrderId { get; set; } public int PaymentMethodId { get; set; } public int? CustomerId { get; set; } }
```

---

> **BƯỚC TIẾP THEO (AI TỰ ĐỘNG HÓA):**
> Sau khi bạn nắm được logic chia khối (Temp vs Paid) và cơ chế gia hạn tự động `Subscriptions` (Extend EndDate), các thành phần tốn thời gian như:
> 1. Viết chi tiết hàm `AddOrIncrementItem` (Trừ chiết khấu, tính thuế tổng đơn).
> 2. Hàm `RemoveItem`, `UpdateDiscount`.
> 3. Cấu hình gửi mail thực tế qua SMTP.
> 
> Sẽ được AI hỗ trợ sinh ra hoàn chỉnh trong những file code thực tế. Hướng tiếp cận này giúp bạn duy trì mã nguồn sạch (Clean Code) chuẩn Enterprise ERP.