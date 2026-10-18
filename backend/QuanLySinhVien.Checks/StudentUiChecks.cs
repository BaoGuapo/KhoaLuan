using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using QuanLySinhVien.Web.Controllers;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;

public static class StudentUiChecks
{
    // Test-only host, fake API, no Azure connection and no production authentication changes.
    public static async Task Run(bool preview)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = Directory.GetCurrentDirectory(), EnvironmentName = "Testing",
            WebRootPath = Path.GetFullPath("web/QuanLySinhVien.Web/wwwroot")
        });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls(preview ? "http://127.0.0.1:7094" : "http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SinhVienController).Assembly);
        builder.Services.AddAuthorization(options => options.AddPolicy("SinhVienOnly", p => p.RequireRole("SinhVien")));
        builder.Services.AddDistributedMemoryCache(); builder.Services.AddSession();
        var fixture = new StudentFixture();
        HttpClient Client() => new(fixture, false) { BaseAddress = new("http://fixture.invalid/") };
        builder.Services.AddSingleton(new StudentAcademicApiClient(Client()));
        builder.Services.AddSingleton<IUserManagementApiClient>(new UserManagementApiClient(Client()));
        builder.Services.AddSingleton<IFaceRecognitionApiClient>(new FaceRecognitionApiClient(Client()));
        builder.Services.AddSingleton<IAuthApiClient>(new AuthApiClient(Client()));
        await using var app = builder.Build();
        app.UseStaticFiles(); app.UseRouting(); app.UseSession();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "fixture"), new(ClaimTypes.Name, "SV-UI-TEST"), new(ClaimTypes.Role, "SinhVien")], "Fixture"));
            context.Session.SetString("AccessToken", "fixture-only"); await next();
        });
        app.UseAuthorization();
        app.MapGet("/mobile-preview", () => Results.Content("""
            <!doctype html><html lang="vi"><head><meta charset="utf-8"><title>Student mobile preview</title></head>
            <body style="margin:0;background:#e1e6eb;display:flex;justify-content:center;padding:16px">
            <iframe title="Cổng sinh viên trên điện thoại" src="/SinhVien/DangKyHocPhan" style="width:390px;height:844px;border:1px solid #b9c5cc;background:white"></iframe></body></html>
            """, "text/html; charset=utf-8"));
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        try
        {
            if (preview)
            {
                Console.WriteLine("Student UI fixture preview: http://127.0.0.1:7094/SinhVien/DangKyHocPhan (no database writes)");
                await app.WaitForShutdownAsync(); return;
            }
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new() }) { BaseAddress = new(address) };
            var checks = 0;
            void Check(bool pass, string name) { if (!pass) throw new Exception(name); Console.WriteLine("PASS: " + name); checks++; }
            async Task<string> Page(string url)
            { using var result = await http.GetAsync(url); Check(result.StatusCode == HttpStatusCode.OK, "render " + url); return WebUtility.HtmlDecode(await result.Content.ReadAsStringAsync()); }
            var html = await Page("/SinhVien/DangKyHocPhan?mon=MH001");
            Check(html.Contains("Hết chỗ") && html.Contains("Trùng lịch") && html.Contains("Kết quả đăng ký"), "registration statuses and result section");
            Check(!html.Contains("Đỗ Gia Bảo") && !html.Contains("K15") && !html.Contains("moPopupDoiLich"), "no hard-coded previous student/mock controls");
            Check(Regex.Matches(html, "aria-current=\"page\"").Count == 1, "exactly one active student navigation link");
            var empty = await Page("/SinhVien/DangKyHocPhan?q=not-a-subject");
            Check(empty.Contains("Không có môn phù hợp bộ lọc") && empty.Contains("Xóa bộ lọc"), "search empty state and reset");
            var filtered = await Page("/SinhVien/DangKyHocPhan?mon=MH001&conCho=true");
            Check(!filtered.Contains("MH001-02"), "available-only filter removes full classes");
            var token = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            using var result = await http.PostAsync("/SinhVien/DangKy", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["__RequestVerificationToken"] = token, ["maLop"] = "MH001-01", ["hocKy"] = "HK1-2026", ["mon"] = "MH001" }));
            Check(result.StatusCode == HttpStatusCode.Redirect && fixture.Registrations == 1, "registration posts to API and redirects");
            html = await Page("/SinhVien/DangKyHocPhan?mon=MH001");
            Check(html.Contains("Đã đăng ký") && html.Contains("Đã chọn lớp khác"), "success state and same-subject lock after registration");
            using var invalid = await http.PostAsync("/SinhVien/DangKy", new FormUrlEncodedContent(new Dictionary<string, string> { ["maLop"] = "MH001-04" }));
            Check(invalid.StatusCode == HttpStatusCode.BadRequest && fixture.Registrations == 1, "antiforgery protects registration");
            html = await Page("/SinhVien/XemLichHoc?tuan=2026-10-19");
            Check(html.Contains("Tiết 1–3") && html.Contains("Lập trình cơ bản"), "weekly schedule comes from enrolled classes");
            await Page("/Home/SinhVien"); await Page("/thong-tin-ca-nhan"); await Page("/thong-tin-ca-nhan/cap-nhat");
            await Page("/khuon-mat"); await Page("/tai-khoan/doi-mat-khau");
            fixture.Failed = true;
            html = await Page("/SinhVien/DangKyHocPhan");
            Check(html.Contains("Chưa tải được danh sách") && !html.Contains("data-registration-form"), "API failure does not present fake/empty success data");
            Console.WriteLine($"{checks} student UI checks passed (fixture API only).");
        }
        finally { await app.StopAsync(); }
    }

    private sealed class StudentFixture : HttpMessageHandler
    {
        public bool Failed { get; set; }
        public int Registrations { get; private set; }
        private readonly List<StudentCourseClass> registered = [Course("MH002", "MH002-01", "Cơ sở dữ liệu", 3, 1)];
        private readonly List<StudentCourseClass> available = [Course("MH001", "MH001-01", "Lập trình cơ bản", 2, 1), Course("MH001", "MH001-02", "Lập trình cơ bản", 4, 1, true), Course("MH001", "MH001-03", "Lập trình cơ bản", 3, 1), Course("MH001", "MH001-04", "Lập trình cơ bản", 5, 7), Course("MH003", "MH003-01", "Mạng máy tính", 6, 1)];
        private static StudentCourseClass Course(string subject, string code, string name, int day, int start, bool full = false) => new()
        {
            MaMonHoc = subject, MaLopHocPhan = code, TenMonHoc = name, MaHocKy = "HK1-2026", HoTen = "Nguyễn Minh An",
            SoTinChiLyThuyet = 3, SiSoToiDa = 40, SiSoHienTai = full ? 40 : 18,
            ThoiGianDangKy = new(2026, 10, 1), LichHoc = [new() { ThuTrongTuan = day, TietBatDau = start, TietKetThuc = start + 2, TenPhong = "A.101" }]
        };
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (Failed) return new(HttpStatusCode.ServiceUnavailable);
            var path = request.RequestUri!.AbsolutePath; object value;
            if (request.Method == HttpMethod.Post)
            {
                var course = available.Single(x => path.EndsWith(x.MaLopHocPhan));
                course.DaDangKy = true; course.SiSoHienTai++; registered.Add(course); Registrations++;
                value = new { message = "Đăng ký thành công." };
            }
            else if (path.EndsWith("danh-sach")) value = new { data = available };
            else if (path.EndsWith("da-dang-ky")) value = registered;
            else if (path.EndsWith("hoc-ky")) value = new[] { new StudentSemester { MaHocKy = "HK1-2026", TenHocKy = "Học kỳ 1 (2026–2027)", NgayBatDau = new(2026, 9, 7), NgayKetThuc = new(2026, 12, 27) } };
            else if (path.Contains("ho-so-khuon-mat")) value = new { maSinhVien = "SV-UI-TEST", daDangKy = false };
            else value = new UserDetailModel(Guid.NewGuid(), "SV-UI-TEST", "Trần Hoàng Nam", "sv@local.test", ["SinhVien"], true, false, DateTime.UtcNow, "SinhVien", "SV-UI-TEST", "", new() { Nganh = "Công nghệ thông tin", Khoa = "Công nghệ thông tin", KhoaHoc = "2026", LopHoc = "26DHTH01", BacDaoTao = "Đại học" });
            await Task.CompletedTask;
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
        }
    }
}
