using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ Controller và View (MVC)
builder.Services.AddControllersWithViews();

// 2. CẤU HÌNH KẾT NỐI API (HTTPCLIENT)
// Đảm bảo URL cơ sở luôn kết thúc bằng dấu "/" để tránh lỗi khi nối Route
var baseUri = ApiConstant.apiBaseUrl.EndsWith("/") ? ApiConstant.apiBaseUrl : ApiConstant.apiBaseUrl + "/";
// BaseAddress sẽ trỏ thẳng vào folder api/ của Backend (Ví dụ: https://localhost:7001/api/)
var finalApiUrl = new Uri(baseUri + "api/");

// Đăng ký IHttpContextAccessor để các Service có thể truy cập Session (lấy Token JWT)
builder.Services.AddHttpContextAccessor();

// Đăng ký HttpClient cho các Service kèm theo địa chỉ Backend tương ứng
builder.Services.AddHttpClient<AuthApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<CourseApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ExamApiService>(client => client.BaseAddress = finalApiUrl);
builder.Services.AddHttpClient<ChatApiService>(client => client.BaseAddress = finalApiUrl);

// QUAN TRỌNG: Đăng ký Dashboard Service để Dashboard có thể gọi dữ liệu từ Database
builder.Services.AddHttpClient<IDashboardApiService, DashboardApiService>(client => client.BaseAddress = finalApiUrl);

// 3. CẤU HÌNH XÁC THỰC (AUTHENTICATION)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn trang đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied"; // Trang báo lỗi quyền truy cập
        options.Cookie.Name = "ToanHocHay_Auth_Cookie";
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // Cookie hết hạn sau 2 giờ
    });

builder.Services.AddAuthorization();

// 4. CẤU HÌNH SESSION (Lưu trữ tạm thời Token JWT sau khi đăng nhập)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Session hết hạn sau 60 phút không hoạt động
    options.Cookie.HttpOnly = true; // Bảo mật ngăn chặn script truy cập cookie
    options.Cookie.IsEssential = true;
});

// 5. CẤU HÌNH CORS (Nếu bạn có gọi AJAX từ các domain khác)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// 6. CẤU HÌNH PIPELINE XỬ LÝ YÊU CẦU (MIDDLEWARE)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// THỨ TỰ QUAN TRỌNG: CORS -> Session -> Authentication -> Authorization
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Hỗ trợ Static Assets cho .NET 9
app.MapStaticAssets();

// CẤU HÌNH ROUTE MẶC ĐỊNH
app.MapControllerRoute(
    name: "default",
    // Chuyển trang chủ mặc định sang Dashboard để bạn kiểm tra kết nối DB ngay khi nhấn Run
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();