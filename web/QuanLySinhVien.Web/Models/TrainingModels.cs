namespace QuanLySinhVien.Web.Models;

public sealed class TrainingOptionModel { public string Code { get; set; } = ""; public string Name { get; set; } = ""; }
public sealed class TrainingCourseModel { public string MaMonHoc { get; set; } = ""; public string TenMonHoc { get; set; } = ""; }
public sealed class TrainingSemesterModel { public string MaHocKy { get; set; } = ""; public string TenHocKy { get; set; } = ""; }
public sealed class TrainingLecturerModel { public string MaGiangVien { get; set; } = ""; public string HoTen { get; set; } = ""; public string? ChuyenNganh { get; set; } }
public sealed class TrainingRoomModel { public string MaPhong { get; set; } = ""; public string TenPhong { get; set; } = ""; }
public sealed class TrainingCatalogModel
{
    public List<TrainingOptionModel> NganhHocs { get; set; } = [];
    public List<TrainingOptionModel> KhoaTuyenSinhs { get; set; } = [];
    public List<TrainingCourseModel> MonHocs { get; set; } = [];
    public List<TrainingSemesterModel> HocKys { get; set; } = [];
    public List<TrainingLecturerModel> GiangViens { get; set; } = [];
    public List<TrainingRoomModel> PhongHocs { get; set; } = [];
}
public sealed class CurriculumCourseModel
{
    public string MaMonHoc { get; set; } = "";
    public string TenMonHoc { get; set; } = "";
    public int ThuTuHocKy { get; set; } = 1;
    public bool BatBuoc { get; set; } = true;
}
public sealed class CurriculumModel
{
    public Guid Id { get; set; }
    public string MaNganh { get; set; } = "";
    public string MaKhoa { get; set; } = "";
    public string TenKhoaDaoTao { get; set; } = "";
    public string TenChuongTrinh { get; set; } = "";
    public List<CurriculumCourseModel> MonHocs { get; set; } = [];
}
public sealed class PlannedClassModel
{
    public string MaLopHocPhan { get; set; } = "";
    public string MaMonHoc { get; set; } = "";
    public int SiSoToiDa { get; set; }
    public bool DaPhanCong { get; set; }
    public string? MaGiangVien { get; set; }
    public string? TrangThai { get; set; }
}
public sealed class SemesterApplicationModel
{
    public Guid Id { get; set; }
    public Guid KeHoachDaoTaoId { get; set; }
    public string MaHocKy { get; set; } = "";
    public int ThuTuHocKy { get; set; }
    public string TenChuongTrinh { get; set; } = "";
    public string MaNganh { get; set; } = "";
    public string MaKhoa { get; set; } = "";
    public List<CurriculumCourseModel> MonHocs { get; set; } = [];
    public List<PlannedClassModel> LopHocPhans { get; set; } = [];
}
public sealed class TrainingWorkspaceModel
{
    public TrainingCatalogModel Catalog { get; set; } = new();
    public List<CurriculumModel> Curricula { get; set; } = [];
    public List<SemesterApplicationModel> Applications { get; set; } = [];
    public SemesterApplicationModel? Selected { get; set; }
    public CurriculumModel Draft { get; set; } = new();
    public List<string> ImportErrors { get; set; } = [];
    public bool Loaded { get; set; }
}
public sealed class CurriculumPreviewModel
{
    public List<CurriculumCourseModel> MonHocs { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public bool CanSave { get; set; }
}
