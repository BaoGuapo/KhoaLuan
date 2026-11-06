namespace QuanLySinhVien.Web.Models;

public class MonHocViewModel
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string? MaNganh { get; set; }
    public int SoTinChi { get; set; }
    public int SoTietLyThuyet { get; set; }
    public int SoTietThucHanh { get; set; }
    public bool LaMonTuChon { get; set; }
}