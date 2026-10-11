using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Contracts.Faces;

public sealed record FaceProfileMetadataResponse(
    string MaSinhVien,
    bool DaDangKy,
    DateTime? ThoiGianCapNhat);

public sealed class FaceDescriptorSamplesRequest
{
    [Required, MinLength(3), MaxLength(5)]
    public IReadOnlyCollection<double[]> Descriptors { get; init; } = [];
}

public sealed record FaceAttendanceResponse(
    string MaSinhVien,
    string HoTen,
    string MaBuoiHoc,
    string TrangThai,
    DateTime ThoiGianCheckIn,
    bool DaDiemDanhTruocDo);
