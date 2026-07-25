DIGIPOSE - MASTER ARCHITECTURE & FUNCTIONAL BLUEPRINT
I. TẦM NHÌN VÀ ĐỊNH VỊ DỰ ÁN (PROJECT VISION & B2B/RETAIL POSITIONING)
DigiPOSE (Digital Point of Sale Enterprise) là hệ thống Quản trị Nguồn lực & Điểm bán hàng song nghị trục (ERP/PoS & E-Commerce Storefront - B2B/B2C). Hệ thống được thiết kế đa năng để giải quyết toàn diện hai phương thức kinh doanh cốt lõi:
1. Giao dịch trực tiếp tại quầy bán lẻ (In-Store High-Frequency POS Terminal).
2. Giao dịch đặt hàng & thuê bao dịch vụ trực tuyến qua Website Cổng thông tin (Online E-Commerce & SaaS Storefront Portal).

Mục tiêu cốt lõi: Tốc độ giao dịch tại quầy đạt mức mili-giây (O(1) lookup), tích hợp chuẩn SEO và bộ tra cứu động đa trường trên cổng web trực tuyến, hiển thị 100% nội dung giao diện web bằng tiếng Anh (English Standard), toàn vẹn dữ liệu lịch sử tuyệt đối, bảo mật đa tầng và sẵn sàng mở rộng theo chiều ngang (Scale-out).

II. KIẾN TRÚC HỆ THỐNG HYBRID SONG NGHỊ TRỤC (DECOUPLED DUAL-SALES ARCHITECTURE)
Dự án ứng dụng kiến trúc kết hợp (Hybrid Decoupled), chia cắt rõ ràng ranh giới giữa Quản trị viên CMS và hai tập người dùng vận hành/khách mua bán hàng:

1. Khu vực Admin CMS (Server-Side Rendering - Backoffice ERP):
- Công nghệ: ASP.NET Core MVC 8.0/10.0 (Razor Views + Cyber-Cinematic Military HUD Design System).
- Mục đích: Xây dựng giao diện quản lý siêu tốc cho Quản lý / Admin bằng Scaffolding. Xử lý các tác vụ quản trị danh mục (CRUD Master Data - 26 Bảng), cấu hình hệ thống, theo dõi radar doanh thu.
- Xác thực (Auth): Sử dụng Cookie Authentication (Stateful) với vòng đời theo ca làm việc, phân tầng bảo mật tối cao qua bộ nhận dạng quyền hạn Claims & Roles.

2. Hai Phân Hệ Bán Hàng Trái Tim (Dual Sales Frontend - React/Next.JS via RESTful API):
Hệ thống bố trí 2 kênh bán hàng song song, tích hợp liên kết trực tiếp trong bộ điều hướng Sidebar (MODULE 5: LINKS & TERMINALS -> `Launch POS Machine` & `Online Storefront`):

- **Kênh 1: POS Machine Terminal (Máy Thu Ngân Vận Hành Tại Quầy):**
  + Công nghệ: Frontend React/Next.JS kết nối ASP.NET Core Web API (JSON).
  + Đặc thù nghiệp vụ: Tần suất cực cao (High-Frequency). Ghi đè vào CSDL thông qua đơn hàng nháp trực tiếp (`Order` với `StatusId = 4` Retail Draft). Không bao giờ dùng bộ nhớ Session tạm để chống sập điện tại quầy, tích hợp máy quét mã vạch O(1) Barcode Scanner và khóa ca két tiền mặt.
  
- **Kênh 2: Online Storefront Web Portal (Cổng Bán Hàng & Gia Hạn SaaS Trực Tuyến):**
  + Công nghệ: Frontend React/Next.JS (App Router SSR) kết nối ASP.NET Core Web API.
  + Đặc thù nghiệp vụ: Phát huy toàn bộ hoa tiêu của bài học Thực hành Buổi 6 (Shopping Cart, Checkout, Custom Orders) trên một nền tảng giao diện React/Next.JS hiện đại và đẳng cấp (thay vì Razor SSR/Cartzilla truyền thống).
  + Khác biệt cốt lõi với POS: Tránh rác nghiệp vụ vào sổ sách kế toán. Cửa hàng online sử dụng kiến trúc **Database-Backed Shopping Cart** riêng biệt (`ShoppingCartItems`), chỉ chuyển hóa thành `Order` chính thức (Trạng thái `Pending Payment / Awaiting Processing`) sau khi Khách hàng bấm chốt Checkout!

