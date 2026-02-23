using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các Controller và View
builder.Services.AddControllersWithViews();

// 2. ĐĂNG KÝ CÁC DỊCH VỤ GỌI API (QUAN TRỌNG)
// Thêm dòng HttpClient cho CourseApiService ?? WebApp có th? l?y d? li?u bài gi?ng
var baseUri = ApiConstant.apiBaseUrl.EndsWith("/") ? ApiConstant.apiBaseUrl : ApiConstant.apiBaseUrl + "/";
var finalApiUrl = new Uri(baseUri + "api/");

// Đăng ký HttpClient kèm BaseAddress cho TẤT CẢ các Service
builder.Services.AddHttpClient<CourseApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ExamApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<AuthApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ChatApiService>(client => client.BaseAddress = finalApiUrl);

builder.Services.AddHttpContextAccessor();


// 3. C?u hình Xác thực bang Cookie
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // 1. Định tuyến trước

app.UseSession(); // 2. Rồi mới đến Session để lưu Token
app.UseAuthentication(); // 3. Xác thực người dùng
app.UseAuthorization(); // 4. Kiểm tra quyền

// Map các file static t? bundle m?i c?a .NET 9 (n?u có)
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();