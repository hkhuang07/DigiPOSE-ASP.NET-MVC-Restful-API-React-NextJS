# DigiPOSE Station - Nền Tảng Quản Trị Hệ Thống POS & ERP Bán Lẻ Doanh Nghiệp
**Tài Liệu Kiến Trúc & Danh Mục Chức Năng Đã Phát Triển (v1.0.0)**

DigiPOSE Station là hệ thống quản trị điểm bán lẻ (POS), hoạch định tài nguyên doanh nghiệp (ERP) và thương mại điện tử (E-Commerce) hiện đại, hiệu năng cao, được thiết kế cho các chuỗi bán lẻ quy mô lớn và phân phối phần mềm đám mây B2B (SaaS). Nền tảng hợp nhất hoạt động bán hàng trực tiếp tại quầy với cổng đặt hàng trực tuyến thông qua hệ thống API tiêu chuẩn và trung tâm điều hành quản trị CMS.

---

## 🖥️ Trình Diễn Kiến Trúc & Giao Diện Thực Tế (Project Visuals)

### 1. Cổng Xác Thực & Bảo Mật Hệ Thống (Security & Identity Gateway)

<p align="center">
  <img src="assets/login.jpg" alt="Cổng Đăng Nhập & Bảo Mật Turnstile" width="850"/>
  <br />
  <strong>Cổng Đăng Nhập & Vệ Thần Phòng Chống Bot Tự Động</strong><br />
  <em>Giao diện Cyber-Cinematic HUD với bộ nền kính tối (Dark Glassmorphism), tiêu chuẩn chữ kỹ thuật (Typography Brand Card) và tích hợp bảo mật Cloudflare Turnstile với thuật toán thử lại tự động Exponential Backoff phía backend.</em>
</p>

<p align="center">
  <img src="assets/signup.jpg" alt="Đăng Ký Tài Khoản Hệ Thống" width="850"/>
  <br />
  <strong>Cổng Đăng Ký Tài Khoản & Định Danh Nhân Sự Doanh Nghiệp</strong><br />
  <em>Quy trình tiếp nhận người dùng mới tích hợp kiểm tra tính hợp lệ dữ liệu trực tiếp (Real-time Field Validation), phản hồi trạng thái mật khẩu và ngắt chặn tức thì các nguy cơ tấn công tự động.</em>
</p>

---

### 2. Cổng Thương Mại Điện Tử & Bán Lẻ (E-Commerce Retail Storefront)

<p align="center">
  <img src="assets/store-front.jpg" alt="Trang Chủ Thương Mại Điện Tử" width="850"/>
  <br />
  <strong>Cổng Bán Lẻ & Đặt Hàng B2B Trực Tuyến</strong><br />
  <em>Giao diện bán hàng trực tuyến có độ trễ cực thấp, hiển thị danh mục sản phẩm động, thẻ thông báo trạng thái tồn kho real-time và hỗ trợ kết xuất Server-Side Rendering (SSR) tối ưu cho tiêu chuẩn SEO.</em>
</p>

<p align="center">
  <img src="assets/storefront-search-filter-expert.jpg" alt="Hệ Thống Tìm Kiếm & Lọc Chuyên sâu" width="850"/>
  <br />
  <strong>Bộ Động Cơ Tìm Kiếm & Lọc Danh Mục Đa Tầng (Search & Filter Expert)</strong><br />
  <em>Hệ thống tra cứu thông tin sản phẩm tốc độ cao, cho phép lọc theo cây danh mục kỹ thuật, hãng sản xuất, mức giá và tìm kiếm trọn vẹn từ khóa (Full-Text Query) tức thì mà không cần nạp lại trang.</em>
</p>

---

### 3. Trung Tâm Quản Trị Hậu Đài & ERP (Enterprise CMS & Operations Hub)

<p align="center">
  <img src="assets/administrator-role-home.jpg" alt="Bảng Điều Khiển Quản Trị Trung Tâm" width="850"/>
  <br />
  <strong>Bảng Điều Khiển Telemetry & Chỉ Số Doanh Nghiệp (Administrator Dashboard)</strong><br />
  <em>Trung tâm chỉ huy dành cho Ban Quản trị, giám sát thời gian thực các chỉ số KPI doanh thu, trạng thái các ca bán hàng đang vận hành và lưới điều hướng nhanh đến toàn bộ phân hệ ERP.</em>