3. Xác thực kép & Token Economy: 
Khu vực API sử dụng JWT (JSON Web Token - Stateless) truyền qua header Authorization: Bearer, cho phép người dùng đăng nhập đồng nhất giữa Web Online Storefront và trạm POS, tích hợp phân quyền linh hoạt theo quy tắc tối cao (Super Admin vào được mọi trạm thu ngân của operator).

III. THIẾT KẾ CƠ SỞ DỮ LIỆU ĐA TẦNG (16 TABLES - 3NF)
Hệ thống sử dụng SQL Server và Entity Framework Core, được chuẩn hóa 3NF và chia thành 4 Bounded Contexts. Toàn bộ hệ thống bị vô hiệu hóa tính năng Cascade Delete bằng Fluent API (DeleteBehavior.Restrict) để bảo vệ dữ liệu lịch sử.

1. Phân hệ Cấu Hình & Nhân Sự (IAM & Org)
1. Branches (Chi nhánh): Mỏ neo của toàn bộ dữ liệu. Mọi giao dịch, nhân viên, tồn kho đều phải gắn với một chi nhánh.

2. Roles (Vai trò): Quản lý phân quyền (Admin, Branch Manager, Cashier).

3. Users (Nhân sự): Chứa thông tin đăng nhập, PasswordHash (mã hóa BCrypt), liên kết khóa ngoại với BranchId và RoleId.

4. Counters (Quầy thu ngân): Máy PoS vật lý tại chi nhánh (BranchId).

5. Shifts (Ca làm việc): Quản lý dòng tiền phiên giao dịch. Lưu thời gian mở/đóng, StartCash (Tiền đầu ca), EndCash (Tiền cuối ca). Liên kết với UserId và CounterId.

2. Phân hệ Đối Tác (Partners)
6. CustomerTypes (Loại khách hàng): Phân loại Khách lẻ, VIP, Doanh nghiệp.

7. Customers (Khách hàng): Quản lý định danh qua Số điện thoại, lưu trữ RewardPoints (Điểm thưởng CRM).

8. Suppliers (Nhà cung cấp): Thông tin đối tác nhập hàng, quản lý DebtBalance (Công nợ).

3. Phân hệ Từ Điển & Tồn Kho (Catalog & Inventory)
9. Categories (Danh mục): Phân loại hàng hóa.

10. Units (Đơn vị tính): Cái, Hộp, Kg.

11. Products (Hàng hóa cốt lõi): Chứa SKU (Mã vạch - Unique Index), BasePrice. Tích hợp [Timestamp] RowVersion để chống xung đột dữ liệu (Optimistic Concurrency).

12. ProductInventories (Tồn kho thực tế): Bảng trung gian giải quyết bài toán đa chi nhánh (1 Sản phẩm - N Chi nhánh). Chứa StockQuantity (Tồn thực tế) và MinStockLevel (Mức cảnh báo).

13. StockVouchers (Chứng từ kho): Lưu thông tin nhập/xuất/kiểm kê. Chứa VoucherType (Import, Export, Return), TotalValue, liên kết với SupplierId (nếu là phiếu nhập).

14. StockVoucherDetails (Chi tiết chứng từ): Cấu trúc cha-con với StockVouchers, lưu Quantity và ActualPrice (Giá nhập thực tế tại thời điểm lập phiếu).

4. Phân hệ Giao Dịch Bán Hàng (Sales Core)
15. Orders (Hóa đơn/Đơn hàng): Giao dịch tổng. Trạng thái (Draft, Completed, Cancelled). Liên kết với ShiftId, UserId và CustomerId.

16. OrderDetails (Chi tiết hóa đơn): Lưu các dòng sản phẩm bán ra. Ràng buộc sinh tử: Giá bán (UnitPrice) và Tên ĐVT (UnitName) phải được COPY cứng từ Products sang đây tại thời điểm bán để bảo toàn báo cáo doanh thu nếu giá gốc thay đổi trong tương lai.

IV. BẢN ĐỒ RÀNG BUỘC QUAN HỆ (ENTITY RELATIONSHIPS)
(1 - N):

Branch -> User, Counter, ProductInventory, StockVoucher.

Role -> User.

User -> Shift, StockVoucher, Order.

Counter -> Shift.

Shift -> Order (Toàn bộ doanh thu trong ca được tổng hợp từ đây).

CustomerType -> Customer.

Customer -> Order.

Supplier -> StockVoucher.

Category -> Product.

Unit -> Product.

Product -> ProductInventory, StockVoucherDetail, OrderDetail.

StockVoucher -> StockVoucherDetail.

