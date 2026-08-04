# DigiPOSE Station - Nền Tảng Quản Trị Hệ Thống POS & ERP Bán Lẻ Doanh Nghiệp
**Tài Liệu Kiến Trúc & Danh Mục Chức Năng Đã Phát Triển (v1.0.0)**

DigiPOSE là hệ thống quản trị điểm bán lẻ (POS), hoạch định tài nguyên doanh nghiệp (ERP) và thương mại điện tử (E-Commerce) hiện đại, hiệu năng cao, được thiết kế cho các chuỗi bán lẻ quy mô lớn và phân phối phần mềm đám mây B2B (SaaS). Nền tảng hợp nhất hoạt động bán hàng trực tiếp tại quầy với cổng đặt hàng trực tuyến thông qua hệ thống API tiêu chuẩn và trung tâm điều hành quản trị CMS.

---

## 🏛 1. Tổng Quan Kiến Trúc Hệ Thống

DigiPOSE được xây dựng theo kiến trúc phân tách độc lập, tách rời tầng giao dịch người dùng và hạ tầng quản trị kế toán trung tâm:

```
               [ ỨNG DỤNG WEB & TRẠM POS THU NGÂN ]
            (Next.js 15 / React 19 / TypeScript / Tailwind)
            ├── Cổng Thương Mại Điện Tử ---> http://localhost:3000/
            ├── Trạm Bán Hàng Tại Quầy ---> http://localhost:3000/pos
            └── Điều Phối Giỏ Hàng      ---> http://localhost:3000/cart
                              │
                (RESTful JSON API / JWT Bearer)
                              │
                              ▼
           [ ASP.NET CORE MVC & CỔNG KẾT NỐI API ]
                  (http://localhost:5128/)
         ┌────────────────────┴────────────────────┐
         ▼                                         ▼
[ TRUNG TÂM QUẢN TRỊ CMS ]               [ CẢNG WEB API RESTFUL ]
 (ASP.NET Core Razor / Cookie Auth)      (Controllers/Api/ -> JSON Siêu Tốc)
 ├── Quản Lý Dữ Liệu Gốc (30 Controller) ├── Quản Trị Ca & Giao Dịch POS (PosController)
 ├── Báo Cáo Tài Chính & Doanh Thu       ├── Tra Cứu Danh Mục & Thanh Toán (Storefront)
 └── Phân Quyền Vai Trò Người Dùng       └── Kênh Truyền Realtime SignalR (PosRealtimeHub)
         │                                         │
         └────────────────────┬────────────────────┘
                              │
             [ DỊCH VỤ & TIẾN TRÌNH NỀN ]
             ├── IInventoryRAMService (Quản Lý Kho RAM O(1))
             ├── IVatBalancingEngine (Động Cơ Cân Bằng Thuế)
             ├── InventoryWarmupWorker (Nạp Kho RAM Khi Khởi Chạy)
             └── ResilientInvoiceWorker (Hàng Đợi Email Hóa Đơn)
                              │
                (Entity Framework Core 10)
                              ▼
               [ HỆ PHẦN CHẤT CSDL SQL SERVER ]
```

---

## ✨ 2. Danh Mục Các Chức Năng Đã Phát Triển

### 🛒 A. Phân Hệ Bán Hàng Tại Quầy POS Thu Ngân (`/pos` & `PosController.cs`)
* **Khấu Trừ Tồn Kho Siêu Tốc O(1)**: Kiểm tra và giữ hàng trực tiếp trên bộ nhớ RAM qua `IInventoryRAMService` (<15ms) trước khi chốt giao dịch CSDL.
* **Chống Nháy Đúp Đầu Đọc Mã Vạch (Hardware Debounce Guard)**: Tích hợp bộ đệm `IMemoryCache` TTL ngăn chặn lỗi quẹt đúp sản phẩm từ tia laser vật lý.
* **Thuật Toán Cân Bằng Thuế VAT (`VatBalancingEngine.cs`)**: Triển khai thuật toán xử lý chênh lệch thuế làm tròn (`Round(Sum(PreTax) * TaxRate, 2)` so với tổng thuế từng dòng), bơm phần chênh lệch vào dòng sản phẩm chính, bảo đảm kế toán đối soát khớp 100%.
* **Phòng Hộ Giao Dịch Đa Luồng (Dual-Layer Idempotency)**: Kết hợp bộ đệm RAM Cache và khóa Unique Constraint SQL triệt tiêu hoàn toàn lỗi lặp giao dịch khi mạng chập chờn.
* **Quản Lý Tiền Khách Trả & Tiền Thối**: Ghi nhận `TenderedAmount` và tự động tính `ChangeAmount` chuẩn xác cho Thu ngân chốt két cuối ca.
* **Quản Lý Ca Làm Việc & Trạm Thu Ngân**: Khởi tạo ca, chốt ca, đối soát tiền mặt và liên kết máy quầy (`ShiftsController`, `CountersController`).

