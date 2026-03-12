using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
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
builder.Services.AddHttpClient<PackageApiService>();
builder.Services.AddHttpClient<SubscriptionApiService>();

// --- CẤU HÌNH SESSION ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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