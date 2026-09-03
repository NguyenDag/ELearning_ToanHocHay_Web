using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// --- CẤU HÌNH API URL ---
var baseUri = ApiConstant.apiBaseUrl.EndsWith("/") ? ApiConstant.apiBaseUrl : ApiConstant.apiBaseUrl + "/";
var finalApiUrl = new Uri(baseUri + "api/");

builder.Services.AddHttpContextAccessor();

// --- TẦNG GỌI API CHUẨN HOÁ ---
builder.Services.AddScoped<ITokenStore, SessionTokenStore>();
builder.Services.AddTransient<AuthTokenHandler>();

// Client "thô" chỉ dùng cho refresh-token — KHÔNG gắn AuthTokenHandler (tránh đệ quy).
builder.Services.AddHttpClient(AuthTokenHandler.RawClientName, c => c.BaseAddress = finalApiUrl);

// Client bọc ApiResponse<T> + mã HTTP thật, dùng cho các service mới/đã refactor.
builder.Services.AddHttpClient<ApiClient>(c => c.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();

// Các service hiện có: gắn AuthTokenHandler để tự đính token + tự refresh khi 401.
builder.Services.AddHttpClient<AuthApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<CourseApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<ExamApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<ChatApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IDashboardApiService, DashboardApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<PackageApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<SubscriptionApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();

// --- CẤU HÌNH SESSION ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "ToanHocHay_Auth_Cookie";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// --- THỨ TỰ MIDDLEWARE ---
app.UseMiddleware<ToanHocHay.WebApp.Common.Http.CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// --- DETECT SESSION MẤT SAU KHI RESTART SERVER ---
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var token = context.Session.GetString("Token");
        if (string.IsNullOrEmpty(token))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Session.Clear();
            context.Response.Cookies.Delete("ToanHocHay_Auth_Cookie");
            context.Response.Cookies.Delete("session_expiry_hint");

            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return;
            }

            context.Response.Redirect("/Account/Login");
            return;
        }
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();