</p>

<p align="center">
  <img src="assets/catalog-manager.jpg" alt="Quản Trị Danh Mục Dữ Liệu Gốc" width="850"/>
  <br />
  <strong>Phân Hệ Quản Trị Dữ Liệu Gốc & Danh Mục Hàng Hóa (Catalog Manager)</strong><br />
  <em>Trung tâm thao tác CRUD toàn diện quản lý hơn 26 thực thể dữ liệu doanh nghiệp, cho phép gán cấu hình mã vạch (Barcode/SKU), quy đổi đơn vị tính và quy hoạch cây danh mục đa cấp.</em>
</p>

<p align="center">
  <img src="assets/inventory-manager.jpg" alt="Quản Trị Kho Vận RAM & Kế Toán" width="850"/>
  <br />
  <strong>Trung Tâm Quản Trị Tồn Kho Biến Động O(1) (Inventory Manager)</strong><br />
  <em>Hạ tầng theo dõi mức tồn kho được nạp siêu tốc trên RAM Cache, tự động khôi phục số lượng khi phát sinh lệnh hủy đơn và liên tục đối soát nhật ký kiểm kê bằng chứng từ cơ sở dữ liệu SQL.</em>
</p>

<p align="center">
  <img src="assets/sales-billing-manager.jpg" alt="Quản Trị Giao Dịch & Tài Chính" width="850"/>
  <br />
  <strong>Phân Hệ Quản Trị Hóa Đơn & Tài Chính Bán Hàng (Sales & Billing Manager)</strong><br />
  <em>Kênh kiểm tra và chứng thực hóa đơn điện tử thời gian thực, trang bị Thuật toán Cân bằng Thuế VAT (VAT Balancing Engine) bảo đảm tổng số tiền đối soát trên sổ sách kế toán trùng khớp tuyệt đối đến từng đồng.</em>
</p>

<p align="center">
  <img src="assets/partners-crm-manager.jpg" alt="Quản Trị Đối Tác & CRM" width="850"/>
  <br />
  <strong>Danh Mục Đối Tác Thương Mại & Hệ Thống CRM (Partners CRM Manager)</strong><br />
  <em>Cơ sở dữ liệu quản trị mối quan hệ khách hàng, thiết lập các tệp khách hàng VIP, thông số tuân thủ pháp lý B2B (Mã số thuế & Tên Công ty) cũng như kiểm soát danh sách Nhà cung cấp chuỗi cung ứng.</em>
</p>

<p align="center">
  <img src="assets/system-iam-manager.jpg" alt="Quản Trị Phân Quyền IAM & RBAC" width="850"/>
  <br />
  <strong>Cổng Quản Trị Phân Quyền Bảo Mật IAM & RBAC (System IAM Manager)</strong><br />
  <em>Phân hệ quản trị danh tính và kiểm soát truy cập dựa trên vai trò (Role-Based Access Control), cho phép chỉ định quyền hạn thi hành nghiêm ngặt cho từng cấp Thu ngân, Giám sát và Ban điều hành.</em>
</p>

---

### 4. Hệ Thống Ngôn Ngữ Thiết Kế Cyber-Cinematic (Design System FX)

