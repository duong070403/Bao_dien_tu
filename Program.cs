using WebBaoDienTu.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebBaoDienTu.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(provider => {
    var smtpServer = builder.Configuration["EmailSettings:SmtpServer"]
        ?? throw new InvalidOperationException("SMTP Server configuration is missing");
    var smtpPortStr = builder.Configuration["EmailSettings:SmtpPort"]
        ?? throw new InvalidOperationException("SMTP Port configuration is missing");
    var senderEmail = builder.Configuration["EmailSettings:SenderEmail"]
        ?? throw new InvalidOperationException("Sender Email configuration is missing");
    var senderPassword = builder.Configuration["EmailSettings:SenderPassword"]
        ?? throw new InvalidOperationException("Sender Password configuration is missing");

    if (!int.TryParse(smtpPortStr, out int smtpPort))
    {
        throw new InvalidOperationException("Invalid SMTP Port configuration");
    }

    return new EmailService(smtpServer, smtpPort, senderEmail, senderPassword);
});

builder.Services.AddDbContext<BaoDienTuContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.IsEssential = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
