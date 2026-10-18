using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using QuanLySinhVien.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<StudentAcademicApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"] ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing."));
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<TrainingWorkflowApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing."));
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/tai-khoan/dang-nhap";
        options.AccessDeniedPath = "/tai-khoan/tu-choi-truy-cap";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(QuanLySinhVien.Web.Models.RoleNames.Admin));
    options.AddPolicy("CanBoQuanLyOnly", policy => policy.RequireRole(QuanLySinhVien.Web.Models.RoleNames.CanBoQuanLy));
    options.AddPolicy("GiangVienOnly", policy => policy.RequireRole(QuanLySinhVien.Web.Models.RoleNames.GiangVien));
    options.AddPolicy("SinhVienOnly", policy => policy.RequireRole(QuanLySinhVien.Web.Models.RoleNames.SinhVien));
});
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IUserManagementApiClient, UserManagementApiClient>(client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ITeachingManagementApiClient, TeachingManagementApiClient>(client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IFaceRecognitionApiClient, FaceRecognitionApiClient>(client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is missing.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "models", "face")),
    RequestPath = "/models/face",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".mjs"] = "text/javascript";
contentTypes.Mappings[".wasm"] = "application/wasm";
contentTypes.Mappings[".onnx"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypes });
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        && string.IsNullOrWhiteSpace(context.Session.GetString("AccessToken"))
        && !context.Request.Path.StartsWithSegments("/tai-khoan/dang-nhap"))
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect("/tai-khoan/dang-nhap");
        return;
    }

    var mustChangePassword = context.User.FindFirst(QuanLySinhVien.Web.Controllers.AccountController.MustChangePasswordClaim)?.Value;
    var isPasswordAction = context.Request.Path.StartsWithSegments("/tai-khoan/doi-mat-khau")
        || context.Request.Path.StartsWithSegments("/tai-khoan/dang-xuat");

    if (context.User.Identity?.IsAuthenticated == true
        && string.Equals(mustChangePassword, "true", StringComparison.OrdinalIgnoreCase)
        && !isPasswordAction)
    {
        context.Response.Redirect("/tai-khoan/doi-mat-khau");
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
