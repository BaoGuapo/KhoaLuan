namespace QuanLySinhVien.Api.Data;

// Tên nghiệp vụ ở API; mã tinyint trong SQL legacy.
public static class LegacyAcademicValues
{
    public static byte SessionCode(string value) => value switch
    { "ChuaDienRa" or "0" => 0, "DangDienRa" or "1" => 1, "DaChot" or "2" => 2, _ => throw new ArgumentException("Trạng thái buổi học không hợp lệ.") };
    public static string SessionName(byte value) => value switch
    { 0 => "ChuaDienRa", 1 => "DangDienRa", 2 => "DaChot", _ => value.ToString() };
    public static byte WarningCode(string value) => value switch
    { "BinhThuong" or "0" => 0, "CanhBao" or "1" => 1, "CamThi" or "2" => 2, _ => throw new ArgumentException("Trạng thái cảnh báo không hợp lệ.") };
    public static string WarningName(byte value) => value switch
    { 0 => "BinhThuong", 1 => "CanhBao", 2 => "CamThi", _ => value.ToString() };
}
