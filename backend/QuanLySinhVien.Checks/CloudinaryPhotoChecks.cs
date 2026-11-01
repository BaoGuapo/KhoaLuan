using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLySinhVien.Api.Services;

/// <summary>
/// Kiểm tra thật trên Cloudinary: tải ảnh lên, lấy URL đã ký, tải về rồi xóa.
/// Cần khóa trong User Secrets nên chỉ chạy khi gọi tay bằng --cloudinary.
/// </summary>
internal static class CloudinaryPhotoChecks
{
    private static int _count;

    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }

    public static async Task Run()
    {
        var secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "d43cef33-5d2c-41ff-89c3-7322e0fef7cc", "secrets.json");
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(secretsPath));
        string Value(string key) => config.RootElement.TryGetProperty(key, out var item) ? item.GetString() ?? "" : "";

        var settings = new CloudinarySettings
        {
            Enabled = true,
            CloudName = Value("Cloudinary:CloudName"),
            ApiKey = Value("Cloudinary:ApiKey"),
            ApiSecret = Value("Cloudinary:ApiSecret"),
            Folder = Value("Cloudinary:Folder")
        };
        Check(settings.IsConfigured, "đã khai báo đủ khóa Cloudinary trong User Secrets");

        var protection = DataProtectionProvider.Create(nameof(CloudinaryPhotoChecks));
        var fallback = new DatabaseFacePhotoStorage(new FaceReviewPhotos(protection));
        var storage = new CloudinaryFacePhotoStorage(
            settings, protection, fallback, NullLogger<CloudinaryFacePhotoStorage>.Instance);

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var samplePath = Path.Combine(repoRoot, ".artifacts", "face-test.jpg");
        Check(File.Exists(samplePath), "có ảnh mẫu để tải lên");
        var frame = Convert.ToBase64String(await File.ReadAllBytesAsync(samplePath));

        var admin = new CloudinaryDotNet.Cloudinary(
            new CloudinaryDotNet.Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
        async Task<int> DemAnhTrongThuMuc()
        {
            var list = await admin.ListResourcesAsync(new CloudinaryDotNet.Actions.ListResourcesByPrefixParams
            {
                Prefix = settings.Folder,
                Type = "authenticated",
                MaxResults = 500
            });
            if (list.Error is not null) throw new Exception("Cloudinary Admin API: " + list.Error.Message);
            return list.Resources?.Length ?? 0;
        }

        var truocKhiTai = await DemAnhTrongThuMuc();
        var stored = await storage.SaveRegistrationAsync([frame, frame, frame], CancellationToken.None);
        try
        {
            Check(!stored.Contains("cloudinary", StringComparison.OrdinalIgnoreCase),
                "mã ảnh ghi vào cơ sở dữ liệu ở dạng đã mã hóa");
            Check(await storage.CountRegistrationAsync(stored, CancellationToken.None) == 3,
                "đếm đúng ba ảnh mà không cần tải ảnh về");
            Check(await DemAnhTrongThuMuc() == truocKhiTai + 3, "thư mục trên Cloudinary có thêm đúng ba ảnh");

            var urls = await storage.ReadRegistrationAsync(stored, CancellationToken.None);
            Check(urls.Length == 3, "trả về ba địa chỉ ảnh");
            Check(urls.All(x => x.StartsWith("https://", StringComparison.Ordinal)), "địa chỉ dùng HTTPS");
            Check(urls.All(x => x.Contains("/authenticated/", StringComparison.Ordinal)),
                "ảnh ở chế độ authenticated, không mở được bằng URL thường");
            Check(urls.All(x => x.Contains("/s--", StringComparison.Ordinal)), "địa chỉ có chữ ký");

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var signed = await http.GetAsync(urls[0]);
            Check(signed.IsSuccessStatusCode, "tải được ảnh bằng địa chỉ đã ký");
            Check(signed.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true,
                "nội dung trả về đúng là ảnh");

            // Bỏ chữ ký đi thì Cloudinary phải từ chối, nếu không thì ảnh đang công khai.
            var khongKy = System.Text.RegularExpressions.Regex.Replace(urls[0], @"/s--[^/]+--/", "/");
            using var unsigned = await http.GetAsync(khongKy);
            Check(!unsigned.IsSuccessStatusCode, "địa chỉ không chữ ký bị từ chối");
        }
        finally
        {
            await storage.DeleteRegistrationAsync(stored, CancellationToken.None);
        }

        // Kiểm tra qua Admin API chứ không qua URL, vì CDN còn phục vụ bản đã lưu đệm thêm một lúc.
        Check(await DemAnhTrongThuMuc() == truocKhiTai, "ba ảnh đã bị xóa khỏi Cloudinary sau khi dọn");

        Console.WriteLine($"PASS: {_count} checks for Cloudinary face photo storage.");
    }
}