Order -> OrderDetail.

V. ĐẶC TẢ YÊU CẦU CHỨC NĂNG CHI TIẾT (FUNCTIONAL REQUIREMENTS)
1. Phân hệ Quản lý Hàng hóa & Tồn kho (Catalog & Inventory Management)
Quản lý Danh mục & Sản phẩm: CRUD Sản phẩm, thiết lập Mã vạch (SKU), Đơn giá, Đơn vị tính. Quét mã vạch nhanh bằng API GET /api/v1/products/scan-barcode/{sku} trả về kết quả O(1).

Quản lý Nhập Kho (Import): Lập StockVoucher với Type = "Import". Bắt buộc liên kết với SupplierId. Khi phiếu hoàn tất, hệ thống kích hoạt transaction cộng dồn StockQuantity vào bảng ProductInventories tương ứng với BranchId. Tính toán lại trung bình giá vốn.

Quản lý Xuất Kho / Hủy hàng (Export/Dispose): Lập StockVoucher với Type = "Export". Không cần SupplierId. Trừ StockQuantity trực tiếp khỏi kho.

Quản lý Trả hàng (Return): Khách hàng hoàn trả hoặc xuất trả Nhà cung cấp. Sử dụng StockVoucher với Type = "Return", kèm theo tham chiếu ghi chú.

Quản lý Đợt hàng (Batch Management qua Chứng từ): Hệ thống không dùng bảng "Lô/Đợt" độc lập để tránh thắt cổ chai hiệu năng. Thay vào đó, "Đợt hàng" được quản lý trực tiếp thông qua Mã Chứng từ nhập kho (StockVoucherId). Có thể truy xuất chính xác đợt hàng nhập ngày nào, của nhà cung cấp nào, số lượng bao nhiêu thông qua báo cáo chứng từ.

2. Phân hệ Quản lý Ca làm việc & Quầy (Shift & Counter Management)
Quản lý Quầy: Định danh các trạm thu ngân vật lý tại từng chi nhánh.

Đăng ký & Mở Ca làm việc (Open Shift): Thu ngân đăng nhập bằng JWT, chọn Quầy (CounterId), nhập Số tiền mặt hiện có trong két (StartCash) để tạo mới một Shift (Trạng thái: "Open").

Đóng ca & Kết toán (Close Shift): Khi hết ca, hệ thống tính tổng doanh thu từ tất cả Orders thuộc ShiftId đó. Cộng với StartCash sinh ra EndCash (Tiền mặt phải có trên lý thuyết). Thu ngân kiểm đếm tiền thực tế, cập nhật trạng thái ca thành "Closed".

3. Phân hệ Bán hàng tại quầy (Point of Sale Operations)
Tạo Đơn Nháp (Draft Order): Thu ngân quét mã vạch sản phẩm. API tạo một Order với trạng thái "Draft". Mọi cập nhật số lượng (Tăng/Giảm/Xóa món) đều tác động vào OrderDetails của Đơn nháp này. Không dùng Session giỏ hàng, giúp hệ thống không bị mất phiên làm việc nếu sập điện.

Đặt hàng / Khách mua (Checkout):

Thu ngân chọn phương thức thanh toán.

Hệ thống gắn CustomerId (Nếu có) để tích lũy RewardPoints.

Chuyển trạng thái Order sang "Completed".

Khởi chạy DB Transaction: Trừ tồn kho (ProductInventories), Ghi nhận doanh thu vào Shift.

Phát hành Hóa đơn (E-Invoice): Tích hợp dịch vụ SMTP MailKit. Sau khi Checkout thành công, trigger luồng Background gửi Biên lai điện tử dạng HTML thẳng vào Email của khách hàng thông qua thông tin từ bảng Customers.

4. Phân hệ Hệ thống Truyền thông & Khách hàng (CRM & Internal System)
Quản trị Tệp Khách hàng (CRM): Cập nhật hạng thành viên, tra cứu lịch sử mua hàng xuyên suốt hệ thống (Lấy 5 Order gần nhất bằng API).

Bảng tin Nội bộ (Announcements): (Thay thế chức năng Blog/Tin tức). Admin sử dụng CKEditor để viết các thông báo vận hành (Khuyến mãi mới, thay đổi chính sách). API đẩy thông báo có cờ IsUrgent lên màn hình máy PoS của nhân viên.

5. Phân hệ Báo cáo & Thống kê (Reporting & Analytics)
Công nghệ áp dụng: Sử dụng thư viện System.Linq.Dynamic.Core kết hợp Server-side DataTables.net.

