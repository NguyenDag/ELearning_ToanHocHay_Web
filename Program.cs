using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. ??ng ký các Controller và View
builder.Services.AddControllersWithViews();

// 2. ??NG KÝ CÁC D?CH V? G?I API (QUAN TR?NG)
// Thêm dòng HttpClient cho CourseApiService ?? WebApp có th? l?y d? li?u bài gi?ng
builder.Services.AddHttpClient<CourseApiService>();
builder.Services.AddHttpClient<ExamApiService>();
builder.Services.AddHttpClient<AuthApiService>();
builder.Services.AddHttpContextAccessor();

// 3. C?u hình Xác th?c b?ng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // ???ng d?n ??n trang ??ng nh?p n?u ch?a auth
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

// 4. C?u hình Session (?? l?u Token ho?c tr?ng thái t?m th?i)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles(); // ??m b?o các file css/js/img trong wwwroot ho?t ??ng

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map các file static t? bundle m?i c?a .NET 9 (n?u có)
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();