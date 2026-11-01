using QuanLySinhVien.Api.Contracts.Faces;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

/// <summary>
/// Tọa độ cơ sở dùng để đối chiếu vị trí lúc điểm danh.
/// Chưa cấu hình thì việc kiểm bị tắt, điểm danh vẫn chạy như cũ.
/// </summary>
public sealed class CampusLocationSettings
{
    public const string SectionName = "CampusLocation";

    public double? ViDo { get; set; }
    public double? KinhDo { get; set; }

    /// <summary>
    /// Bán kính coi là trong khuôn viên. Mặc định 250 m: GPS trong nhà sai số
    /// hàng chục mét nên bán kính hẹp hơn sẽ báo động giả cho sinh viên ngồi
    /// đúng lớp.
    /// </summary>
    public double BanKinhMet { get; set; } = 250;

    /// <summary>
    /// Sai số định vị lớn hơn ngưỡng này thì tọa độ không đủ tin để kết luận,
    /// ghi nhận là chưa xác minh được thay vì báo ngoài khuôn viên.
    /// </summary>
    public double SaiSoToiDaMet { get; set; } = 200;

    public bool DaCauHinh => ViDo is not null && KinhDo is not null;
}

/// <summary>Kết quả đối chiếu vị trí của một lượt điểm danh.</summary>
public enum KetQuaViTri
{
    /// <summary>Chưa cấu hình tọa độ cơ sở, hoặc máy không gửi vị trí.</summary>
    ChuaXacMinh,
    TrongKhuonVien,
    NgoaiKhuonVien
}

public sealed record ViTriDiemDanh(double ViDo, double KinhDo, double? DoChinhXacMet);

public sealed record DoiChieuViTri(KetQuaViTri KetQua, double? KhoangCachMet, string? LyDo);

public static class CampusLocation
{
    private const double BanKinhTraiDatMet = 6_371_000;

    /// <summary>
    /// Đối chiếu vị trí sinh viên gửi lên với tọa độ cơ sở. Không bao giờ chặn
    /// điểm danh: hàm chỉ trả về kết luận để lưu lại và hiện cho giảng viên.
    /// </summary>
    public static DoiChieuViTri DoiChieu(CampusLocationSettings cauHinh, ViTriDiemDanh? viTri)
    {
        if (!cauHinh.DaCauHinh)
            return new DoiChieuViTri(KetQuaViTri.ChuaXacMinh, null, "Chưa cấu hình tọa độ cơ sở.");
        if (viTri is null)
            return new DoiChieuViTri(KetQuaViTri.ChuaXacMinh, null, "Thiết bị không gửi vị trí.");
        if (!HopLe(viTri))
            return new DoiChieuViTri(KetQuaViTri.ChuaXacMinh, null, "Tọa độ không hợp lệ.");

        var khoangCach = KhoangCachMet(cauHinh.ViDo!.Value, cauHinh.KinhDo!.Value, viTri.ViDo, viTri.KinhDo);

        if (viTri.DoChinhXacMet is > 0 && viTri.DoChinhXacMet > cauHinh.SaiSoToiDaMet)
            return new DoiChieuViTri(KetQuaViTri.ChuaXacMinh, khoangCach,
                $"Sai số định vị {viTri.DoChinhXacMet:0} m, vượt ngưỡng {cauHinh.SaiSoToiDaMet:0} m.");

        // Cộng thêm sai số thiết bị báo để không kết luận oan khi định vị kém.
        var nguong = cauHinh.BanKinhMet + Math.Max(0, viTri.DoChinhXacMet ?? 0);
        return khoangCach <= nguong
            ? new DoiChieuViTri(KetQuaViTri.TrongKhuonVien, khoangCach, null)
            : new DoiChieuViTri(KetQuaViTri.NgoaiKhuonVien, khoangCach,
                $"Cách cơ sở {khoangCach:0} m, ngoài bán kính {cauHinh.BanKinhMet:0} m.");
    }

    public static bool HopLe(ViTriDiemDanh viTri) =>
        double.IsFinite(viTri.ViDo) && double.IsFinite(viTri.KinhDo)
        && viTri.ViDo is >= -90 and <= 90 && viTri.KinhDo is >= -180 and <= 180
        && (viTri.DoChinhXacMet is null || (double.IsFinite(viTri.DoChinhXacMet.Value) && viTri.DoChinhXacMet >= 0));

    /// <summary>
    /// Đối chiếu rồi ghi vị trí vào bản ghi điểm danh. Trả lại kết luận để
    /// endpoint đưa vào phản hồi cho sinh viên biết.
    /// </summary>
    public static DoiChieuViTri GhiNhanVao(
        ChiTietDiemDanh banGhi, CampusLocationSettings cauHinh, ViTriRequest? yeuCau)
    {
        var viTri = yeuCau is null ? null : new ViTriDiemDanh(yeuCau.ViDo, yeuCau.KinhDo, yeuCau.DoChinhXacMet);
        var ketQua = DoiChieu(cauHinh, viTri);
        if (viTri is not null && HopLe(viTri))
        {
            banGhi.ViDo = viTri.ViDo;
            banGhi.KinhDo = viTri.KinhDo;
            banGhi.DoChinhXacMet = viTri.DoChinhXacMet;
        }
        banGhi.KhoangCachMet = ketQua.KhoangCachMet;
        banGhi.KetQuaViTri = ketQua.KetQua == KetQuaViTri.ChuaXacMinh ? null : ketQua.KetQua.ToString();
        return ketQua;
    }

    /// <summary>Khoảng cách theo công thức haversine, đủ chính xác ở quy mô khuôn viên.</summary>
    public static double KhoangCachMet(double viDo1, double kinhDo1, double viDo2, double kinhDo2)
    {
        var p1 = double.DegreesToRadians(viDo1);
        var p2 = double.DegreesToRadians(viDo2);
        var dP = double.DegreesToRadians(viDo2 - viDo1);
        var dL = double.DegreesToRadians(kinhDo2 - kinhDo1);
        var a = Math.Sin(dP / 2) * Math.Sin(dP / 2)
                + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dL / 2) * Math.Sin(dL / 2);
        return 2 * BanKinhTraiDatMet * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