### 🌐 B. Cổng Thương Mại Điện Tử Trực Tuyến (`/`, `/cart` & `StorefrontController.cs`)
* **Danh Mục Sản Phẩm Động**: Tìm kiếm, lọc theo danh mục, phân trang siêu tốc và tối ưu hóa SEO SSR Metadata.
* **Giỏ Hàng Đệm Phi Kế Toán (`cartStore.ts`)**: Quản lý trạng thái giỏ hàng qua Zustand, kiểm tra tồn kho realtime trước khi thanh toán.
* **Giao Dịch Đặt Hàng Nguyên Tử (Checkout)**: Tính phí vận chuyển (`ShippingFee`), ghi nhận địa chỉ (`ShippingAddress`), ghi chú khách hàng (`OrderNotes`) và đóng khung trong SQL Transaction (`BeginTransactionAsync`).

### 📡 C. Kênh Truyền Dữ Liệu Realtime & SignalR (`PosRealtimeHub.cs`)
* **Đồng Bộ Tồn Kho Tức Thời**: Phát tín hiệu cập nhật tồn kho (`OnStockChanged`) tới tất cả các trạm POS trong chi nhánh với độ trễ <1ms.
* **Cảnh Báo Tồn Kho Thấp**: Tự động bắn cảnh báo (`LowStockAlerts <= 5`) cho Thu ngân và Quản trị viên.
* **Thông Báo Đơn Hàng Mới**: Đẩy thông báo đơn hàng trực tuyến (`WEB_ORDER_CREATED`) lập tức lên màn hình điều hành CMS.

### ⚡ D. Động Cơ Xử Lý Tiến Trình Nền (`Services/Background/`)
* **`InventoryWarmupWorker`**: Tự động nạp sẵn tồn kho của các chi nhánh hoạt động vào RAM Cache khi hệ thống ASP.NET Core khởi động.
* **`ResilientInvoiceWorker`**: Hàng đợi xử lý hóa đơn điện tử và gửi email xác nhận qua MailKit SMTP chạy ngầm, không gây nghẽn luồng thanh toán.

### 🛡️ E. Trung Tâm Quản Trị Hậu Đài CMS (`/Administrator` & `Areas/Administrator/`)
* **30 Controller Quản Trị Dữ Liệu Gốc**: Quản lý CRUD toàn diện cho 26 bảng danh mục (Sản phẩm, Tồn kho, Danh mục, Nhà cung cấp, Khách hàng, Hãng sản xuất, Đơn vị tính, Loại thuế, Phương thức thanh toán,...).
* **Vệ Thần Hoàn Kho Khi Hủy Đơn (`OrdersController.cs`)**: Khi Quản trị viên hủy hoặc xóa đơn hàng, hệ thống tự động hoàn stock về RAM (`RestoreStock`), ghi nhật ký chứng từ kho (`InventoryTransactions`) và phát SignalR báo về quầy POS.
* **Phân Quyền Vai Trò & Bảo Mật (RBAC)**: Kiểm soát truy cập dựa trên vai trò (`Permissions`, `Roles`, `UserRoles`), mã hóa mật khẩu BCrypt, xác thực Cookie & JWT Bearer API.
* **Giao Diện Cyber-Cinematic HUD**: Phong cách Military Lab hiện đại với nền tối (`#000000`), các badge trạng thái neon Cyan/Emerald/Amber/Crimson, thanh tiến trình phân đoạn và hiệu ứng scanline kỹ thuật.

---

## 📁 3. Cấu Trúc Mã Nguồn Dự Án

```
digipose/
├── backend/                  # Nền tảng Máy chủ & Cổng kết nối (.NET SDK 10.0)
│   └── DigiPOSE/             # Dự án chính ASP.NET Core MVC & RESTful Web API
│       ├── Areas/            # Phân vùng Quản trị nội bộ CMS (Administrator Area - 30 Controllers)
│       ├── Controllers/      # Bộ điều hướng Cổng API (Controllers/Api/ -> PosController, StorefrontController)
│       ├── Hubs/             # Kênh kết nối WebSocket Realtime (PosRealtimeHub)
│       ├── Models/           # Thực thể CSDL, DbContext và Định nghĩa DTOs
│       ├── Services/         # Bộ xử lý nghiệp vụ, quản lý kho RAM & động cơ cân bằng thuế
│       │   └── Background/   # Tiến trình nền (InventoryWarmupWorker, ResilientInvoiceWorker)
│       ├── Views/            # Giao diện SSR Razor và cấu trúc bố cục Cyber-HUD
│       └── wwwroot/          # Thư viện Stylesheet tĩnh và thư mục chứa tệp truyền thông
├── frontend/                 # Ứng dụng Giao diện Khách hàng (Node.js v20+)
│   ├── app/                  # Bố cục routing Next.js 15 (/, /pos, /cart)
│   ├── components/           # Bộ thành phần giao diện Cyber-HUD (CyberNavbar, CyberSidebar)
│   ├── services/             # Bộ điều hợp kết nối API và cổng dịch vụ Axios
│   ├── store/                # Bộ quản lý trạng thái máy khách Zustand (cartStore, authStore)
│   └── types/                # Hệ thống từ điển cấu trúc DTO TypeScript
├── docs/                     # Tài liệu thông số kỹ thuật và bản thảo nghiệp vụ hệ thống
├── asset/                    # Hình ảnh sơ đồ, biểu tượng thương mại và dữ liệu bổ trợ
└── demo/                     # Thư mục cô lập lưu trữ hình ảnh giao diện & chụp màn hình
```

