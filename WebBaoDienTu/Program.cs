using WebBaoDienTu.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("BaoDienTuContext");

// Thêm DbContext vào dịch vụ
builder.Services.AddDbContext<BaoDienTuContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình pipeline xử lý request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Quan trọng: để load CSS, JS, ảnh, v.v.

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
