using iameewh.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using iameewh.Hubs;
using iameewh.Utility;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH DỊCH VỤ CƠ BẢN
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSignalR(); // Kích hoạt ChatHub cho nẫu

// 2. CẤU HÌNH CƠ SỞ DỮ LIỆU SQLITE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=KimiMarket.db"));

// 3. CẤU HÌNH IDENTITY (Xác thực & Phân quyền)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

// Trỏ đường dẫn Đăng nhập/Đăng xuất vô đúng khu vực Buyer cho khách
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Buyer/Account/Login";
    options.LogoutPath = $"/Buyer/Account/Logout";
    options.AccessDeniedPath = $"/Buyer/Account/AccessDenied";
});

// 4. CẤU HÌNH SESSION (Để làm Giỏ hàng hông bị mất đồ)
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 5. CẤU HÌNH ĐƯỜNG ĐI (Middleware Pipeline)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Buyer/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // Phải nằm sau UseRouting và trước MapRoute nghen nẫu

// 6. ĐOẠN GỌI SEED DATA (Tự động tạo quyền Admin/User dới địa chỉ khi chạy web)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();

        DbInitializer.Initialize(context, userMgr, roleMgr).Wait();
        DbInitializer.SeedLocations(context).Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi nạp hạt giống dữ liệu!");
    }
}

// 7. ĐỊNH NGHĨA ROUTE (Quan trọng nhất để hết lỗi 404)

// Ưu tiên 1: Tìm trong các khu vực Area (Admin, Seller, Buyer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Ưu tiên 2: Nếu nẫu gõ localhost:7167 thì nó tự động bay vô khu Buyer cho khách luôn
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Buyer}/{controller=Home}/{action=Index}/{id?}");

// Khai báo đường dẫn cho ChatHub
app.MapHub<ChatHub>("/chatHub");

app.Run();