---

## 💻 4. Công Nghệ Nền Tảng & Yêu Cầu Môi Môi Trường

### Danh Mục Công Nghệ (Tech Stack)
* **Backend Runtime**: .NET 10.0 SDK, ASP.NET Core MVC, ASP.NET Core Web API, SignalR.
* **Cơ Sở Dữ Liệu**: Microsoft SQL Server 2022+, Entity Framework Core 10, System.Linq.Dynamic.Core.
* **Frontend Runtime**: Node.js v20+, Next.js 15, React 19, TypeScript, Tailwind CSS v4, PostCSS.
* **Quản Trị Trạng Thái**: Zustand State Manager, TanStack React Query, Axios HTTP Client.
* **Bảo Mật & Dịch Vụ**: BCrypt.Net-Next (Mã hóa Hash mật khẩu), MailKit (Cửa điểu SMTP Hóa đơn điện tử), Stateless JWT Bearer & Secure Http-Only Cookie Authentication.

### Yêu Cầu Môi Trường Cài Đặt (Prerequisites)
* [Bộ công cụ phát triển .NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js (Phiên bản từ v20.x trở lên) & Trình quản lý gói npm](https://nodejs.org/)
* [Microsoft SQL Server (2019 hoặc 2022) - Bản Express hoặc Developer](https://www.microsoft.com/en-us/sql-server)
* [Hệ thống quản lý phiên bản Git](https://git-scm.com/)

---

## 🚀 5. Hướng Dẫn Biên Dịch, Cài Đặt & Vận Hành Cụ Thể

### Bước 1: Trích Xuất Mã Nguồn & Cấu Hình Kết Nối CSDL
1. Tải về kho lưu trữ hệ thống:
   ```bash
   git clone <repository-url> digipose
   cd digipose
   ```
2. Mở tệp `backend/DigiPOSE/appsettings.json` và cấu hình chuỗi kết nối:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DigiPOSE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

### Bước 2: Cài Đặt Package & Cập Nhật CSDL
1. Phục hồi NuGet package:
   ```powershell
   cd backend/DigiPOSE
   dotnet restore
   ```
2. Biên dịch dự án Backend:
   ```powershell
   dotnet build --nologo -v q
   ```
3. Cập nhật cấu trúc bảng EF Core và dữ liệu mẫu:
   ```powershell
   dotnet ef database update
   ```

### Bước 3: Khởi Chạy Máy Chủ Backend ASP.NET Core & REST API
```powershell
dotnet run
```
Địa chỉ các trang phục vụ chính:
* **Trang Điều Hành CMS Backoffice**: `http://localhost:5128/Administrator`
* **Cổng REST API POS**: `http://localhost:5128/api/v1/pos/products`
* **Cổng REST API Storefront**: `http://localhost:5128/api/v1/Storefront/user-identity`

---

### Bước 4: Cài Đặt & Khởi Chạy Frontend Next.js Client
Mở cửa sổ Terminal thứ hai:
1. Di chuyển vào thư mục frontend:
   ```powershell
   cd frontend
   ```
2. Cài đặt các gói phụ thuộc NodeJS:
   ```powershell
   npm install
   ```
3. Khởi chạy máy chủ giao diện Next.js:
   ```powershell
   npm run dev
   ```
Địa chỉ các giao diện Next.js:
* **Trang Chủ Bán Hàng Trực Tuyến**: `http://localhost:3000/`
* **Trạm Bán Hàng Quầy POS Thu Ngân**: `http://localhost:3000/pos`
* **Khu Vực Giỏ Hàng & Thanh Toán**: `http://localhost:3000/cart`

---

## 🏗 6. Quy Trình Đóng Gói Triển Khai Production

### Đóng Gói Máy Chủ Backend
```powershell
cd backend/DigiPOSE
dotnet publish -c Release -o ./publish
```

### Đóng Gói App Frontend Next.js
```powershell
cd frontend
npm run build
npm run start --port 3000
```

---

## 🔐 7. Tiêu Chuẩn Bảo Mật & An Toàn Kế Toán Doanh Nghiệp
* **Cô Lập Mật Mã & Tham Số**: Mọi mật khẩu CSDL, secret key JWT và tài khoản MailKit đều được đưa vào `.gitignore` và quản lý qua biến môi trường.
* **Chống Truy Cập IDOR**: Mọi API endpoint đều giải mã JWT token và áp đặt ranh giới quyền truy cập theo từng chi nhánh/người dùng.
* **Giao Dịch Tài Chính Nguyên Tử (ACID)**: Quá trình chốt đơn và thanh toán được bọc chặt trong SQL Transaction `BeginTransactionAsync` với cơ chế Rollback tự động khi gặp sự cố, đảm bảo dữ liệu không bị sai lệch.
