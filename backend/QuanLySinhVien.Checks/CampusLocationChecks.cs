using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

internal static class CampusLocationChecks
{
    private static int _count;

    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }

    // Cổng chính HUIT, dùng làm mốc cho các phép thử dưới đây.
    private const double ViDoCoSo = 10.785;
    private const double KinhDoCoSo = 106.624;

    private static CampusLocationSettings CauHinh(double banKinh = 250, double saiSoToiDa = 200) => new()
    {
        ViDo = ViDoCoSo,
        KinhDo = KinhDoCoSo,
        BanKinhMet = banKinh,
        SaiSoToiDaMet = saiSoToiDa
    };

    public static void Run()
    {
        // Haversine: một độ vĩ tuyến xấp xỉ 111 km, lệch dưới 1%.
        var motDo = CampusLocation.KhoangCachMet(0, 0, 1, 0);
        Check(Math.Abs(motDo - 111_195) < 1_000, "một độ vĩ tuyến xấp xỉ 111 km");
        Check(CampusLocation.KhoangCachMet(ViDoCoSo, KinhDoCoSo, ViDoCoSo, KinhDoCoSo) == 0,
            "cùng một điểm thì khoảng cách bằng 0");
        Check(CampusLocation.KhoangCachMet(10, 20, 11, 21) == CampusLocation.KhoangCachMet(11, 21, 10, 20),
            "khoảng cách đối xứng hai chiều");

        // Chưa cấu hình tọa độ thì không kết luận gì, dù thiết bị có gửi vị trí.
        var chuaCauHinh = CampusLocation.DoiChieu(new CampusLocationSettings(),
            new ViTriDiemDanh(ViDoCoSo, KinhDoCoSo, 10));
        Check(chuaCauHinh.KetQua == KetQuaViTri.ChuaXacMinh, "chưa cấu hình cơ sở thì không kết luận");
        Check(chuaCauHinh.KhoangCachMet is null, "chưa cấu hình thì không có khoảng cách");

        Check(CampusLocation.DoiChieu(CauHinh(), null).KetQua == KetQuaViTri.ChuaXacMinh,
            "thiết bị không gửi vị trí thì không kết luận");

        // Ngay tại cơ sở và cách 100 m đều nằm trong bán kính 250 m.
        Check(CampusLocation.DoiChieu(CauHinh(), new ViTriDiemDanh(ViDoCoSo, KinhDoCoSo, 10)).KetQua
            == KetQuaViTri.TrongKhuonVien, "đứng tại cơ sở là trong khuôn viên");
        var cach100 = new ViTriDiemDanh(ViDoCoSo + 0.0009, KinhDoCoSo, 10);
        var ketQua100 = CampusLocation.DoiChieu(CauHinh(), cach100);
        Check(ketQua100.KetQua == KetQuaViTri.TrongKhuonVien, "cách 100 m vẫn trong bán kính 250 m");
        Check(ketQua100.KhoangCachMet is > 90 and < 110, "khoảng cách tính ra xấp xỉ 100 m");

        // Cách 2 km thì ngoài khuôn viên, và lý do nói rõ khoảng cách.
        var cach2Km = new ViTriDiemDanh(ViDoCoSo + 0.018, KinhDoCoSo, 10);
        var ngoai = CampusLocation.DoiChieu(CauHinh(), cach2Km);
        Check(ngoai.KetQua == KetQuaViTri.NgoaiKhuonVien, "cách 2 km là ngoài khuôn viên");
        Check(ngoai.LyDo is not null && ngoai.LyDo.Contains("ngoài bán kính"), "ngoài khuôn viên có nêu lý do");

        // Sai số thiết bị được cộng vào ngưỡng để không kết luận oan.
        var cach300 = new ViTriDiemDanh(ViDoCoSo + 0.0027, KinhDoCoSo, 150);
        Check(CampusLocation.DoiChieu(CauHinh(), cach300).KetQua == KetQuaViTri.TrongKhuonVien,
            "sai số 150 m nới ngưỡng nên cách 300 m vẫn được chấp nhận");
        Check(CampusLocation.DoiChieu(CauHinh(), cach300 with { DoChinhXacMet = 5 }).KetQua
            == KetQuaViTri.NgoaiKhuonVien, "định vị chính xác thì cách 300 m là ngoài khuôn viên");

        // Sai số quá lớn thì tọa độ không đủ tin để kết luận.
        var saiSoLon = CampusLocation.DoiChieu(CauHinh(), new ViTriDiemDanh(ViDoCoSo, KinhDoCoSo, 500));
        Check(saiSoLon.KetQua == KetQuaViTri.ChuaXacMinh, "sai số vượt ngưỡng thì chưa xác minh");
        Check(saiSoLon.KhoangCachMet is not null, "sai số lớn vẫn ghi lại khoảng cách để tra cứu");

        // Tọa độ vô lý bị loại thay vì tính ra một khoảng cách sai.
        Check(!CampusLocation.HopLe(new ViTriDiemDanh(91, 0, null)), "vĩ độ ngoài [-90, 90] là không hợp lệ");
        Check(!CampusLocation.HopLe(new ViTriDiemDanh(0, 181, null)), "kinh độ ngoài [-180, 180] là không hợp lệ");
        Check(!CampusLocation.HopLe(new ViTriDiemDanh(double.NaN, 0, null)), "NaN là không hợp lệ");
        Check(!CampusLocation.HopLe(new ViTriDiemDanh(0, 0, -1)), "sai số âm là không hợp lệ");
        Check(CampusLocation.DoiChieu(CauHinh(), new ViTriDiemDanh(91, 0, null)).KetQua
            == KetQuaViTri.ChuaXacMinh, "tọa độ không hợp lệ thì không kết luận");

        // Ghi vào bản ghi điểm danh.
        var banGhi = new ChiTietDiemDanh();
        CampusLocation.GhiNhanVao(banGhi, CauHinh(),
            new ViTriRequest { ViDo = ViDoCoSo, KinhDo = KinhDoCoSo, DoChinhXacMet = 12 });
        Check(banGhi.ViDo == ViDoCoSo && banGhi.KinhDo == KinhDoCoSo, "lưu lại tọa độ sinh viên gửi lên");
        Check(banGhi.DoChinhXacMet == 12, "lưu lại sai số định vị");
        Check(banGhi.KetQuaViTri == "TrongKhuonVien", "lưu kết luận trong khuôn viên");

        var khongViTri = new ChiTietDiemDanh();
        CampusLocation.GhiNhanVao(khongViTri, CauHinh(), null);
        Check(khongViTri.ViDo is null && khongViTri.KinhDo is null, "không gửi vị trí thì không lưu tọa độ");
        Check(khongViTri.KetQuaViTri is null, "chưa xác minh được thì để trống kết luận");

        var toaDoHong = new ChiTietDiemDanh();
        CampusLocation.GhiNhanVao(toaDoHong, CauHinh(), new ViTriRequest { ViDo = 91, KinhDo = 0 });
        Check(toaDoHong.ViDo is null, "tọa độ không hợp lệ thì không lưu vào bản ghi");
        Check(toaDoHong.KetQuaViTri is null, "tọa độ không hợp lệ thì để trống kết luận");

        Console.WriteLine($"PASS: {_count} checks for the campus location check.");
    }
}
