namespace QuanLySinhVien.Api.Models.Entities;

public sealed class HoSoHocVu
{
    public string MaSinhVien { get; set; } = "";
    public int ThuTuHocKy { get; set; }
    // Imported/confirmed by academic staff, not inferred from an incomplete transcript.
    public decimal? GpaTichLuy { get; set; }
    public bool LaHocKyDau { get; set; }
    public bool LaHocKyCuoi { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
}

public sealed class KetQuaHocPhan
{
    public string MaSinhVien { get; set; } = "";
    public string MaMonHoc { get; set; } = "";
    public string MaHocKy { get; set; } = "";
    public string DiemChu { get; set; } = "";
    public bool DaCongBo { get; set; }
    public SinhVien SinhVien { get; set; } = null!;
    public HocKyThucTe HocKy { get; set; } = null!;
}

public sealed class DieuKienHocPhan
{
    public Guid KeHoachDaoTaoId { get; set; }
    public string MaMonHoc { get; set; } = "";
    public string MaMonDieuKien { get; set; } = "";
    // tienquyet: passed; hoctruoc: final grade, including F; songhanh: enrolled or passed.
    public string Loai { get; set; } = "tienquyet";
}

public sealed class DotDangKyHocPhan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenDot { get; set; } = "";
    public string MaHocKy { get; set; } = "";
    public Guid? KeHoachDaoTaoId { get; set; }
    public DateTime MoLucUtc { get; set; }
    public DateTime DongLucUtc { get; set; }
    public bool ChoPhepDangKy { get; set; } = true;
    public bool LaHocKyHe { get; set; }
    public HocKyThucTe HocKy { get; set; } = null!;
}
