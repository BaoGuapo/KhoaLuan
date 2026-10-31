using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured with User Secrets or an environment variable.");
}

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
jwtSettings.Validate();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobile", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddDataProtection();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuanLySinhVien API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste only the JWT access token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    connectionString,
    sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null)));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<FaceRecognitionOptions>(builder.Configuration.GetSection(FaceRecognitionOptions.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountProvisioningService, AccountProvisioningService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddOptions<FaceEvidenceOptions>().Bind(builder.Configuration.GetSection("FaceEvidence"))
    .Validate(x => x.MinimumLiveScore > 0 && x.MinimumLiveScore <= 1 && x.MinimumTurn > 0 && x.MinimumTurn < 1,
        "Face evidence thresholds are invalid.").ValidateOnStart();
builder.Services.AddSingleton<IFaceEvidenceService, FaceEvidenceService>();
builder.Services.AddSingleton<AttendanceQrTokens>();
builder.Services.AddSingleton<FaceReviewPhotos>();
// Chưa khai báo đủ khóa Cloudinary thì ảnh đăng ký vẫn lưu trong cơ sở dữ liệu như trước.
var cloudinarySettings = builder.Configuration.GetSection("Cloudinary").Get<CloudinarySettings>()
    ?? new CloudinarySettings();
builder.Services.AddSingleton(cloudinarySettings);
builder.Services.AddSingleton<DatabaseFacePhotoStorage>();
if (cloudinarySettings.IsConfigured)
    builder.Services.AddSingleton<IFacePhotoStorage, CloudinaryFacePhotoStorage>();
else
    builder.Services.AddSingleton<IFacePhotoStorage>(x => x.GetRequiredService<DatabaseFacePhotoStorage>());
builder.Services.AddScoped<TrainingEnrollmentService>();
builder.Services.AddScoped<AttendanceReportService>();
// Chưa khai báo SMTP thì thư chỉ được ghi vào nhật ký, nghiệp vụ vẫn chạy bình thường.
builder.Services.AddSingleton(builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings());
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailSender>(x => x.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailBackgroundSender>();
builder.Services.AddScoped<AttendanceEmailNotifier>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("face-guidance", context =>
        RateLimitPartition.GetFixedWindowLimiter(context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("face-recognition", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("face-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Mobile emulator/device uses the HTTP development endpoint because it cannot
// trust the ASP.NET Core local development certificate. Production still
// redirects all traffic to HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowMobile");
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        && Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        var account = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.IsActive,
                x.MustChangePassword,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToArray()
            })
            .SingleOrDefaultAsync(context.RequestAborted);

        if (account is null || !account.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Tài khoản không tồn tại hoặc đã bị khóa.",
                code = "ACCOUNT_DISABLED"
            });
            return;
        }

        var tokenRoles = context.User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.Ordinal);
        if (!tokenRoles.SetEquals(account.Roles))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Quyền tài khoản đã thay đổi. Vui lòng đăng nhập lại.",
                code = "ACCOUNT_PERMISSIONS_CHANGED"
            });
            return;
        }

        if (account.MustChangePassword
            && !context.Request.Path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
            && !context.Request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Password must be changed before using other protected API endpoints.",
                code = "PASSWORD_CHANGE_REQUIRED"
            });
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();

// This seeds only an already-migrated, reachable development database. It never runs migrations.
if (app.Environment.IsDevelopment())
{
    await DbInitializer.SeedDevelopmentAdminAsync(app.Services);
}

app.Run();

public partial class Program;
