using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Web.Models;

public sealed record PagedApiResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record UserSummaryModel(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAtUtc,
    string? ProfileType,
    string? ProfileCode);

public sealed record UserDetailModel(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAtUtc,
    string? ProfileType,
    string? ProfileCode,
    string? PhoneNumber,
    AcademicProfileModel? AcademicProfile);

public sealed class AcademicProfileModel
{
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
    public string? CoSo { get; set; }
    public string? Khoa { get; set; }
    public string? ChuyenNganh { get; set; }
    public string? Nganh { get; set; }
    public string? LopHoc { get; set; }
    public string? KhoaHoc { get; set; }
    public string? BacDaoTao { get; set; }
    public string? LoaiHinhDaoTao { get; set; }
    public DateTime? NgayVaoTruong { get; set; }
    public DateTime? NgayBatDauCongTac { get; set; }
    public string? TrinhDoHocVan { get; set; }
    public string? ChucDanh { get; set; }
}

public class PersonalAcademicProfileModel
{
    [MaxLength(500), Display(Name = "Ảnh đại diện (URL)")] public string? AnhDaiDien { get; set; }
    [MaxLength(20), Display(Name = "Giới tính")] public string? GioiTinh { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày sinh")] public DateTime? NgaySinh { get; set; }
    [MaxLength(200), Display(Name = "Nơi sinh")] public string? NoiSinh { get; set; }
    [MaxLength(100), Display(Name = "Dân tộc")] public string? DanToc { get; set; }
    [MaxLength(100), Display(Name = "Tôn giáo")] public string? TonGiao { get; set; }
    [MaxLength(100), Display(Name = "Quốc tịch")] public string? QuocTich { get; set; }
    [MaxLength(30), Display(Name = "Số CCCD")] public string? SoCCCD { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày cấp CCCD")] public DateTime? NgayCapCCCD { get; set; }
    [MaxLength(200), Display(Name = "Nơi cấp CCCD")] public string? NoiCapCCCD { get; set; }
    [MaxLength(500), Display(Name = "Địa chỉ liên hệ")] public string? DiaChiLienHe { get; set; }
    [MaxLength(500), Display(Name = "Hộ khẩu thường trú")] public string? HoKhauThuongTru { get; set; }
}

public sealed class SinhVienModel : PersonalAcademicProfileModel
{
    public string MaSinhVien { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool TrangThaiTaiKhoan { get; set; }
    public string? KhuVuc { get; set; }
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
}

public sealed class GiangVienModel : PersonalAcademicProfileModel
{
    public string MaGiangVien { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public bool TrangThaiTaiKhoan { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public DateTime? NgayBatDauCongTac { get; set; }
    public string? CoSo { get; set; }
    public string? Khoa { get; set; }
    public string? ChuyenNganh { get; set; }
    public string? TrinhDoHocVan { get; set; }
    public string? ChucDanh { get; set; }
    public DateTime? NgayVaoDang { get; set; }
}

public sealed class UserListViewModel
{
    public required PagedApiResponse<UserSummaryModel> Result { get; init; }
    public string? Search { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class CreateStaffAccountViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã tài khoản."), MaxLength(100)]
    [Display(Name = "Mã tài khoản")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên."), MaxLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tạm")]
    public string InitialPassword { get; set; } = string.Empty;

    [Required, Display(Name = "Vai trò")]
    public string Role { get; set; } = RoleNames.CanBoQuanLy;
}

public sealed class ResetPasswordViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tạm mới")]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(TemporaryPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu tạm")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class SinhVienFormViewModel : PersonalAcademicProfileModel
{
    [Required, MaxLength(30), Display(Name = "Mã sinh viên")]
    public string MaSinhVien { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30), Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [MaxLength(100), Display(Name = "Khu vực")] public string? KhuVuc { get; set; }
    [MaxLength(50), Display(Name = "Mã hồ sơ")] public string? MaHoSo { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày vào trường")] public DateTime? NgayVaoTruong { get; set; }
    [MaxLength(100), Display(Name = "Lớp học")] public string? LopHoc { get; set; }
    [MaxLength(200), Display(Name = "Cơ sở")] public string? CoSo { get; set; }
    [MaxLength(100), Display(Name = "Bậc đào tạo")] public string? BacDaoTao { get; set; }
    [MaxLength(100), Display(Name = "Loại hình đào tạo")] public string? LoaiHinhDaoTao { get; set; }
    [MaxLength(200), Display(Name = "Khoa")] public string? Khoa { get; set; }
    [MaxLength(200), Display(Name = "Ngành")] public string? Nganh { get; set; }
    [MaxLength(200), Display(Name = "Chuyên ngành")] public string? ChuyenNganh { get; set; }
    [MaxLength(50), Display(Name = "Khóa học")] public string? KhoaHoc { get; set; }
    [MaxLength(100), Display(Name = "Đối tượng")] public string? DoiTuong { get; set; }
    [MaxLength(100), Display(Name = "Diện chính sách")] public string? DienChinhSach { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày vào Đoàn")] public DateTime? NgayVaoDoan { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày vào Đảng")] public DateTime? NgayVaoDang { get; set; }
}

public sealed class GiangVienFormViewModel : PersonalAcademicProfileModel
{
    [Required, MaxLength(30), Display(Name = "Mã giảng viên")]
    public string MaGiangVien { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(256)] public string? Email { get; set; }
    [MaxLength(20), Display(Name = "Số điện thoại")] public string? SoDienThoai { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày bắt đầu công tác")] public DateTime? NgayBatDauCongTac { get; set; }
    [MaxLength(200), Display(Name = "Cơ sở")] public string? CoSo { get; set; }
    [MaxLength(200), Display(Name = "Khoa")] public string? Khoa { get; set; }
    [MaxLength(200), Display(Name = "Mã hoặc tên chuyên ngành")] public string? ChuyenNganh { get; set; }
    [MaxLength(100), Display(Name = "Trình độ học vấn")] public string? TrinhDoHocVan { get; set; }
    [MaxLength(100), Display(Name = "Chức danh")] public string? ChucDanh { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày vào Đảng")] public DateTime? NgayVaoDang { get; set; }
}

public sealed class ProvisionAccountViewModel
{
    public string ProfileCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsLecturer { get; set; }

    [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tạm")]
    public string InitialPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(InitialPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu tạm")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class PersonalProfileEditViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string? ProfileType { get; set; }

    [Required, MaxLength(200), Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30), Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [MaxLength(500), Display(Name = "Ảnh đại diện (URL)")] public string? AnhDaiDien { get; set; }
    [MaxLength(20), Display(Name = "Giới tính")] public string? GioiTinh { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày sinh")] public DateTime? NgaySinh { get; set; }
    [MaxLength(200), Display(Name = "Nơi sinh")] public string? NoiSinh { get; set; }
    [MaxLength(100), Display(Name = "Dân tộc")] public string? DanToc { get; set; }
    [MaxLength(100), Display(Name = "Tôn giáo")] public string? TonGiao { get; set; }
    [MaxLength(100), Display(Name = "Quốc tịch")] public string? QuocTich { get; set; }
    [MaxLength(500), Display(Name = "Địa chỉ liên hệ")] public string? DiaChiLienHe { get; set; }
    [MaxLength(500), Display(Name = "Hộ khẩu thường trú")] public string? HoKhauThuongTru { get; set; }
}
