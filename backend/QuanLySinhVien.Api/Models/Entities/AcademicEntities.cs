namespace QuanLySinhVien.Api.Models.Entities;

public sealed class SinhVien
{
    public string MaSinhVien { get; set; } = string.Empty;
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

    // --- THÔNG TIN CÔNG TÁC ---
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
}

public sealed class HocKyThucTe
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public string LoaiHocKy { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
}

public sealed class PhongHoc
{
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
    public string? DiaChiCoSo { get; set; }
    public string? LoaiPhong { get; set; }
}

public sealed class LopHocPhan
{
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string MaMonHoc { get; set; } = string.Empty;
    public string MaHocKy { get; set; } = string.Empty;
    public string MaGiangVien { get; set; } = string.Empty;
    public int SiSoToiDa { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public MonHoc MonHoc { get; set; } = null!;
    public GiangVien GiangVien { get; set; } = null!;
    public ICollection<LichHoc> LichHocs { get; set; } = new List<LichHoc>();
    public ICollection<DanhSachSinhVienLop> DanhSachSinhVien { get; set; } = new List<DanhSachSinhVienLop>();
}

public sealed class DanhSachSinhVienLop
{
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
    public BuoiHoc BuoiHoc { get; set; } = null!;
    public SinhVien SinhVien { get; set; } = null!;
}

public sealed class HoSoKhuonMat
{
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
