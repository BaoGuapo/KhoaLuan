using QuanLySinhVien.Api.Services;

internal static class ClassPeriodChecks
{
    private static int _count;

    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }

    public static void Run()
    {
        Check(ClassPeriods.GioBatDauTiet(1) == new TimeOnly(7, 0), "tiết 1 bắt đầu 07:00");
        Check(ClassPeriods.GioKetThucTiet(1) == new TimeOnly(7, 50), "tiết 1 kết thúc 07:50");
        Check(ClassPeriods.GioBatDauTiet(6) == new TimeOnly(13, 0), "tiết 6 bắt đầu 13:00");
        Check(ClassPeriods.GioBatDauTiet(11) == new TimeOnly(18, 0), "tiết 11 bắt đầu 18:00");
        Check(ClassPeriods.GioKetThucTiet(15) == new TimeOnly(22, 40), "tiết 15 kết thúc 22:40");
        Check(ClassPeriods.GioBatDauTiet(0) is null && ClassPeriods.GioBatDauTiet(16) is null,
            "tiết ngoài bảng giờ không có giờ học");

        // Buổi học thứ Ba 03/11/2026, tiết 1–3: mở 06:45 và đóng 09:50 giờ Việt Nam.
        var ngayHoc = new DateTime(2026, 11, 3);
        var khung = ClassPeriods.KhungGioDiemDanh(ngayHoc, 1, 3);
        Check(khung is not null, "lịch học trong bảng giờ có khung điểm danh");
        Check(khung!.Value.MoLuc == ngayHoc.AddHours(6).AddMinutes(45), "mở trước tiết đầu 15 phút");
        Check(khung.Value.DongLuc == ngayHoc.AddHours(9).AddMinutes(50), "đóng khi hết tiết cuối");
        Check(ClassPeriods.KhungGioDiemDanh(ngayHoc, 1, 99) is null, "tiết cuối ngoài bảng giờ bị loại");

        // Giờ Việt Nam = UTC + 7, nên mốc UTC dưới đây là 06:44, 06:45, 09:50 và 09:51 tại Việt Nam.
        var utcTruocGio = ngayHoc.AddHours(6).AddMinutes(44).AddHours(-7);
        Check(!ClassPeriods.DangTrongKhungGio(ngayHoc, 1, 3, utcTruocGio), "trước giờ mở thì khóa");
        Check(ClassPeriods.DangTrongKhungGio(ngayHoc, 1, 3, utcTruocGio.AddMinutes(1)), "đúng lúc mở thì cho điểm danh");
        Check(ClassPeriods.DangTrongKhungGio(ngayHoc, 1, 3, ngayHoc.AddHours(8).AddHours(-7)), "trong giờ lên lớp thì cho điểm danh");
        Check(ClassPeriods.DangTrongKhungGio(ngayHoc, 1, 3, ngayHoc.AddHours(9).AddMinutes(50).AddHours(-7)), "đúng lúc đóng vẫn cho điểm danh");
        Check(!ClassPeriods.DangTrongKhungGio(ngayHoc, 1, 3, ngayHoc.AddHours(9).AddMinutes(51).AddHours(-7)), "sau giờ đóng thì khóa");

        // Cùng giờ nhưng khác ngày học: chỉ buổi của hôm đó được mở.
        var trongGio = ngayHoc.AddHours(8).AddHours(-7);
        Check(!ClassPeriods.DangTrongKhungGio(ngayHoc.AddDays(-7), 1, 3, trongGio), "buổi tuần trước vẫn khóa");
        Check(!ClassPeriods.DangTrongKhungGio(ngayHoc.AddDays(7), 1, 3, trongGio), "buổi tuần sau vẫn khóa");
        Check(!ClassPeriods.DangTrongKhungGio(ngayHoc, 99, 99, trongGio), "lịch học sai tiết thì khóa");

        Check(ClassPeriods.MoTaTiet(1, 3) == "07:00–09:50", "mô tả khung giờ theo tiết");

        Console.WriteLine($"PASS: {_count} checks for the attendance window.");
    }
}
