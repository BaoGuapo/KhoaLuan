using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Faces;

public sealed record FaceProfileMetadataResponse(
    string MaSinhVien,
    bool DaDangKy,
    DateTime? ThoiGianCapNhat);

/// <summary>Vị trí thiết bị lúc điểm danh. Không bắt buộc; thiếu thì ghi là chưa xác minh.</summary>
public sealed record ViTriRequest
{
    [Range(-90, 90)] public double ViDo { get; init; }
    [Range(-180, 180)] public double KinhDo { get; init; }
    [Range(0, 100_000)] public double? DoChinhXacMet { get; init; }
}

public sealed class FaceDescriptorSamplesRequest
{
    [Required, MinLength(3), MaxLength(5)]
    public IReadOnlyCollection<double[]> Descriptors { get; init; } = [];

    public ViTriRequest? ViTri { get; init; }
}

public sealed record FaceAttendanceResponse(
    string MaSinhVien,
    string HoTen,
    string MaBuoiHoc,
    string TrangThai,
    DateTime ThoiGianCheckIn,
    bool DaDiemDanhTruocDo,
    string KetQuaViTri,
    double? KhoangCachMet,
    string? GhiChuViTri);