Thống kê Doanh thu: Gom nhóm (GroupBy) theo BranchId, UserId (Nhân viên), hoặc khoảng thời gian (Ngày/Tháng). Tính tổng FinalTotal từ bảng Orders.

Thống kê Hàng hóa: Báo cáo tồn kho dựa trên ProductInventories. Lọc các sản phẩm có StockQuantity <= MinStockLevel để cảnh báo nhập hàng.

Tra soát Chứng từ: Tìm kiếm động mọi phiếu Nhập/Xuất kho theo khoảng thời gian, theo Nhà cung cấp, hoặc theo ID chứng từ.

VI. TIÊU CHUẨN KỸ THUẬT VÀ BẢO MẬT (TECHNICAL & SECURITY STANDARDS)
Phòng chống Deadlock (Concurrency): Bảng Products được gắn mỏ neo [Timestamp] byte[] RowVersion. Bất kỳ giao dịch nào sửa đổi giá gốc sẽ bị chặn (DbUpdateConcurrencyException) nếu có giao dịch khác đang thực thi đồng thời.

Toàn vẹn Lịch sử (Immutable History): UnitPrice và UnitName lưu dạng snapshot trong OrderDetails và StockVoucherDetails. Không bao giờ có sự cố truy vấn ngược làm thay đổi doanh thu năm cũ.

Mã hóa & Xác thực kép:

Mật khẩu 100% băm bằng BCrypt.

Cookie Auth cho Admin / Quản lý (Trượt phiên sau 8 tiếng).

JWT Bearer cho Frontend PoS (Có thời hạn, cấp qua api/v1/auth/login).

Transaction Nguyên khối (ACID): Quá trình thanh toán (Checkout) được gói trong IDbContextTransaction. Nếu việc trừ tồn kho thành công nhưng cộng tiền vào ca làm việc thất bại, toàn bộ quá trình sẽ Rollback, không sinh ra rác dữ liệu. Dữ liệu chuẩn bị sẵn sàng cho các hạ tầng CI/CD, GitOps sau này.

VII. TIÊU CHUẨN TRUYỀN THÔNG WEB, NGÔN NGỮ & BỘ TRƯỜNG SEO (UI LANGUAGE & SEO MECHANISMS)
Hệ thống tuân thủ nghiêm ngặt các quy chuẩn quốc tế hóa và tối ưu công cụ tìm kiếm chuẩn Enterprise B2B SaaS:

1. Ngôn Ngữ Trình Diễn (English Standardized Interface):
Toàn bộ các biểu tượng, văn bản nhãn (labels), tiêu đề cột DataTables, thông báo HUD, và tài liệu xuất ra cho người dùng trên nền tảng Web & Trạm POS đều phải hiển thị chuẩn bằng Tiếng Anh (English).

2. Cơ Chế SEO Tối Ưu (Search Engine Optimization tags & REST APIs):
- Tại tầng SSR CMS Admin & Web Layout chung (`_Layout.cshtml`), mã nguồn tích hợp sẵn các cờ SEO nguyên tử cơ sở:
  ```html
  <!-- SEO Meta Tags -->
  <meta name="description" content="DigiPOSE - Next-Generation High-Density Cyber-Cinematic B2B/Retail Point of Sale and E-Commerce Web Storefront." />
  <meta name="keywords" content="POS, ERP, E-Commerce, Retail Portal, SaaS Subscriptions, Shopping Cart, Cyber HUD" />
  <meta name="author" content="DigiPOSE Systems Architecture Team" />
  ```
- Tại tầng RESTful Web API cho Frontend React/Next.JS (App Router): Các endpoint tải chi tiết sản phẩm (`/api/v1/storefront/catalog/products/{slug}`) bắt buộc trả kèm các trường dữ liệu siêu dữ liệu (`MetaTitle`, `MetaDescription`, `MetaKeywords`, `Slug`, `ImageUrl`) để trình biên dịch Next.JS thực thi hàm SSR `generateMetadata`, đem lại khả năng lập chỉ mục (SEO indexing) xuất sắc trên Google và mạng xã hội.

VIII. ĐẶC TẢ HOÀN THIỆN ĐA PHÂN HỆ BÁN HÀNG - POS VÀ ONLINE STOREFRONT (PRODUCTION ORDER & CART DOMAIN LOGIC)
Hệ thống chính thức vận hành trên 2 trụ phân hệ kinh doanh riêng biệt, kết hợp trọn vẹn tinh hoa thực hành từ bài giảng Buổi 6 và thiết kế hệ thống có độ an toàn tuyệt đối của DigiPOSE:

