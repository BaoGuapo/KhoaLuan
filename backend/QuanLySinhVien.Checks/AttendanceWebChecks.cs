using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class AttendanceWebChecks
{
    public static async Task Run()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = Directory.GetCurrentDirectory(), EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(QuanLySinhVien.Web.Controllers.QrAttendanceController).Assembly);
        builder.Services.AddDistributedMemoryCache(); builder.Services.AddSession(); builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("FixtureCookie").AddCookie("FixtureCookie", options =>
        { options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }; });
        var fixture = new FixtureApi();
        builder.Services.AddHttpClient("Attendance", c => c.BaseAddress = new Uri("http://fixture.invalid/"))
            .ConfigurePrimaryHttpMessageHandler(() => fixture);
        await using var app = builder.Build();
        app.UseRouting(); app.UseSession();
        app.Use(async (context, next) =>
        {
            var role = context.Request.Headers["X-Fixture-Role"].FirstOrDefault() ?? "GiangVien";
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, role == "SinhVien" ? "SV1" : "GV1"), new(ClaimTypes.Role, role)], "Fixture"));
            context.Session.SetString("AccessToken", "fixture-only"); await next();
        });
        app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new() }) { BaseAddress = new Uri(address) };
            var count = 0;
            void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; Console.WriteLine("PASS: " + text); }
            var html = await http.GetStringAsync("/diem-danh");
            Check(html.Contains("id=\"open-session\"") && html.Contains("id=\"qr-image\"") && !html.Contains("id=\"profiles\""), "lecturer Razor page renders QR controls without face approval");
            var csrf = WebUtility.HtmlDecode(Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            using var denied = await http.PostAsJsonAsync("/diem-danh/api/open?code=BH", new { minutes = 10 });
            Check(denied.StatusCode == HttpStatusCode.BadRequest, "web rejects mutation without CSRF token");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/diem-danh/api/open?code=BH") { Content = JsonContent.Create(new { minutes = 10 }) };
            request.Headers.Add("RequestVerificationToken", csrf);
            using var accepted = await http.SendAsync(request);
            Check(accepted.IsSuccessStatusCode && fixture.LastPath == "/api/v1/qr-diem-danh/buoi-hoc/BH/mo" && fixture.Bearer == "fixture-only", "web proxy forwards validated request with server-held bearer");
            using var guide = new HttpRequestMessage(HttpMethod.Post, "/diem-danh/api/guidance") { Content = JsonContent.Create(new { challengeId = Guid.NewGuid(), frame = "fixture" }) };
            guide.Headers.Add("RequestVerificationToken", csrf);
            using var guided = await http.SendAsync(guide);
            Check(guided.IsSuccessStatusCode && fixture.LastPath == "/api/v1/qr-diem-danh/huong-dan" && fixture.Bearer == "fixture-only", "web live guidance preserves CSRF protection and server-held bearer");
            using var invalid = await http.GetAsync("/diem-danh/api/arbitrary-url");
            Check(invalid.StatusCode == HttpStatusCode.BadRequest, "proxy cannot forward arbitrary URL");
            using var forbiddenList = await http.GetAsync("/diem-danh/api/profiles");
            Check(forbiddenList.StatusCode == HttpStatusCode.Forbidden, "lecturer cannot bypass hidden face list through web proxy");
            using var forbiddenPage = await http.GetAsync("/quan-ly/ho-so-khuon-mat");
            using var forbiddenDedicatedApi = await http.GetAsync("/quan-ly/ho-so-khuon-mat/api/profiles");
            Check(forbiddenPage.StatusCode == HttpStatusCode.Forbidden && forbiddenDedicatedApi.StatusCode == HttpStatusCode.Forbidden, "lecturer denied from dedicated officer page and its API");
            foreach (var operation in new[] { "approval-challenge", "approve", "revoke" }) {
                using var blockedRequest = new HttpRequestMessage(HttpMethod.Post, $"/diem-danh/api/{operation}?code=SV1") { Content = JsonContent.Create(new { consent = true }) };
                blockedRequest.Headers.Add("RequestVerificationToken", csrf);
                using var blocked = await http.SendAsync(blockedRequest);
                Check(blocked.StatusCode == HttpStatusCode.Forbidden, $"lecturer web proxy blocks {operation} with valid CSRF");
            }
            foreach (var role in new[] { "CanBoQuanLy", "Admin" }) {
                http.DefaultRequestHeaders.Remove("X-Fixture-Role"); http.DefaultRequestHeaders.Add("X-Fixture-Role", role);
                html = await http.GetStringAsync("/quan-ly/ho-so-khuon-mat");
                Check(html.Contains("id=\"profiles\"") && html.Contains("id=\"profile-filter\"") && html.Contains("id=\"profile-search\"") && !html.Contains("id=\"open-session\""), $"{role} has a dedicated face approval workspace without QR session controls");
                using var profiles = await http.GetAsync("/quan-ly/ho-so-khuon-mat/api/profiles");
                Check(profiles.IsSuccessStatusCode && fixture.LastPath == "/api/v1/qr-diem-danh/ho-so/cho-duyet", $"{role} can load profiles through dedicated API");
                var faceCsrf = WebUtility.HtmlDecode(Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
                Check(html.Contains("id=\"identity-photo\"") && html.Contains("id=\"registration-photos\"") && !html.Contains("id=\"attendance-video\""), $"{role} reviews official and registration photos without a camera");
                using var noCsrf = await http.PostAsJsonAsync("/quan-ly/ho-so-khuon-mat/api/approve?code=SV1", new { });
                Check(noCsrf.StatusCode == HttpStatusCode.BadRequest, $"{role} dedicated page mutations require CSRF");
                using var faceRequest = new HttpRequestMessage(HttpMethod.Post, "/quan-ly/ho-so-khuon-mat/api/approve?code=SV1") { Content = JsonContent.Create(new { profileVersion = Guid.NewGuid(), identityPhotoVersion = Guid.NewGuid(), confirmCompared = true }) };
                faceRequest.Headers.Add("RequestVerificationToken", faceCsrf);
                using var faceResponse = await http.SendAsync(faceRequest);
                Check(faceResponse.IsSuccessStatusCode && fixture.LastPath == "/api/v1/qr-diem-danh/ho-so/SV1/duyet" && fixture.Bearer == "fixture-only", $"{role} dedicated page forwards photo-review decision with server-held token");
                using var review = await http.GetAsync("/quan-ly/ho-so-khuon-mat/api/review?code=SV1");
                Check(review.IsSuccessStatusCode && fixture.LastPath == "/api/v1/qr-diem-danh/ho-so/SV1/doi-chieu", $"{role} reads protected comparison photos through authenticated proxy");
            }
            http.DefaultRequestHeaders.Remove("X-Fixture-Role");
            http.DefaultRequestHeaders.Add("X-Fixture-Role", "SinhVien");
            html = await http.GetStringAsync("/diem-danh");
            Check(html.Contains("id=\"scan-qr\"") && html.Contains("id=\"my-results\"") && !html.Contains("id=\"open-session\""), "student Razor page renders QR scanner and personal history");
            html = await http.GetStringAsync("/diem-danh/ho-so");
            Check(html.Contains("id=\"face-consent\"") && html.Contains("id=\"start-enrollment\""), "face enrollment renders consent and live capture entry");
            using var studentDenied = await http.GetAsync("/quan-ly/ho-so-khuon-mat");
            Check(studentDenied.StatusCode == HttpStatusCode.Forbidden, "student cannot access dedicated officer approval page");
            Console.WriteLine($"{count} attendance web checks passed (isolated fixture, no shared database writes).");
        }
        finally { await app.StopAsync(); }
    }
    private sealed class FixtureApi : HttpMessageHandler
    {
        public string? LastPath; public string? Bearer;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastPath = request.RequestUri!.AbsolutePath; Bearer = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { sessionId = Guid.NewGuid() }) });
        }
    }
}
