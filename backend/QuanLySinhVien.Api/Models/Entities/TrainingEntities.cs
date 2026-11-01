namespace QuanLySinhVien.Api.Models.Entities;

// Bảng mở rộng cho luồng đào tạo; không thay đổi các hồ sơ/CTĐT legacy.
public sealed class KeHoachDaoTao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MaNganh { get; set; } = "";
    public string MaKhoa { get; set; } = ""; // khóa tuyển sinh, không phải khoa chuyên môn
    public string TenKhoaDaoTao { get; set; } = "";
    public string TenChuongTrinh { get; set; } = "";
    public ICollection<MonKeHoachDaoTao> MonHocs { get; set; } = new List<MonKeHoachDaoTao>();
}

public sealed class MonKeHoachDaoTao
{
    public Guid KeHoachDaoTaoId { get; set; }
    public string MaMonHoc { get; set; } = "";
    public int ThuTuHocKy { get; set; }
    public bool BatBuoc { get; set; } = true;
    public KeHoachDaoTao KeHoach { get; set; } = null!;
    public MonHoc MonHoc { get; set; } = null!;
}

public sealed class ApDungHocKy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KeHoachDaoTaoId { get; set; }
    public string MaHocKy { get; set; } = "";
    public int ThuTuHocKy { get; set; }
    public KeHoachDaoTao KeHoach { get; set; } = null!;
    public ICollection<LopHocPhanDuKien> LopHocPhans { get; set; } = new List<LopHocPhanDuKien>();
}

// Lớp chờ phân công chưa đưa vào LopHocPhan (bảng cũ bắt buộc có giảng viên).
public sealed class LopHocPhanDuKien
{
    public string MaLopHocPhan { get; set; } = "";
    public Guid ApDungHocKyId { get; set; }
    public string MaMonHoc { get; set; } = "";
    public int SiSoToiDa { get; set; }
    public bool DaPhanCong { get; set; }
    public ApDungHocKy ApDung { get; set; } = null!;
    public MonHoc MonHoc { get; set; } = null!;
}
