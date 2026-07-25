# DigiPOSE - Nền Tảng Quản Trị Hệ Thống POS & ERP Bán Lẻ Doanh Nghiệp
**Tài Liệu Kiến Trúc & Hướng Dẫn Triển Khai Kỹ Thuật (v1.0.0)**

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
 ├── Quản Lý Dữ Liệu Gốc (26 Bảng)       ├── Quản Trị Ca & Giao Dịch POS
 ├── Báo Cáo Tài Chính & Doanh Thu       ├── Tra Cứu Danh Mục & Thanh Toán Web
 └── Phân Quyền Vai Trò Người Dùng       └── Hàng Đợi Gửi Hóa Đơn Tự Động (SMTP)
         │                                         │
         └────────────────────┬────────────────────┘
                              │
                (Entity Framework Core 10)
                              ▼
               [ HỆ PHẦN CHẤT CSDL SQL SERVER ]
```

### Đặc Điểm Kiến Trúc Cốt Lõi:
* **Mô Hình Bán Hàng Kép (Dual Sales Architecture)**: Vận hành đồng thời máy bán hàng POS tại các chi nhánh và cổng bán hàng web trực tuyến mà không gây ô nhiễm rác dữ liệu kế toán. Giỏ hàng trực tuyến được lưu đệm phi kế toán và chỉ ghi nhận vào bảng hóa đơn doanh thu khi hoàn tất thủ tục chốt đơn (Checkout).
* **Tối Ưu Kết Nối Hồ Bể (DbContextPooling)**: Ứng dụng công nghệ tái sử dụng liên kết cơ sở dữ liệu nhằm tiết kiệm bộ nhớ máy chủ (GC Pressure) và bảo đảm tốc độ phản hồi cao dưới công suất thanh toán tải nặng.
* **Tự Động Hóa Tối Ưu SEO & SSR**: Hệ thống Next.js App Router render giao diện trực tiếp tại phía máy chủ, nén dữ liệu metadata cấu trúc vào từng node sản phẩm bán lẻ hoặc bản quyền phần mềm SaaS.
* **Bảo Toàn Lịch Sử Giao Dịch**: Dữ liệu hóa đơn, giá trị thanh toán, chiết khấu và danh tính của người bán và người mua được khắc ghi tĩnh vào nhật ký bán hàng tại thời điểm lập đơn, bảo đảm tính minh bạch khi kiểm toán doanh thu.

---

## 📁 2. Cấu Trúc Mã Nguồn Dự Án

Mã nguồn được tổ chức chặt chẽ theo tiêu chuẩn quy hoạch thư mục chuyên nghiệp của các doanh nghiệp B2B ERP:

```
digipose/
├── backend/                  # Nền tảng Máy chủ & Cổng kết nối (.NET SDK 10.0)
│   └── DigiPOSE/             # Dự án chính ASP.NET Core MVC & RESTful Web API
│       ├── Areas/            # Phân vùng Quản trị nội bộ CMS (Administrator Area)
│       ├── Controllers/      # Bộ điều hướng Web MVC & Cổng API (Controllers/Api/)
│       ├── Models/           # Thực thể CSDL, DbContext và Định nghĩa DTOs
│       ├── Services/         # Xử lý luồng nghiệp vụ & Dịch vụ gửi email MaiKit
│       ├── Views/            # Giao diện SSR Razor và cấu trúc bố cục POS
│       └── wwwroot/          # Thư viện Stylesheet tĩnh và thư mục chứa tệp truyền thông
├── frontend/                 # Ứng dụng Giao diện Khách hàng (Node.js v20+)
│   ├── app/                  # Bố cục routing, giao diện và cấu hình cơ sở Next.js 15
│   ├── components/           # Bộ tái sử dụng thành phần giao diện theo module
│   ├── services/             # Bộ điều hợp kết nối API và cổng dịch vụ Axios
│   ├── store/                # Bộ quản lý trạng thái tĩnh máy khách (Zustand Stores)
│   └── types/                # Hệ thống từ điển cấu trúc DTO TypeScript
├── docs/                     # Tài liệu thông số kỹ thuật và bản thảo nghiệp vụ hệ thống
└── asset/                    # Hình ảnh sơ đồ, biểu tượng thương mại và dữ liệu bổ trợ
```

---

## 💻 3. Công Nghệ Nền Tảng & Yêu Cầu Môi Trường

### Danh Mục Công Nghệ (Tech Stack)
* **Backend Runtime**: .NET 10.0 SDK, ASP.NET Core MVC, ASP.NET Core Web API.
* **Cơ Sở Dữ Liệu**: Microsoft SQL Server 2022+, Entity Framework Core 10, System.Linq.Dynamic.Core.
* **Frontend Runtime**: Node.js v20+, Next.js 15, React 19, TypeScript, Tailwind CSS v4, PostCSS.
* **Quản Trị Trạng Thái**: Zustand State Manager, TanStack React Query, Axios HTTP Client.
* **Bảo Mật & Dịch Vụ**: BCrypt.Net-Next (Mã hóa Hash mật khẩu), MailKit (Cửa điểu SMTP Hóa đơn điện tử), Stateless JWT Bearer & Secure Http-Only Cookie Authentication.

### Yêu Cầu Môi Trường Cài Đặt (Prerequisites)
Để biên dịch và khởi chạy dự án tại máy trạm nội bộ, cần kiểm tra cài đặt các bộ công cụ kỹ thuật sau:
* [Bộ công cụ phát triển .NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js (Phiên bản từ v20.x trở lên) & Trình quản lý gói npm](https://nodejs.org/)
* [Microsoft SQL Server (2019 hoặc 2022) - Bản Express hoặc Developer](https://www.microsoft.com/en-us/sql-server)
* [Hệ thống quản lý phiên bản Git](https://git-scm.com/)

---

## 🚀 4. Hướng Dẫn Biên Dịch, Cài Đặt & Vận Hành Cụ Thể

Thực hiện tuần tự các bước dưới đây để thiết lập CSDL, biên dịch mã nguồn và chạy thử máy chủ cục bộ.

### Bước 1: Trích Xuất Mã Nguồn & Cấu Hình Kết Nối CSDL
1. Tải về kho lưu trữ hệ thống:
   ```bash
   git clone <repository-url> digipose
   cd digipose
   ```
2. Di chuyển vào không gian thư mục máy chủ và mở tệp cấu hình `appsettings.json`:
   ```bash
   cd backend/DigiPOSE
   ```
3. Cập nhật tham số chuỗi kết nối `DefaultConnection` phù hợp với máy chủ SQL Server của bạn:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DigiPOSE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

### Bước 2: Cài Đặt Package Cấu Hình & Tạo Bảng CSDL
1. Nạp và giải quyết các gói phụ thuộc Nuget Package cho dự án C#:
   ```powershell
   dotnet restore
   ```
2. Biên dịch nhanh để kiểm nghiệm toàn vẹn mã nguồn:
   ```powershell
   dotnet build --nologo -v q
   ```
3. Chạy câu lệnh EF Core Migration để nạp toàn bộ cấu trúc bảng và dữ liệu mẫu (Seeding Data) vào CSDL:
   ```powershell
   dotnet ef database update
   ```
   *(Lưu ý: Nếu máy tính chưa có thư viện dòng lệnh EF Core CLI, thi hành lệnh cài đặt trước: `dotnet tool install --global dotnet-ef`)*

### Bước 3: Khởi Chạy Máy Chủ Backend ASP.NET Core MVC & Web API
Khởi chạy tiến trình Web Server tại thư mục `backend/DigiPOSE`:
```powershell
dotnet run
```
Máy chủ tự động mở kênh phục vụ tại cổng `5128` (hoặc cổng cấu hình trong launchSettings). Tra cứu hệ thống qua trình duyệt:
* **Trang Điều Hành CMS Backoffice**: `http://localhost:5128/Administrator`
* **Cổng Kiểm Nghiệm MVC Storefront**: `http://localhost:5128/Storefront`
* **Điểm Trục Giao Thức REST API**: `http://localhost:5128/api/v1/Storefront/user-identity`

