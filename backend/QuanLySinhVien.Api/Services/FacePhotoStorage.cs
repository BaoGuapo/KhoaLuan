using System.Security.Cryptography;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.DataProtection;

namespace QuanLySinhVien.Api.Services;

public sealed class CloudinarySettings
{
    public bool Enabled { get; init; }
    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string Folder { get; init; } = "huit-attendance/face-review";

    public bool IsConfigured =>
        Enabled && CloudName.Length > 0 && ApiKey.Length > 0 && ApiSecret.Length > 0;
}

/// <summary>
/// Nơi cất ảnh đăng ký khuôn mặt. Giá trị trả về được ghi vào cột
/// <c>HoSoKhuonMat.RegistrationPhotosProtected</c>, nên hai cách lưu dùng chung một cột:
/// ảnh cũ nằm thẳng trong cơ sở dữ liệu, ảnh mới nằm trên Cloudinary và cột chỉ giữ mã ảnh.
/// </summary>
public interface IFacePhotoStorage
{
    Task<string> SaveRegistrationAsync(string[] frames, CancellationToken ct);

    /// <summary>Địa chỉ xem ảnh: chuỗi data URL khi lưu trong CSDL, hoặc URL đã ký của Cloudinary.</summary>
    Task<string[]> ReadRegistrationAsync(string? stored, CancellationToken ct);

    /// <summary>Số ảnh đang lưu, dùng để kiểm tra đủ ba ảnh trước khi duyệt mà không cần tải ảnh về.</summary>
    Task<int> CountRegistrationAsync(string? stored, CancellationToken ct);

    Task DeleteRegistrationAsync(string? stored, CancellationToken ct);
}

/// <summary>Cách lưu sẵn có: ảnh được mã hóa rồi ghi thẳng vào cột trong SQL Server.</summary>
public sealed class DatabaseFacePhotoStorage(FaceReviewPhotos photos) : IFacePhotoStorage
{
    public Task<string> SaveRegistrationAsync(string[] frames, CancellationToken ct) =>
        Task.FromResult(photos.ProtectRegistration(frames));

    public Task<string[]> ReadRegistrationAsync(string? stored, CancellationToken ct) =>
        Task.FromResult(photos.ReadRegistration(stored).Select(FaceReviewPhotos.DataUrl).ToArray());

    public Task<int> CountRegistrationAsync(string? stored, CancellationToken ct) =>
        Task.FromResult(photos.ReadRegistration(stored).Length);

    public Task DeleteRegistrationAsync(string? stored, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// Tải ảnh lên Cloudinary ở chế độ <c>authenticated</c>: ảnh không mở được bằng URL thường,
/// mỗi lần xem phải sinh một URL có chữ ký và có hạn. Cột trong CSDL chỉ giữ danh sách mã ảnh,
/// vẫn được mã hóa để mã ảnh không lộ khi ai đó đọc được cơ sở dữ liệu.
/// </summary>
public sealed class CloudinaryFacePhotoStorage : IFacePhotoStorage
{
    private const string Marker = "cloudinary.v1";
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly IDataProtector _protector;
    private readonly IFacePhotoStorage _fallback;
    private readonly ILogger<CloudinaryFacePhotoStorage> _logger;

    public CloudinaryFacePhotoStorage(
        CloudinarySettings settings,
        IDataProtectionProvider protection,
        DatabaseFacePhotoStorage fallback,
        ILogger<CloudinaryFacePhotoStorage> logger)
    {
        _settings = settings;
        _fallback = fallback;
        _logger = logger;
        _protector = protection.CreateProtector("CloudinaryFacePhotoIds.v1");
        _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    private sealed record StoredIds(string Kind, string[] PublicIds);

    public async Task<string> SaveRegistrationAsync(string[] frames, CancellationToken ct)
    {
        var publicIds = new List<string>(frames.Length);
        try
        {
            foreach (var frame in frames)
            {
                await using var stream = new MemoryStream(Convert.FromBase64String(frame));
                var upload = await _cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription($"{Guid.NewGuid():N}.jpg", stream),
                    Folder = _settings.Folder,
                    // Ảnh sinh trắc học không được để công khai.
                    Type = "authenticated",
                    Overwrite = false,
                    UseFilename = false,
                    UniqueFilename = true
                }, ct);
                if (upload.Error is not null || upload.PublicId is null)
                    throw new InvalidOperationException(upload.Error?.Message ?? "Cloudinary không trả về mã ảnh.");
                publicIds.Add(upload.PublicId);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Hỏng kết nối Cloudinary thì dọn ảnh đã lên rồi quay về cách lưu trong CSDL,
            // để sinh viên vẫn đăng ký được khuôn mặt.
            await DestroyAsync(publicIds, CancellationToken.None);
            _logger.LogError(e, "Không tải được ảnh đăng ký lên Cloudinary, quay về lưu trong cơ sở dữ liệu.");
            return await _fallback.SaveRegistrationAsync(frames, ct);
        }

        return _protector.Protect(JsonSerializer.Serialize(new StoredIds(Marker, [.. publicIds])));
    }

    public Task<string[]> ReadRegistrationAsync(string? stored, CancellationToken ct)
    {
        if (Read(stored) is not { } ids) return _fallback.ReadRegistrationAsync(stored, ct);
        // Ảnh authenticated chỉ mở được bằng URL mang chữ ký sinh từ API secret;
        // đoán mò địa chỉ hay sửa tham số đều bị Cloudinary từ chối.
        return Task.FromResult(ids.PublicIds
            .Select(publicId => _cloudinary.Api.UrlImgUp
                .Secure(true)
                .Type("authenticated")
                .Signed(true)
                .BuildUrl(publicId))
            .ToArray());
    }

    public Task<int> CountRegistrationAsync(string? stored, CancellationToken ct) =>
        Read(stored) is { } ids
            ? Task.FromResult(ids.PublicIds.Length)
            : _fallback.CountRegistrationAsync(stored, ct);

    public async Task DeleteRegistrationAsync(string? stored, CancellationToken ct)
    {
        if (Read(stored) is { } ids) await DestroyAsync(ids.PublicIds, ct);
    }

    private StoredIds? Read(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return null;
        try
        {
            var value = JsonSerializer.Deserialize<StoredIds>(_protector.Unprotect(stored));
            return value?.Kind == Marker ? value : null;
        }
        // Giá trị cũ được mã hóa bằng protector khác nên bung ra sẽ lỗi; đó là ảnh lưu trong CSDL.
        catch (Exception e) when (e is CryptographicException or JsonException or FormatException) { return null; }
    }

    private async Task DestroyAsync(IEnumerable<string> publicIds, CancellationToken ct)
    {
        foreach (var publicId in publicIds)
        {
            try
            {
                // Invalidate để bản sao trên CDN cũng bị gỡ, không chỉ bản gốc.
                await _cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    Type = "authenticated",
                    Invalidate = true
                });
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Không xóa được ảnh {PublicId} trên Cloudinary.", publicId);
            }
        }
    }
}
