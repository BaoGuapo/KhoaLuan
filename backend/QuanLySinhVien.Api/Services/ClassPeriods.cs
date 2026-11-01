namespace QuanLySinhVien.Api.Services;

/// <summary>
/// Bảng giờ tiết học và khung giờ cho phép giảng viên điểm danh một buổi học.
/// Giờ học là giờ Việt Nam (UTC+7), trùng với cách các controller khác quy đổi.
/// </summary>
public static class ClassPeriods
{
    public const int SoPhutMoiTiet = 50;
    public const int SoPhutMoSom = 15;
    private static readonly TimeSpan OffsetVietNam = TimeSpan.FromHours(7);

    // Tiết 1–5 học sáng, 6–10 học chiều, 11–15 học tối; mỗi tiết 50 phút.
    private static readonly TimeOnly[] GioBatDau =
    [
        new(7, 0), new(7, 55), new(9, 0), new(9, 55), new(10, 50),
        new(13, 0), new(13, 55), new(15, 0), new(15, 55), new(16, 50),
        new(18, 0), new(18, 55), new(20, 0), new(20, 55), new(21, 50)
    ];

    public static int SoTietToiDa => GioBatDau.Length;

    public static DateTime BayGioVietNam(DateTime? utcNow = null) => (utcNow ?? DateTime.UtcNow) + OffsetVietNam;

    public static TimeOnly? GioBatDauTiet(int tiet) =>
        tiet >= 1 && tiet <= GioBatDau.Length ? GioBatDau[tiet - 1] : null;

    public static TimeOnly? GioKetThucTiet(int tiet) =>
        GioBatDauTiet(tiet)?.AddMinutes(SoPhutMoiTiet);

    /// <summary>
    /// Khung giờ điểm danh của một buổi học, tính theo giờ Việt Nam:
    /// mở trước tiết đầu <see cref="SoPhutMoSom"/> phút và đóng khi hết tiết cuối.
    /// Trả về null khi lịch học khai báo tiết ngoài bảng giờ.
    /// </summary>
    public static (DateTime MoLuc, DateTime DongLuc)? KhungGioDiemDanh(
        DateTime ngayHoc,
        int tietBatDau,
        int tietKetThuc)
    {
        var batDau = GioBatDauTiet(tietBatDau);
        // Lịch học sai dữ liệu (tiết cuối trước tiết đầu) vẫn phải cho ra một khung giờ hợp lý.
        var ketThuc = GioKetThucTiet(Math.Max(tietBatDau, tietKetThuc));
        if (batDau is null || ketThuc is null) return null;

        var ngay = ngayHoc.Date;
        return (ngay.Add(batDau.Value.ToTimeSpan()).AddMinutes(-SoPhutMoSom), ngay.Add(ketThuc.Value.ToTimeSpan()));
    }

    /// <summary>
    /// Buổi học có đang trong khung giờ điểm danh hay không, xét theo giờ Việt Nam hiện tại.
    /// Buổi không nằm trong bảng giờ luôn bị khóa để tránh mở sai thời điểm.
    /// </summary>
    public static bool DangTrongKhungGio(
        DateTime ngayHoc,
        int tietBatDau,
        int tietKetThuc,
        DateTime? utcNow = null)
    {
        var khung = KhungGioDiemDanh(ngayHoc, tietBatDau, tietKetThuc);
        if (khung is null) return false;
        var bayGio = BayGioVietNam(utcNow);
        return bayGio >= khung.Value.MoLuc && bayGio <= khung.Value.DongLuc;
    }

    public static string MoTaTiet(int tietBatDau, int tietKetThuc)
    {
        var batDau = GioBatDauTiet(tietBatDau);
        var ketThuc = GioKetThucTiet(Math.Max(tietBatDau, tietKetThuc));
        return batDau is null || ketThuc is null
            ? $"Tiết {tietBatDau}–{tietKetThuc}"
            : $"{batDau:HH\\:mm}–{ketThuc:HH\\:mm}";
    }
}
