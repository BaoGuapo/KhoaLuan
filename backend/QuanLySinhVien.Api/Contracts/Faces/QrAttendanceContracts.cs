using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Faces;

public sealed record OpenAttendanceRequest([Range(1, 30)] int Minutes = 10);
public sealed record ScanAttendanceQrRequest([Required, MaxLength(4096)] string QrToken);
public sealed class FaceEvidenceRequest
{
    public Guid ChallengeId { get; init; }
    [Required, MinLength(3), MaxLength(3)] public string[] Frames { get; init; } = [];
    public bool Consent { get; init; }
    public bool StoreReviewPhotosConsent { get; init; }
}
public sealed class FaceGuidanceRequest
{
    public Guid ChallengeId { get; init; }
    [Required, MaxLength(1_400_000)] public string Frame { get; init; } = string.Empty;
}
public sealed class FacePhotoReviewRequest
{
    public Guid? ProfileVersion { get; init; }
    public Guid? IdentityPhotoVersion { get; init; }
    public bool ConfirmCompared { get; init; }
    [MaxLength(500)] public string? Note { get; init; }
}
public sealed class IdentityPhotoRequest
{
    [Required, MaxLength(1_400_000)] public string Frame { get; init; } = string.Empty;
}
public sealed record AttendanceQrResponse(Guid SessionId, string MaBuoiHoc, string MaLopHocPhan,
    string TenMonHoc, DateTime SessionExpiresAtUtc, string QrToken, DateTime QrExpiresAtUtc);
public sealed record FaceChallengeResponse(Guid ChallengeId, DateTime ExpiresAtUtc, string Direction,
    string MaSinhVien, string HoTen, string? MaBuoiHoc, string? MaLopHocPhan, string? TenMonHoc);
public sealed record AttendanceSessionSummary(string MaBuoiHoc, string MaLopHocPhan, string TenMonHoc,
    DateTime NgayHoc, bool DaChot, Guid? SessionId, DateTime? SessionExpiresAtUtc);
