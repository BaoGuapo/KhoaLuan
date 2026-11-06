using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLySinhVien.Api.Models.Entities;

public sealed class SinhVien
{
    public string MaSinhVien { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    [JsonIgnore]
    public string MatKhauLegacy { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool TrangThaiTaiKhoan { get; set; } = true;
    public string? AnhDaiDien { get; set; }
    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? NoiSinh { get; set; }
    public string? DanToc { get; set; }
    public string? TonGiao { get; set; }
    public string? QuocTich { get; set; }
    public string? KhuVuc { get; set; }
    public string? SoCCCD { get; set; }
    public DateTime? NgayCapCCCD { get; set; }
    public string? NoiCapCCCD { get; set; }
    public string? DiaChiLienHe { get; set; }
    public string? HoKhauThuongTru { get; set; }
    public string? MaHoSo { get; set; }
    public DateTime? NgayVaoTruong { get; set; }
    public string? LopHoc { get; set; }
    public string? CoSo { get; set; }
    public string? BacDaoTao { get; set; }
    public string? LoaiHinhDaoTao { get; set; }
    public string? Khoa { get; set; }
    public string? Nganh { get; set; }
    public string? ChuyenNganh { get; set; }
    public string? KhoaHoc { get; set; }
    public string? DoiTuong { get; set; }
    public string? DienChinhSach { get; set; }
    public DateTime? NgayVaoDoan { get; set; }
    public DateTime? NgayVaoDang { get; set; }
    public ICollection<DanhSachSinhVienLop> DangKyLop { get; set; } = new List<DanhSachSinhVienLop>();
    public HoSoKhuonMat? HoSoKhuonMat { get; set; }
}

public sealed class GiangVien
{
    public string MaGiangVien { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    [JsonIgnore]
    public string MatKhauLegacy { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public bool TrangThaiTaiKhoan { get; set; } = true;
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? NoiSinh { get; set; }
    public string? DanToc { get; set; }
    public string? TonGiao { get; set; }
    public string? QuocTich { get; set; }
    public string? SoCCCD { get; set; }
    public DateTime? NgayCapCCCD { get; set; }
    public string? NoiCapCCCD { get; set; }
    public string? DiaChiLienHe { get; set; }
    public string? HoKhauThuongTru { get; set; }
    public DateTime? NgayBatDauCongTac { get; set; }
    public string? CoSo { get; set; }
    public string? Khoa { get; set; }
    public string? ChuyenNganh { get; set; }
    public string? TrinhDoHocVan { get; set; }
    public string? ChucDanh { get; set; }
    public DateTime? NgayVaoDang { get; set; }
}

public sealed class MonHoc
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoTinChiLyThuyet { get; set; }
    public int SoTinChiThucHanh { get; set; }
    public int SoTinChi { get; set; }
    public int SoTietLyThuyet { get; set; }
    public int SoTietThucHanh { get; set; }
    public string? MaNganh { get; set; }
    public ICollection<LopHocPhan> LopHocPhans { get; set; } = new List<LopHocPhan>();
    public bool LaMonTuChon { get; set; } = false;
}

public sealed class HocKyThucTe
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public string LoaiHocKy { get; set; } = "0";
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
}

public sealed class PhongHoc
{
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
    public string? DiaChiCoSo { get; set; }
    public string? LoaiPhong { get; set; }
}

public class LopHocPhan
{
    public string MaLopHocPhan { get; set; } = string.Empty;

    public string MaMonHoc { get; set; } = string.Empty;
    public string MaGiangVien { get; set; } = string.Empty;
    public int SiSoToiDa { get; set; } = 70;
    public string TrangThai { get; set; } = "0";
    public string? MaHocKy { get; set; }

    // Trường lưu Thời khóa biểu cho giao diện xếp lịch
    public string? LoaiPhong { get; set; }
    public int? SoBuoi { get; set; }
    public string? NgayBatDau { get; set; }
    public string? NgayKetThuc { get; set; }
    public string? LichHoc { get; set; }

    // Navigation Properties (Đã khai báo rõ ForeignKey để tránh lỗi MonHocMaMonHoc)
    [ForeignKey("MaMonHoc")]
    public virtual MonHoc? MonHoc { get; set; }

    [ForeignKey("MaGiangVien")]
    public virtual GiangVien? GiangVien { get; set; }

    // Tập hợp liên kết chuẩn của hệ thống
    public virtual ICollection<LichHoc> LichHocs { get; set; } = new List<LichHoc>();
    public virtual ICollection<DanhSachSinhVienLop> DanhSachSinhVien { get; set; } = new List<DanhSachSinhVienLop>();
}
[Table("GiaoTrinh")]
public class GiaoTrinh
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string MaMonHoc { get; set; } = string.Empty;

    [Required]
    public string TenFile { get; set; } = string.Empty;

    [Required]
    public string DuongDanFile { get; set; } = string.Empty;

    public string LoaiFile { get; set; } = string.Empty; // "WORD" hoặc "PPT"

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey("MaMonHoc")]
    public virtual MonHoc? MonHoc { get; set; }
}
public sealed class DanhSachSinhVienLop
{
    public string LoaiDangKy { get; set; } = "moi";
    public string MaSinhVien { get; set; } = string.Empty;
    public string MaLopHocPhan { get; set; } = string.Empty;
    public DateTime ThoiGianDangKy { get; set; }
    public int TongSoBuoiVang { get; set; }
    public string? TrangThaiCanhBao { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public LopHocPhan LopHocPhan { get; set; } = null!;
}

public sealed class LichHoc
{
    public string MaLichHoc { get; set; } = string.Empty;
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string MaPhong { get; set; } = string.Empty;
    public int ThuTrongTuan { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public LopHocPhan LopHocPhan { get; set; } = null!;
    public PhongHoc PhongHoc { get; set; } = null!;
    public ICollection<BuoiHoc> BuoiHocs { get; set; } = new List<BuoiHoc>();
}

public sealed class BuoiHoc
{
    public string MaBuoiHoc { get; set; } = string.Empty;
    public string MaLichHoc { get; set; } = string.Empty;
    public DateTime NgayHocCuThe { get; set; }
    public DateTime? ThoiGianDiemDanh { get; set; }
    public string TrangThaiBuoiHoc { get; set; } = string.Empty;
    public LichHoc LichHoc { get; set; } = null!;
    public ICollection<ChiTietDiemDanh> ChiTietDiemDanhs { get; set; } = new List<ChiTietDiemDanh>();
}

public sealed class ChiTietDiemDanh
{
    public string MaBuoiHoc { get; set; } = string.Empty;
    public string MaSinhVien { get; set; } = string.Empty;
    public DateTime? ThoiGianCheckIn { get; set; }
    public string TrangThai { get; set; } = "Vang";

