var builder = WebApplication.CreateBuilder(args);

// Sử dụng cấu hình từ appsettings.json
builder.WebHost.UseKestrel()
    .ConfigureKestrel((context, options) =>
    {
        options.ListenAnyIP(6001); // HTTP
        options.ListenAnyIP(6002, listenOptions => listenOptions.UseHttps()); // HTTPS
    });

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình Middleware
app.UseStaticFiles();

// Nếu không cần HTTPS, có thể comment dòng này lại
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=News}/{action=Index}/{id?}");

app.Run();
