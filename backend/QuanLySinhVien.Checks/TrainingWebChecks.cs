using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLySinhVien.Web.Models;
using QuanLySinhVien.Web.Services;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class TrainingWebChecks
{
    // Host kiểm tra riêng, dữ liệu giả trong memory, không nới lỏng xác thực của web thật.
    public static async Task Run()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = Directory.GetCurrentDirectory(), EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(QuanLySinhVien.Web.Controllers.TrainingWorkflowController).Assembly);
        builder.Services.AddAuthorization(); builder.Services.AddDistributedMemoryCache(); builder.Services.AddSession();
        var handler = new FixtureApi();
        builder.Services.AddSingleton(new TrainingWorkflowApiClient(new HttpClient(handler) { BaseAddress = new("http://fixture.invalid/") }));
        await using var app = builder.Build();
        app.UseRouting(); app.UseSession();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "fixture"), new(ClaimTypes.Name, "CBQL_TEST"), new(ClaimTypes.Role, "CanBoQuanLy")], "Fixture"));
            context.Session.SetString("AccessToken", "fixture-only"); await next();
        });
        app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new(address) };
            using var response = await http.GetAsync("/dao-tao");
            var html = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK) throw new Exception($"Razor rendering failed: {response.StatusCode}");
            var checks = 0;
            void Check(bool pass, string label) { if (!pass) throw new Exception(label); Console.WriteLine("PASS: " + label); checks++; }
            Check(html.Contains("id=\"curriculum\"") && html.Contains("id=\"assignment\""), "Razor renders four-step training workspace");
            Check(Enumerable.Range(1, 8).All(i => html.Contains($"TEST-{i:00}")), "Razor renders eight same-course classes");
            Check(html.Contains("/dao-tao/phan-cong") && html.Contains("/dao-tao/mo-dang-ky"), "assignment and publish forms have working routes");
            Check(html.Contains("/dao-tao/hoc-ky") && html.Contains("/dao-tao/luu-chuong-trinh"), "catalog and curriculum form routes");
            var token = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Check(token.Length > 0, "mutating forms include antiforgery token");
            using var post = await http.PostAsync("/dao-tao/luu-chuong-trinh", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token), ["MaNganh"] = "CNTT", ["MaKhoa"] = "K2026",
                ["TenKhoaDaoTao"] = "Khoa CNTT", ["TenChuongTrinh"] = "Chương trình test",
                ["MonHocs[0].MaMonHoc"] = "LTCB", ["MonHocs[0].ThuTuHocKy"] = "1", ["MonHocs[0].BatBuoc"] = "true"
            }));
            Check(post.StatusCode == HttpStatusCode.Redirect && handler.LastBody?.GetProperty("maNganh").GetString() == "CNTT" &&
                handler.LastBody?.GetProperty("monHocs")[0].GetProperty("batBuoc").GetBoolean() == true, "MVC binds curriculum form and sends JSON to API");
            using var rejected = await http.PostAsync("/dao-tao/mo-lop", new FormUrlEncodedContent(new Dictionary<string, string> { ["soLop"] = "8" }));
            Check(rejected.StatusCode == HttpStatusCode.BadRequest, "reject POST without antiforgery token");
            Console.WriteLine($"{checks} web smoke checks passed (mock API; no database writes).");
        }
        finally { await app.StopAsync(); }
    }

    private sealed class FixtureApi : HttpMessageHandler
    {
        public JsonElement? LastBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            object value;
            if (request.Method == HttpMethod.Post)
            {
                LastBody = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                value = new { message = "Fixture success" };
            }
            else if (request.RequestUri!.AbsolutePath.EndsWith("danh-muc"))
                value = new TrainingCatalogModel { NganhHocs = [new() { Code = "CNTT", Name = "Công nghệ thông tin" }],
                    KhoaTuyenSinhs = [new() { Code = "K2026", Name = "Khóa 2026" }], MonHocs = [new() { MaMonHoc = "LTCB", TenMonHoc = "Lập trình cơ bản" }],
                    HocKys = [new() { MaHocKy = "HK1", TenHocKy = "Học kỳ 1" }], GiangViens = [new() { MaGiangVien = "GV001", HoTen = "Giảng viên test", ChuyenNganh = "CNTT" }],
                    PhongHocs = [new() { MaPhong = "P101", TenPhong = "Phòng 101" }] };
            else if (request.RequestUri!.AbsolutePath.EndsWith("chuong-trinh"))
                value = new[] { new CurriculumModel { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), MaNganh = "CNTT", MaKhoa = "K2026", TenChuongTrinh = "CNTT 2026", MonHocs = [new() { MaMonHoc = "LTCB", TenMonHoc = "Lập trình cơ bản" }] } };
            else value = new[] { new SemesterApplicationModel { Id = Guid.NewGuid(), MaNganh = "CNTT", MaKhoa = "K2026", TenChuongTrinh = "CNTT 2026", MaHocKy = "HK1", ThuTuHocKy = 1,
                MonHocs = [new() { MaMonHoc = "LTCB", TenMonHoc = "Lập trình cơ bản" }],
                LopHocPhans = Enumerable.Range(1, 8).Select(i => new PlannedClassModel { MaLopHocPhan = $"TEST-{i:00}", MaMonHoc = "LTCB", SiSoToiDa = 40, DaPhanCong = i == 1, MaGiangVien = i == 1 ? "GV001" : null, TrangThai = i == 1 ? "1" : null }).ToList() } };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
        }
    }
}