    // Vị trí lúc điểm danh. Null khi thiết bị không gửi hoặc điểm danh thủ công.
    public double? ViDo { get; set; }
    public double? KinhDo { get; set; }
    public double? DoChinhXacMet { get; set; }
    public double? KhoangCachMet { get; set; }
    /// <summary>"TrongKhuonVien", "NgoaiKhuonVien" hoặc null khi chưa xác minh được.</summary>
    public string? KetQuaViTri { get; set; }

    public BuoiHoc BuoiHoc { get; set; } = null!;
    public SinhVien SinhVien { get; set; } = null!;
}

/// <summary>
/// Vết một lần sửa thủ công kết quả điểm danh của buổi đã qua, ví dụ khi sinh viên
/// xác nhận trực tiếp với giảng viên. Lưu để xem lại ai sửa, lúc nào và vì sao.
/// </summary>
public sealed class LichSuSuaDiemDanh
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MaBuoiHoc { get; set; } = string.Empty;
    public string MaSinhVien { get; set; } = string.Empty;
    /// <summary>Null khi trước đó sinh viên chưa có bản ghi điểm danh.</summary>
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = string.Empty;
    public string LyDo { get; set; } = string.Empty;
    public string NguoiSua { get; set; } = string.Empty;
    public DateTime SuaLucUtc { get; set; }
    public BuoiHoc BuoiHoc { get; set; } = null!;
    public SinhVien SinhVien { get; set; } = null!;
}

public sealed class HoSoKhuonMat
{
    public string? RegistrationPhotosProtected { get; set; }
    public Guid? ReviewVersion { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public string MaHoSo { get; set; } = string.Empty;
    public string MaSinhVien { get; set; } = string.Empty;
    public string VectorJson { get; set; } = string.Empty;
    public DateTime ThoiGianCapNhat { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
}

// Các bảng dưới đây là phần mở rộng bắt buộc bởi UC15–UC34 trong tài liệu đồ án.
public sealed class PhienDiemDanh
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MaBuoiHoc { get; set; } = string.Empty;
    public string QrTokenHash { get; set; } = string.Empty;
    public DateTime MoLucUtc { get; set; }
    public DateTime HetHanLucUtc { get; set; }
    public DateTime? DongLucUtc { get; set; }
    public BuoiHoc BuoiHoc { get; set; } = null!;
    public bool ConHieuLuc => DongLucUtc is null && HetHanLucUtc > DateTime.UtcNow;
}

public sealed class ThongBao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string? MaLopHocPhan { get; set; }
    public DateTime HieuLucTuUtc { get; set; }
    public DateTime TaoLucUtc { get; set; } = DateTime.UtcNow;
}
