using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- CẤU HÌNH API URL ---
var baseUri = ApiConstant.apiBaseUrl.EndsWith("/") ? ApiConstant.apiBaseUrl : ApiConstant.apiBaseUrl + "/";
var finalApiUrl = new Uri(baseUri + "api/");

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AuthApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<CourseApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ExamApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ChatApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<IDashboardApiService, DashboardApiService>(client => client.BaseAddress = finalApiUrl);

// --- CẤU HÌNH SESSION CHO SERVER (BẮT BUỘC) ---
builder.Services.AddDistributedMemoryCache(); // <--- THÊM DÒNG NÀY ĐỂ SERVER LƯU ĐƯỢC SESSION
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Lax giúp Cookie không bị trình duyệt chặn khi chạy qua IP Server
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "ToanHocHay_Auth_Cookie";
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
var app = builder.Build();

// --- THỨ TỰ MIDDLEWARE (KHÔNG ĐƯỢC SAI) ---
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // <--- PHẢI NẰM TRƯỚC AUTHENTICATION
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();