---

### Bước 4: Cài Đặt & Khởi Chạy Frontend Next.js Client SPA
Mở một cửa sổ Terminal ngoài (External Terminal) song song và chuyển sang phân khu giao diện frontend:
1. Di chuyển vào thư mục giao diện Web Client:
   ```powershell
   cd d:\Study\ASP_Web_Technology\Project\digipose\frontend
   ```
2. Cài đặt toàn bộ bộ từ điển NodeJS và cấu hình PostCSS / Tailwind CSS:
   ```powershell
   npm install
   ```
3. Bật máy chủ giao diện ở chế độ Development:
   ```powershell
   npm run dev
   ```
Hệ thống Next.js sẽ biên dịch và túc trực nhận request tại cổng `3000`:
* **Trang Chủ Bán Hàng Trực Tuyến & SaaS**: `http://localhost:3000/`
* **Trạm Bán Hàng Quầy POS Thu Ngân**: `http://localhost:3000/pos`
* **Khu Trục Kiểm Khám Giỏ Hàng**: `http://localhost:3000/cart`

---

## 🏗 5. Quy Trình Đóng Gói Triển Khai Production

Khi đưa hệ thống lên các trạm vận hành chính thức hoặc máy chủ cụm mây Enterprise, áp dụng quy chuẩn build sau:

### Đóng Gói Máy Chủ Backend (.NET Release Publish)
Biên dịch ra khối đóng gói nguyên tản để phục vụ việc liên kết cùng Docker Container hoặc kestrel reverse-proxy IIS/Nginx:
```powershell
cd backend/DigiPOSE
dotnet publish -c Release -o ./publish
```

### Đóng Gói Frontend Web App (Next.js SSR & Static Optimize)
Hệ thống tạo ra thư mục phân bổ tĩnh `.next` tối ưu dung lượng và tốc độ phân phát qua màng CDN:
```powershell
cd frontend
npm run build
npm run start --port 3000
```

---

## 🔐 6. Kỷ Luật Bảo Mật & Tiêu Chuẩn Giám Sát Enterprise
* **Cô Lập Tham Số Bảo Mật**: Tuyệt đối không cho phép đẩy các tài khoản quản trị SQL Server, secret keys JWT hoặc API MailKit lên hệ thống lưu trữ mã nguồn Git. Mọi giá trị nhạy cảm được quản lý lập trình riêng tại `.env` và `appsettings.Production.json` nằm ngoài phạm vi `.gitignore`.
* **Phân Lớp Khách Hàng (Tenant Isolation)**: Mọi endpoint thuộc nhóm dịch vụ thao tác dữ liệu đều tuân thủ nguyên tắc xác minh token và giải mã định danh, chống hoàn toàn nguy cơ truy cập chéo tham số IDOR.
* **Giao Dịch ACID Kép**: Thao tác chốt đơn trực tuyến (Checkout) được đóng khung an toàn trong lệnh rào chắn CSDL transaction (`BeginTransactionAsync`), sẵn sàng hoàn tác (Rollback) 100% nếu có chấn động mất mát đường truyền hoặc xung đột tồn kho phát sinh.
