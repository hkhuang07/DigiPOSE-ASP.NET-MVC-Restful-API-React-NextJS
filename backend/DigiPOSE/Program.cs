global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Authorization;
using DigiPOSE.Models;
using DigiPOSE.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DigiPOSE.Services;
using DigiPOSE.Services.Background;
using DigiPOSE.Hubs;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
 
// Sử dụng DbContextPooling để tái sử dụng DbContext Instance, giảm tối đa chi phí cấp phát bộ nhớ (GC Pressure) khi xử lý hàng ngàn request/giây
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DigiPoseConnection") 
    ?? builder.Configuration.GetConnectionString("DigiPoseDbContext");

builder.Services.AddDbContextPool<DigiPoseDbContext>(options => 
    options.UseSqlServer(connectionString));

// >>> [LEAN_LAN_ARCHITECTURE_SERVICES]: Singleton Lazy-Loading RAM Stock Engine, VAT Balancing Engine & Background Resilient Queue
builder.Services.AddSingleton<IInventoryRAMService, InventoryRAMService>();
builder.Services.AddSingleton<IVatBalancingEngine, VatBalancingEngine>();
builder.Services.AddScoped<IInventoryLedgerService, InventoryLedgerService>();
builder.Services.AddSingleton(Channel.CreateUnbounded<JobQueueItem>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }));
builder.Services.AddHostedService<ResilientInvoiceWorker>();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "DigiPOSE.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Forbidden";
});

builder.Services.AddAuthorization(options =>
{
    var permissions = new[] {
        "System.Config.Manage", "System.Branch.Manage", "System.Role.Manage", "System.User.Manage",
        "POS.Shift.Open", "POS.Shift.Close", "POS.Order.Create", "POS.Order.Void", "POS.Discount.Apply",
        "Warehouse.Inventory.View", "Warehouse.Voucher.Create", "Warehouse.Voucher.Approve", "Warehouse.Inventory.Adjust", "Warehouse.Supplier.Manage",
        "Catalog.Product.Manage", "Catalog.Category.Manage", "Catalog.Price.Manage",
        "Finance.Report.View", "Finance.Invoice.View", "Finance.Audit.Export"
    };
    
    foreach (var p in permissions)
    {
        options.AddPolicy(p, policy => policy.RequireClaim("Permission", p));
    }
});
builder.Services.AddHttpContextAccessor();

// Configure CORS for Next.JS Storefront & POS Frontend 
builder.Services.AddCors(options =>
{
    options.AddPolicy("StorefrontCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddTransient<IMailLogic, MailLogic>();
// Lấy thông tin cấu hình trong tập tin appsettings.json và gán vào đối tượng MailSettings
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseCors("StorefrontCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "administratorarea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<PosRealtimeHub>("/hubs/pos");

app.Run();
