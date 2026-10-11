using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Web.Models;

public sealed class FaceProfileMetadataModel
{
    public string MaSinhVien { get; set; } = string.Empty;
    public bool DaDangKy { get; set; }
    public DateTime? ThoiGianCapNhat { get; set; }
}

public sealed class FaceCaptureViewModel
{
    public FaceProfileMetadataModel Profile { get; set; } = new();
    public string DescriptorsJson { get; set; } = string.Empty;
}

public sealed class FaceAttendanceCaptureViewModel
{
    [Required] public string MaLopHocPhan { get; set; } = string.Empty;
    [Required] public string MaBuoiHoc { get; set; } = string.Empty;
    public string? NgayHoc { get; set; }
    public string DescriptorsJson { get; set; } = string.Empty;
}

public sealed class FaceAttendanceResultModel
{
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string MaBuoiHoc { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime ThoiGianCheckIn { get; set; }
    public bool DaDiemDanhTruocDo { get; set; }
}
