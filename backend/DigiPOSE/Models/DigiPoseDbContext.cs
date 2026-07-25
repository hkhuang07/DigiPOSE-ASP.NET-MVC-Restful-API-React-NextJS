using Microsoft.EntityFrameworkCore;

namespace DigiPOSE.Models
{
    public class DigiPoseDbContext : DbContext
    {
        public DigiPoseDbContext(DbContextOptions<DigiPoseDbContext> options) : base(options) { }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SystemModule> SystemModules { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PermissionRole> PermissionRoles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Counter> Counters { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftStatus> ShiftStatuses { get; set; }

        public DbSet<CustomeType> CustomerTypes { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<TaxType> TaxTypes { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ItemNature> ItemNatures { get; set; }
        
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductInventory> ProductInventories { get; set; }
        public DbSet<StockVoucher> StockVouchers { get; set; }
        public DbSet<StockVoucherDetail> StockVoucherDetails { get; set; }

        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceStatus> InvoiceStatuses { get; set; }
        public DbSet<InvoiceType> InvoiceTypes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ====================================================================
            // 0. EXPLICIT TABLE MAPPING (ARCHITECTURE STANDARDS)
            // Đảm bảo chính xác 28 Bảng trong Model hiện tại, không phụ thuộc Convention
            // ====================================================================
            modelBuilder.Entity<Branch>().ToTable("Branches");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<SystemModule>().ToTable("SystemModules");
            modelBuilder.Entity<Permission>().ToTable("Permissions");
            modelBuilder.Entity<PermissionRole>().ToTable("PermissionRoles")
                .HasKey(pr => new { pr.RoleId, pr.PermissionId });
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Counter>().ToTable("Counters");
            modelBuilder.Entity<Shift>().ToTable("Shifts");
            modelBuilder.Entity<ShiftStatus>().ToTable("ShiftStatuses");
            
            modelBuilder.Entity<CustomeType>().ToTable("CustomerTypes");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Unit>().ToTable("Units");
            modelBuilder.Entity<Manufacturer>().ToTable("Manufacturers");
            modelBuilder.Entity<TaxType>().ToTable("TaxTypes");
            modelBuilder.Entity<ProductType>().ToTable("ProductTypes");
            modelBuilder.Entity<ItemNature>().ToTable("ItemNatures");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductInventory>().ToTable("ProductInventories");
            modelBuilder.Entity<StockVoucher>().ToTable("StockVouchers");
            modelBuilder.Entity<StockVoucherDetail>().ToTable("StockVoucherDetails");
            
            modelBuilder.Entity<OrderStatus>().ToTable("OrderStatuses");
            modelBuilder.Entity<PaymentMethod>().ToTable("PaymentMethods");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
            
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<InvoiceStatus>().ToTable("InvoiceStatuses");
            modelBuilder.Entity<InvoiceType>().ToTable("InvoiceTypes");
            modelBuilder.Entity<Subscription>().ToTable("Subscriptions");

            // 1. CHỐNG XÓA DÂY CHUYỀN (NO CASCADE DELETE) - BẢO VỆ DỮ LIỆU KẾ TOÁN
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 2. TỐI ƯU CƠ CHẾ CONCURRENCY CONTROL (OPTIMISTIC CONCURRENCY)
            modelBuilder.Entity<Product>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<ProductInventory>().Property(p => p.RowVersion).IsRowVersion();

            // 3. TỐI ƯU HIỆU NĂNG - INDEXING STRATEGY (SHARDING & LOW LATENCY)
            // Bảng Orders: Tránh Last Page Latch Contention khi có hàng ngàn Insert/giây
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId).IsClustered(false);
            modelBuilder.Entity<Order>().HasIndex(o => new { o.BranchId, o.CreatedAt }).IsClustered(true);
            modelBuilder.Entity<Order>().HasIndex(o => o.ShiftId);

            // Bảng ProductInventory: Unique Index phức hợp (1 SP/1 Chi nhánh)
            modelBuilder.Entity<ProductInventory>()
                .HasIndex(pi => new { pi.BranchId, pi.ProductId })
                .IsUnique();

            // Unique Indexes (Catalog Names & Codes)
            modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
            
            modelBuilder.Entity<Category>().HasIndex(c => c.CategoryName).IsUnique();
            modelBuilder.Entity<Manufacturer>().HasIndex(m => m.ManufacturerName).IsUnique();
            modelBuilder.Entity<Unit>().HasIndex(u => u.UnitName).IsUnique();
            modelBuilder.Entity<TaxType>().HasIndex(t => t.TaxName).IsUnique();
            modelBuilder.Entity<Supplier>().HasIndex(s => s.SupplierName).IsUnique();
            modelBuilder.Entity<ShiftStatus>().HasIndex(s => s.StatusName).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionName).IsUnique();
            modelBuilder.Entity<ProductType>().HasIndex(p => p.TypeName).IsUnique();
            modelBuilder.Entity<PaymentMethod>().HasIndex(p => p.MethodName).IsUnique();
            modelBuilder.Entity<OrderStatus>().HasIndex(o => o.StatusName).IsUnique();
            modelBuilder.Entity<ItemNature>().HasIndex(i => i.NatureName).IsUnique();
            modelBuilder.Entity<InvoiceType>().HasIndex(i => i.TypeName).IsUnique();
            modelBuilder.Entity<InvoiceStatus>().HasIndex(i => i.StatusName).IsUnique();
            modelBuilder.Entity<CustomeType>().HasIndex(c => c.TypeName).IsUnique();
            modelBuilder.Entity<Counter>().HasIndex(c => c.CounterName).IsUnique();
            modelBuilder.Entity<Branch>().HasIndex(b => b.BranchName).IsUnique();
            
            // Tìm kiếm nhanh
            modelBuilder.Entity<Customer>().HasIndex(c => c.PhoneNumber);
            modelBuilder.Entity<Customer>().HasIndex(c => c.TaxCode);
            modelBuilder.Entity<Subscription>().HasIndex(s => new { s.CustomerId, s.ProductId });

            // 4. CHUẨN HÓA TỰ ĐỘNG DECIMAL (CHỐNG LỖI ROUND-OFF KẾ TOÁN)
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                if (property.Name.Contains("Amount") || property.Name.Contains("Cash") || property.Name.Contains("Total") || property.Name.Contains("Gross"))
                {
                    property.SetColumnType("decimal(18,4)");
                }
                else
                {
                    property.SetColumnType("decimal(18,4)");
                }
            }
            
            // 5. Seed Initial Data
            modelBuilder.SeedData();
        }
    }
}
