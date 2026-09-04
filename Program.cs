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
// Thứ tự: biến môi trường thật (Api__BaseUrl) > appsettings.json (Api:BaseUrl) > mặc định trong ApiConstant.
// .NET tự nạp biến môi trường qua AddEnvironmentVariables() trong CreateBuilder.
ApiConstant.apiBaseUrl = builder.Configuration["Api:BaseUrl"]?.Trim().TrimEnd('/') is { Length: > 0 } apiUrl
    ? apiUrl : ApiConstant.apiBaseUrl.TrimEnd('/');
ApiConstant.webBaseUrl = builder.Configuration["Api:WebBaseUrl"]?.Trim().TrimEnd('/') is { Length: > 0 } webUrl
    ? webUrl : ApiConstant.webBaseUrl.TrimEnd('/');

var sessionIdleMinutes = builder.Configuration.GetValue<int?>("Session:IdleTimeoutMinutes") ?? 60;
var cookieExpireDays = builder.Configuration.GetValue<int?>("Auth:CookieExpireDays") ?? 7;

var finalApiUrl = new Uri(ApiConstant.apiBaseUrl + "/api/");

builder.Services.AddHttpContextAccessor();

// --- TẦNG GỌI API CHUẨN HOÁ ---
builder.Services.AddScoped<ITokenStore, SessionTokenStore>();
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddTransient<ClientContextHandler>();

// Gắn ClientContextHandler (X-Client-Key) vào MỌI HttpClient gọi Backend — kể cả client
// mặc định và client "thô" — để rate limiter phía Backend phân vùng theo người dùng cuối.
builder.Services.ConfigureAll<Microsoft.Extensions.Http.HttpClientFactoryOptions>(options =>
{
    options.HttpMessageHandlerBuilderActions.Add(b =>
        b.AdditionalHandlers.Add(b.Services.GetRequiredService<ClientContextHandler>()));
});

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
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
builder.Services.AddHttpClient<PackageApiService>(client => client.BaseAddress = finalApiUrl)
    .AddHttpMessageHandler<AuthTokenHandler>();

// Các service dùng ApiClient (P2/P4/P5/P6).
builder.Services.AddScoped<ContentApiService>();
builder.Services.AddScoped<SubscriptionApiService>();
builder.Services.AddScoped<PaymentApiService>();
builder.Services.AddScoped<RefundApiService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<ChatApiService>();
builder.Services.AddScoped<ParentApiService>();

// --- CẤU HÌNH SESSION ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionIdleMinutes);
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
        options.ExpireTimeSpan = TimeSpan.FromDays(cookieExpireDays);
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