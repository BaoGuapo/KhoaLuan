using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLySinhVien.Api.Controllers;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Services;

internal static class AttendanceApiBindingChecks
{
    // Exercise MVC model binding/validation over HTTP; direct controller calls miss record metadata failures.
    public static async Task Run()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(QrAttendanceController).Assembly);
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("FixtureCookie").AddCookie("FixtureCookie", options =>
        { options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }; });
        builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("face-recognition", window =>
        { window.PermitLimit = 50; window.Window = TimeSpan.FromMinutes(1); window.QueueLimit = 0; }));
        builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("face-guidance", window =>
        { window.PermitLimit = 120; window.Window = TimeSpan.FromMinutes(1); window.QueueLimit = 0; }));
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddSingleton<AttendanceQrTokens>();
        builder.Services.AddSingleton<FaceReviewPhotos>();
        builder.Services.Configure<FaceRecognitionOptions>(_ => { });
        builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
        builder.Services.AddSingleton<IFaceEvidenceService, UnusedEvidence>();
        await using var app = builder.Build();
        using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            var role = context.Request.Headers["X-Fixture-Role"].FirstOrDefault() ?? (context.Request.Path.Value!.EndsWith("/quet") ? "SinhVien" : "GiangVien");
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, "FIXTURE"), new(ClaimTypes.Role, role)], "Fixture"));
            await next();
        });
        app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            var count = 0;
            async Task Expect(string route, object body, HttpStatusCode expected, string label)
            {
                using var result = await http.PostAsJsonAsync(route, body);
                if (result.StatusCode != expected) throw new Exception($"{label}: expected {expected}, got {result.StatusCode}: {await result.Content.ReadAsStringAsync()}");
                count++; Console.WriteLine("PASS: " + label);
            }
            const string open = "/api/v1/qr-diem-danh/buoi-hoc/MISSING/mo";
            await Expect(open, new { minutes = 10 }, HttpStatusCode.NotFound, "valid open-session payload reaches controller without metadata exception");
            await Expect(open, new { }, HttpStatusCode.NotFound, "default ten-minute payload binds over HTTP");
            await Expect(open, new { minutes = 0 }, HttpStatusCode.BadRequest, "zero-minute session rejected by MVC validation");
            await Expect(open, new { minutes = 31 }, HttpStatusCode.BadRequest, "overlong session rejected by MVC validation");
            await Expect("/api/v1/qr-diem-danh/quet", new { qrToken = "not-a-valid-qr" }, HttpStatusCode.UnprocessableEntity, "QR scan reaches token validation without metadata exception");
            await Expect("/api/v1/qr-diem-danh/quet", new { qrToken = "" }, HttpStatusCode.BadRequest, "empty QR rejected by MVC validation");
            await Expect("/api/v1/qr-diem-danh/quet", new { qrToken = new string('x', 4097) }, HttpStatusCode.BadRequest, "oversized QR rejected by MVC validation");
            await Expect("/api/v1/qr-diem-danh/huong-dan", new { challengeId = Guid.NewGuid(), frame = "" }, HttpStatusCode.BadRequest, "empty live camera frame rejected by MVC validation");
            await Expect("/api/v1/qr-diem-danh/huong-dan", new { challengeId = Guid.NewGuid(), frame = "fixture" }, HttpStatusCode.Conflict, "live guidance endpoint requires an active owned nonce before inference");
            foreach (var role in new[] { "GiangVien", "SinhVien" })
            {
                http.DefaultRequestHeaders.Remove("X-Fixture-Role"); http.DefaultRequestHeaders.Add("X-Fixture-Role", role);
                using var denied = await http.GetAsync("/api/v1/qr-diem-danh/ho-so/cho-duyet");
                if (denied.StatusCode != HttpStatusCode.Forbidden) throw new Exception($"{role} can list profiles: {denied.StatusCode}");
                count++;
                foreach (var action in new[] { "thu-thach-duyet", "duyet", "huy", "anh-the", "tu-choi" })
                    await Expect($"/api/v1/qr-diem-danh/ho-so/MISSING/{action}", new { profileVersion = Guid.NewGuid(), identityPhotoVersion = Guid.NewGuid(), confirmCompared = true, note = "fixture", frame = "fixture" }, HttpStatusCode.Forbidden, $"{role} forbidden from face {action} over HTTP");
                using var hiddenPhotos = await http.GetAsync("/api/v1/qr-diem-danh/ho-so/MISSING/doi-chieu");
                if (hiddenPhotos.StatusCode != HttpStatusCode.Forbidden) throw new Exception("Unauthorized role can read review images.");
                count++;
            }
            foreach (var role in new[] { "CanBoQuanLy", "Admin" })
            {
                http.DefaultRequestHeaders.Remove("X-Fixture-Role"); http.DefaultRequestHeaders.Add("X-Fixture-Role", role);
                using var allowed = await http.GetAsync("/api/v1/qr-diem-danh/ho-so/cho-duyet");
                if (allowed.StatusCode != HttpStatusCode.OK) throw new Exception($"{role} cannot manage profiles: {allowed.StatusCode}");
                count++;
                foreach (var action in new[] { "thu-thach-duyet", "duyet", "huy", "anh-the", "tu-choi" })
                    await Expect($"/api/v1/qr-diem-danh/ho-so/MISSING/{action}", new { profileVersion = Guid.NewGuid(), identityPhotoVersion = Guid.NewGuid(), confirmCompared = true, note = "fixture", frame = "fixture" }, action == "thu-thach-duyet" ? HttpStatusCode.Gone : HttpStatusCode.NotFound, $"{role} photo-review authorization handles {action}");
            }
            Console.WriteLine($"{count} attendance HTTP binding and authorization checks passed.");
        }
        finally { await app.StopAsync(); }
    }
    private sealed class UnusedEvidence : IFaceEvidenceService
    {
        public Task<FacePoseObservation> InspectPoseAsync(string frame, CancellationToken ct) => throw new InvalidOperationException("Unexpected guidance inference.");
        public Task<IReadOnlyCollection<double[]>> AnalyzeAsync(string[] frames, string direction, CancellationToken ct) => throw new InvalidOperationException("These HTTP validation checks must not invoke AI.");
    }
}