<p align="center">
  <img src="assets/processbar.jpg" alt="Ngôn Ngữ Thiết Kế Cyber-Cinematic HUD" width="850"/>
  <br />
  <strong>Đặc Trưng Giao Diện Viễn Trinh & Thể Hiện Trạng Thái Kỹ Thuật (Military HUD FX)</strong><br />
  <em>Thiết kế độc quyền với thanh tiến trình phân đoạn (Segmented Progress Bars), đường vạch tia quét hiển thị mật độ thông tin cao và bảng phối màu huỳnh quang (#00E5FF, #00FF66, #FFB000, #FF3333) tối ưu hóa tốc độ nhận diện cho nhân sự thao tác.</em>
</p>

---

## 🏛 1. Tổng Quan Kiến Trúc Hệ Thống

DigiPOSE được xây dựng theo kiến trúc phân tách độc lập (Decoupled Domain-Driven Architecture), tách rời tầng giao dịch người dùng và hạ tầng quản trị kế toán trung tâm:

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
             ├── ICloudflareTurnstileService (Vệ Thần Chống Bot)
             ├── InventoryWarmupWorker (Nạp Kho RAM Khi Khởi Chạy)
             └── ResilientInvoiceWorker (Hàng Đợi Email Hóa Đơn)
                              │
                (Entity Framework Core 10)
                              ▼
               [ HỆ QUẢN TRỊ CSDL SQL SERVER ]
```

---

## ✨ 2. Danh Mục Các Chức Năng Đã Phát Triển

### 🛒 A. Phân Hệ Bán Hàng Tại Quầy POS Thu Ngân (`/POS` & `PosController.cs`)
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

### 🛡️ D. Bảo Mật Vững Chắc & Phòng Ngự Chống Bot (`CloudflareTurnstileService.cs`)
* **Xác Thực Zero-Friction Turnstile**: Tích hợp CAPTCHA thế hệ mới Cloudflare Turnstile bảo vệ các tuyến đường Đăng ký và Đăng nhập khỏi lực lượng cản phá tự động.
* **Thuật Toán Tự Chữa Lỗi Exponential Backoff**: Xử lý thử lại các cuộc gọi xác thực mây khi phát sinh gián đoạn mạng ngắt quãng mà không làm đổ vỡ trải nghiệm người dùng.
* **Tiêu Chuẩn Cô Lập Hồ sơ Bảo Mật (SecOps Guardrails)**: Tự động phân tách thông số nhạy cảm qua tệp mẫu `.example` (`appsettings.example.json`) và chặn tường rào bọc lọt qua git (`.gitignore`).

### ⚡ E. Động Cơ Xử Lý Tiến Trình Nền (`Services/Background/`)
* **`InventoryWarmupWorker`**: Tự động nạp sẵn tồn kho của các chi nhánh hoạt động vào RAM Cache khi hệ thống ASP.NET Core khởi động.
* **`ResilientInvoiceWorker`**: Hàng đợi xử lý hóa đơn điện tử và gửi email xác nhận qua MailKit SMTP chạy ngầm, không gây nghẽn luồng thanh toán.

### 🏢 F. Trung Tâm Quản Trị Hậu Đài CMS (`/Administrator` & `Areas/Administrator/`)
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
│       ├── Services/         # Bộ xử lý nghiệp vụ, quản lý kho RAM, xác thực Turnstile & động cơ cân bằng thuế
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
├── assets/                   # Hình ảnh trực quan kiến trúc, biểu tượng thương mại và dữ liệu bổ trợ
└── demo/                     # Thư mục lưu trữ hình ảnh minh họa và media cô lập
```

---

## 💻 4. Công Nghệ Sử Dụng & Yêu Cầu Môi Trường

### Danh sách các công nghệ (Tech Stack)
* **Nền Tảng Server**: .NET 10.0 SDK, ASP.NET Core MVC, ASP.NET Core Web API, SignalR WebSockets.
* **Cơ Sở Dữ Liệu**: Microsoft SQL Server 2022+, Entity Framework Core 10, System.Linq.Dynamic.Core.
* **Nền Tảng Giao Diện**: Node.js v20+, Next.js 15, React 19, TypeScript, Tailwind CSS v4, PostCSS.
* **Quản Lý Trạng Thái & Kênh Tiếp Cận**: Zustand, TanStack React Query, Axios.
* **Bảo Mật & Công Cụ**: Cloudflare Turnstile Bot Defense, BCrypt.Net-Next, MailKit SMTP, JWT Bearer Token & HTTP-Only Cookie.

### Điều Kiện Môi Trường Hạng Tầng
* [.NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js (v20.x hoặc cao hơn) & npm](https://nodejs.org/)
* [Microsoft SQL Server (2019/2022) hoặc SQL Server Developer/Express](https://www.microsoft.com/en-us/sql-server)
* [Git Version Control](https://git-scm.com/)

---

## 🚀 5. Hướng Dẫn Nạp Băng, Tự Động Hóa & Vận Hành

### Bước 1: Khai Lấy Mã Nguồn & Tinh Chỉnh Chuỗi Kết Nối
1. Tải toàn bộ mã nguồn về môi trường:
   ```bash
   git clone <repository-url> digipose
   cd digipose
   ```
2. Mở tệp `backend/DigiPOSE/appsettings.json` (hoặc khởi tạo từ `appsettings.example.json`) và thiết lập `DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DigiPOSE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   },
   "CloudflareTurnstile": {
     "SiteKey": "1x00000000000000000000AA",
     "SecretKey": "1x0000000000000000000000000000000AA"
   }
   ```

### Bước 2: Kích Hoạt Gói Bổ Trợ & Thực Thi Tái Cấu Trúc CSDL
1. Đồng bộ thư viện NuGet:
   ```powershell
   cd backend/DigiPOSE
   dotnet restore
   ```
2. Biên dịch xác thực mã nguồn:
   ```powershell
   dotnet build --nologo -v q
   ```
3. Cập nhật thực thể cơ sở dữ liệu và nạp dữ liệu gốc ban đầu:
   ```powershell
   dotnet ef database update
   ```

### Bước 3: Kích Hoạt Động Cơ Máy Chủ Backend & API
Khởi chạy hệ máy chủ ASP.NET Core:
```powershell
dotnet run
```
Các tuyến cổng giao tiếp tiêu chuẩn:
* **Trung Tâm Điều Hành CMS**: `http://localhost:5128/Administrator`
* **Trạm Thu Ngân Trực Tiếp**: `http://localhost:5128/POS`
* **Cổng Gateway POS REST API**: `http://localhost:5128/api/v1/pos/products`
* **Cổng Gateway Storefront REST API**: `http://localhost:5128/api/v1/Storefront/user-identity`

---

### Bước 4: Kích Hoạt Giao Diện Khách Hàng Next.js Frontend
Mở một Terminal xử lý song ngữ riêng rẽ:
1. Di chuyển vào thư mục giao diện:
   ```powershell
   cd frontend
   ```
2. Tải mô-đun thư viện:
   ```powershell
   npm install
   ```
3. Nạp động cơ ứng dụng phía khách:
   ```powershell
   npm run dev
   ```
Các tuyến kết nối ứng dụng Web:
* **Cổng Thương Mại Storefront**: `http://localhost:3000/`
* **Cổng Bán Lẻ POS Cashier Terminal**: `http://localhost:3000/pos`
* **Kênh Điều Phối Giỏ Hàng & Chốt Đơn**: `http://localhost:3000/cart`

---

## 🏗 6. Hướng Dẫn Đóng Gói Triển Khai Môi Trường Production

### Đóng Gói Phân Hội Backend Server
```powershell
cd backend/DigiPOSE
dotnet publish -c Release -o ./publish
```

### Kết Xuất Gói Ứng Dụng Frontend Client
```powershell
cd frontend
npm run build
npm run start --port 3000
```

---

## 🔐 7. Tiêu Chuẩn Bảo Mật & Phòng Hộ An Ninh Dữ Liệu
* **Cô Lập Mật Khẩu (Secret Isolation)**: Toàn bộ khóa API Turnstile, chuỗi JWT và cấu hình máy chủ SMTP được niêm phong hoàn toàn trong thẻ loại trừ `.gitignore`, thay thế an toàn bằng hồ sơ `.example`.
* **Phòng Ngự Leo Thang Quyền (IDOR & Tenant Isolation)**: Các tuyến API kiểm chứng chữ ký JWT Bearer Token, chặn tuyệt đối truy cập ngang hàng vi phạm ranh giới đối tác.
* **Giao Dịch ACID Nguyên Tử**: Luồng thanh toán của khách và chốt sổ POS thi hành trong khối `BeginTransactionAsync`, đi Kèm tự động `Rollback` khi đứt đoạn, bảo đảm 0.00% sai lệch dữ liệu tài chính.