1. Đam Mê Phân Đỉnh (Domain Isolation: POS Draft Order vs. Web Shopping Cart):
- **Trạm POS Thu Ngân (`PosController.cs`):** Dành cho vận hành tại chỗ có kiểm đếm tiền mặt theo ca. Dùng trực tiếp Thực thể `Orders` (với `StatusId = 4` Draft). Đơn hàng nháp của thu ngân là một sự chiếm dụng tài nguyên kho tạm thời, sẵn sàng in biên lai chớp nhoáng dưới 15ms.
- **Cổng Bán Hàng Trực Tuyến (`StorefrontController.cs`):** Dành cho khách mua online, B2B order, hoặc thuê bao SaaS. BẮT BUỘC sử dụng bảng Giỏ hàng chuyên biệt (`ShoppingCartItems` hoặc hybrid repository), TRÁNH TUYỆT ĐỐI việc lưu giỏ hàng đang nháp vào bảng `Orders` gây bẩn số liệu kế toán hầm khổng lồ từ các xe giỏ hàng bỏ quên (Abandoned Carts).

2. Danh Sách Nghiệp Vụ Chuẩn Hóa Khung Xương (Core Sales & Cart Methods Standard):
Cả hai phân hệ POS và Online Web chia sẻ hệ quy trình định dạng phương thức động lực học, phơi bày trọn vẹn qua REST JSON API cho client React/Next.JS:
- **Xác định định danh Người mua & Trợ lý:** `getUsername()` / `getCustomerIdentity()` (Truy xuất thông tin JWT Token hoặc `CustomerId` từ CRM, hiển thị Hạng VIP và Điểm thưởng RewardPoints).
- **Tính toán trạng thái giỏ & Thuế tự động:** `getShoppingCart()` (Trả về cấu trúc Cart hoặc Draft Order kèm Trạng thái giỏ `Card` hoặc `CardEmpty`), `getTotalQuantity()` (Tổng số món có trong giỏ), `getTotalPrice()` (Tích hợp động: Giá bán trước thuế `Gross`, % thuế TaxRate `TaxAmount`, Chiết khấu CRM `DiscountAmount`, và tổng chốt thanh toán `TotalAmount`).
- **Nghiệp vụ Xử lý Hàng hóa & Căn chỉnh giỏ (Line Item Manipulation):**
  + `addItem()` / `addToCart()` (Thêm sản phẩm mới bằng `ProductId` hoặc `SKU`, tự động dồn chung dòng nếu trùng mã, kế thừa giá bán `BasePrice` từ catalog thời gian thực).
  + `updateQuantity()` / `increaseProduct()` / `decreaseProduct()` (Tăng/giảm số lượng tự động tính lại thuế và tiền món hàng).
  + `removeItem()` / `deleteProduct()` (Xóa từng dòng khỏi giỏ hàng/đơn nháp).
  + `removeAllItems()` / `clearCart()` (Xóa sạch giỏ hàng khi người mua chọn Hủy hoặc làm rỗng giỏ).
  + `updateProduct()` / `applyCustomDiscount()` (Điều chỉnh đặc Quyền giá cho các món SaaS, hoặc cập nhật lựa chọn đơn vị tính/thuế theo thẩm quyền thu ngân/quản lý).
- **Giao dịch Chốt chốt chặn (Atomic Checkout & POS Execution):**
  + Lệnh `checkout()` / `paid()`: Biến đổi Cart/Draft thành Hóa đơn ghi sổ chính thức (`Order` với trạng thái `Completed` hoặc `Awaiting Payment`). Mở rộng hạn thuê bao `Subscriptions` nối tiếp và trừ tồn kho vật lý qua rào cản ACID Transaction Serializable/ExecuteUpdate.
- **Truy xuất SEO & Bộ Lọc Tốc Độ Cao (Dynamic Catalog Filter & SEO Search):**
  + API Tìm kiếm đa trường `/api/v1/storefront/catalog/search` hỗ trợ đồng thời các bộ lọc: Tìm theo Từ khóa (Tên/SKU/Slug), Bộ lọc Nhà sản xuất (`ManufacturerId`), Bộ lọc Danh mục (`CategoryId`), Bộ lọc Loại hình hàng (`ProductTypeId`), Bản chất hàng (`ItemNatureId`), Khoảng giá và Trạng thái hàng trong kho (`StockQuantity > 0`). Output JSON đồng bộ kèm cờ SEO.