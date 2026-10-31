using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using SkiaSharp;

namespace QuanLySinhVien.Api.Services;

public sealed class FaceReviewPhotos(IDataProtectionProvider protection)
{
    private readonly IDataProtector _registration = protection.CreateProtector("FaceRegistrationReviewPhotos.v1");
    private readonly IDataProtector _identity = protection.CreateProtector("OfficialStudentIdentityPhoto.v1");
    public string ProtectRegistration(string[] frames) => _registration.Protect(JsonSerializer.Serialize(frames));
    public string[] ReadRegistration(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];
        try { return JsonSerializer.Deserialize<string[]>(_registration.Unprotect(value)) ?? []; }
        catch (Exception e) when (e is CryptographicException or JsonException or FormatException) { return []; }
    }
    public string ProtectIdentity(string frame) => _identity.Protect(NormalizeIdentity(frame));
    public string? ReadIdentity(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try { return _identity.Unprotect(value); }
        catch (CryptographicException) { return null; }
    }
    private static string NormalizeIdentity(string frame)
    {
        try
        {
            var bytes = Convert.FromBase64String(frame);
            if (bytes.Length > 1_050_000) throw new FormatException();
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png)
                || codec.Info.Width < 160 || codec.Info.Height < 160 || codec.Info.Width > 4096 || codec.Info.Height > 4096) throw new FormatException();
            using var image = FaceImage.Decode(codec);
            var scale = Math.Min(1d, 960d / Math.Max(image.Width, image.Height));
            using var normalized = image.Resize((int)Math.Round(image.Width * scale), (int)Math.Round(image.Height * scale));
            return Convert.ToBase64String(normalized.EncodeJpeg());
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        { throw new FaceEvidenceException("InvalidIdentityPhoto", "Ảnh thẻ phải là JPEG/PNG hợp lệ, cạnh 160–4096 pixel và tối đa 1 MB."); }
    }
    public static string DataUrl(string frame) => "data:image/jpeg;base64," + frame;
}
