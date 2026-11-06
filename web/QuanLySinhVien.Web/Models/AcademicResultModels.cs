namespace QuanLySinhVien.Web.Models;

public sealed class AcademicResult
{
    public decimal? GpaTichLuy { get; set; }
    public int TinChiTichLuy { get; set; }
    public int TinChiChuaCoDiem { get; set; }
    public List<AcademicSemesterResult> HocKy { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class AcademicSemesterResult
{
    public string MaHocKy { get; set; } = "";
    public string TenHocKy { get; set; } = "";
    public decimal? GpaHocKy { get; set; }
    public int TinChiDat { get; set; }
    public decimal? GpaTichLuy { get; set; }
    public int TinChiTichLuy { get; set; }
    public List<AcademicCourseResult> MonHoc { get; set; } = [];
}

public sealed class AcademicCourseResult
{
    public string MaMonHoc { get; set; } = "";
    public string TenMonHoc { get; set; } = "";
    public string? MaLopHocPhan { get; set; }
    public int SoTinChi { get; set; }
    public decimal? DiemTongKet { get; set; }
    public string? DiemChu { get; set; }
    public decimal? Diem4 { get; set; }
    public string TrangThai { get; set; } = "ChuaCoDiem";
    public AcademicAttendanceTally? ChuyenCan { get; set; }
}

public sealed class AcademicAttendanceTally
{
    public int CoMat { get; set; }
    public int DiTre { get; set; }
    public int CoPhep { get; set; }
    public int Vang { get; set; }
    public int TongSoBuoi { get; set; }
    public string? MucCanhBao { get; set; }
}
