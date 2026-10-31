namespace QuanLySinhVien.Web.Models;

public sealed class AttendanceOverviewModel
{
    public int SoSinhVien { get; set; }
    public int SoLopHocPhan { get; set; }
    public int SoMonHoc { get; set; }
    public int SoBuoiDaDienRa { get; set; }
    public int TongSoBuoi { get; set; }
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int ChuaDiemDanh { get; set; }
    public double TyLeChuyenCan { get; set; }

    public int TongLuot => CoMat + DiTre + CoPhep + Vang + ChuaDiemDanh;
}

public sealed class SubjectAttendanceModel
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoLop { get; set; }
    public int SoSinhVien { get; set; }
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int ChuaDiemDanh { get; set; }
    public int SoLuotDuKien { get; set; }
    public double TyLeChuyenCan { get; set; }

    /// <summary>Không có lượt dự kiến thì tỷ lệ chuyên cần chưa có nghĩa.</summary>
    public bool CoSoLieu => SoLuotDuKien > 0;
}

public sealed class ClassAttendanceReportModel
{
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string HoTenGiangVien { get; set; } = string.Empty;
    public int SiSo { get; set; }
    public int SoBuoiDaDienRa { get; set; }
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int ChuaDiemDanh { get; set; }
    public int SoLuotDuKien { get; set; }
    public double TyLeChuyenCan { get; set; }
    public int SoSinhVienCanhBao { get; set; }

    public int TongLuot => CoMat + DiTre + CoPhep + Vang + ChuaDiemDanh;
    public bool CoSoLieu => SoLuotDuKien > 0;
}

public sealed class DailyAttendanceModel
{
    public DateTime Ngay { get; set; }
    public int SoBuoi { get; set; }
    public int SoLuotDuKien { get; set; }
    public int CoMatVaDiTre { get; set; }
    public double TyLeChuyenCan { get; set; }
}

public sealed class AttendanceReportModel
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public AttendanceOverviewModel TongQuan { get; set; } = new();
    public IReadOnlyCollection<SubjectAttendanceModel> TheoMonHoc { get; set; } = [];
    public IReadOnlyCollection<ClassAttendanceReportModel> TheoLopHocPhan { get; set; } = [];
    public IReadOnlyCollection<DailyAttendanceModel> TheoNgay { get; set; } = [];
}

public sealed class SemesterOptionModel
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
}

public sealed class AttendanceReportPageViewModel
{
    public required IReadOnlyCollection<SemesterOptionModel> HocKy { get; init; }
    public string? MaHocKyDangChon { get; init; }
    public AttendanceReportModel? BaoCao { get; init; }
}
