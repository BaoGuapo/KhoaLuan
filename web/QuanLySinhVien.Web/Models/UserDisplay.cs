using System.Security.Claims;

namespace QuanLySinhVien.Web.Models;

/// <summary>
/// Tên và ảnh đại diện hiển thị trên thanh trên và thanh điều hướng.
/// Họ tên lấy từ claim được gắn lúc đăng nhập, không gọi thêm API mỗi lần dựng trang.
/// </summary>
public static class UserDisplay
{
    public const string FullNameClaim = "full_name";

    /// <summary>Họ tên để hiển thị; chưa có thì lùi về mã đăng nhập.</summary>
    public static string TenHienThi(this ClaimsPrincipal user)
    {
        var hoTen = user.FindFirst(FullNameClaim)?.Value;
        return string.IsNullOrWhiteSpace(hoTen) ? MaDangNhap(user) : hoTen;
    }

    /// <summary>Mã đăng nhập (mã sinh viên, mã giảng viên hoặc tài khoản quản trị).</summary>
    public static string MaDangNhap(this ClaimsPrincipal user) => user.Identity?.Name ?? string.Empty;

    /// <summary>Số ảnh pravatar hợp lệ; dịch vụ chỉ có img=1 đến img=70.</summary>
    private const int SoAnhMau = 70;

    /// <summary>
    /// Ảnh đại diện là ảnh thẻ chụp lúc nhập học. Route /anh-dai-dien trả ảnh thẻ nếu có,
    /// chưa có thì chuyển sang ảnh mẫu, nên mọi tài khoản đều dùng chung một đường dẫn.
    /// </summary>
    public static string AnhDaiDien(this ClaimsPrincipal user) => "/anh-dai-dien";

    /// <summary>
    /// Ảnh mẫu tạm thời khi chưa có ảnh thẻ. Chọn theo mã đăng nhập để mỗi tài khoản
    /// luôn ra cùng một ảnh giữa các lần chạy.
    /// </summary>
    public static string AnhMau(this ClaimsPrincipal user) => AnhMau(MaDangNhap(user));

    public static string AnhMau(string maDangNhap)
    {
        // FNV-1a để mã nào cũng ra cùng một số, khác với GetHashCode vốn đổi theo tiến trình.
        uint hash = 2166136261;
        foreach (var kyTu in maDangNhap)
        {
            hash = (hash ^ kyTu) * 16777619;
        }
        var soAnh = (int)(hash % SoAnhMau) + 1;
        return $"https://i.pravatar.cc/150?img={soAnh}";
    }

    /// <summary>Chữ đầu của tên gọi (từ cuối trong họ tên), dùng khi ảnh chưa tải được.</summary>
    public static string ChuDaiDien(this ClaimsPrincipal user)
    {
        var tu = TenHienThi(user).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tu.Length == 0 ? "?" : tu[^1][..1].ToUpperInvariant();
    }
}
