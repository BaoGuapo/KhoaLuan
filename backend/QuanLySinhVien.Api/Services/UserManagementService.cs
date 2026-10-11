using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Users;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

public sealed class UserManagementService(AppDbContext db)
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<PagedResponse<UserSummaryResponse>> GetUsersAsync(
        string? search,
        string? role,
        bool? isActive,
        int page,
        int pageSize,
        bool canViewStaff,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!canViewStaff)
        {
            query = query.Where(x => x.UserRoles.Any(ur =>
                ur.Role.Name == RoleNames.SinhVien || ur.Role.Name == RoleNames.GiangVien));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.UserName.Contains(keyword)
                || x.FullName.Contains(keyword)
                || x.Email.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.UserRoles.Any(ur => ur.Role.Name == role));
        }
        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var rawItems = await query
            .OrderBy(x => x.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.FullName,
                x.Email,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).OrderBy(name => name).ToArray(),
                x.IsActive,
                x.MustChangePassword,
                x.CreatedAtUtc,
                SinhVienCode = db.SinhViens.Where(s => s.UserId == x.Id).Select(s => s.MaSinhVien).FirstOrDefault(),
                GiangVienCode = db.GiangViens.Where(g => g.UserId == x.Id).Select(g => g.MaGiangVien).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rawItems.Select(x => new UserSummaryResponse(
            x.Id,
            x.UserName,
            x.FullName,
            x.Email,
            x.Roles,
            x.IsActive,
            x.MustChangePassword,
            x.CreatedAtUtc,
            x.SinhVienCode is not null ? "SinhVien" : x.GiangVienCode is not null ? "GiangVien" : null,
            x.SinhVienCode ?? x.GiangVienCode)).ToArray();

        return new PagedResponse<UserSummaryResponse>(items, page, pageSize, totalCount);
    }

    public async Task<UserDetailResponse?> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return null;

        var student = await db.SinhViens.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        var lecturer = student is null
            ? await db.GiangViens.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct)
            : null;

        return new UserDetailResponse(
            user.Id,
            user.UserName,
            user.FullName,
            user.Email,
            user.UserRoles.Select(x => x.Role.Name).Order().ToArray(),
            user.IsActive,
            user.MustChangePassword,
            user.CreatedAtUtc,
            student is not null ? "SinhVien" : lecturer is not null ? "GiangVien" : null,
            student?.MaSinhVien ?? lecturer?.MaGiangVien,
            student?.SoDienThoai ?? lecturer?.SoDienThoai,
            student is not null ? ToProfile(student) : lecturer is not null ? ToProfile(lecturer) : null);
    }

    public async Task<UserDetailResponse?> GetMyProfileAsync(Guid userId, CancellationToken ct) =>
        await GetUserAsync(userId, ct);

    public async Task<(User? User, string? Error)> CreateStaffAsync(CreateStaffAccountRequest request, CancellationToken ct)
    {
        var roleName = request.Role.Trim();
        if (roleName is not (RoleNames.Admin or RoleNames.CanBoQuanLy))
            return (null, "Tài khoản sinh viên/giảng viên phải được cấp từ hồ sơ tương ứng.");

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(x => x.UserName == userName, ct))
            return (null, "Mã tài khoản đã tồn tại.");
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            return (null, "Email đã được sử dụng.");

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, ct);
        if (role is null) return (null, "Vai trò chưa được cấu hình.");

        var user = new User
        {
            UserName = userName,
            FullName = request.FullName.Trim(),
            Email = email,
            IsActive = true,
            MustChangePassword = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.InitialPassword);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
        await db.SaveChangesAsync(ct);
        return (user, null);
    }

    public async Task<string?> SetStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken ct)
    {
        var user = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return "Không tìm thấy tài khoản.";
        if (!isActive && id == actorId) return "Bạn không thể khóa chính tài khoản đang đăng nhập.";

        user.IsActive = isActive;
        var student = await db.SinhViens.SingleOrDefaultAsync(x => x.UserId == id, ct);
        if (student is not null) student.TrangThaiTaiKhoan = isActive;
        var lecturer = await db.GiangViens.SingleOrDefaultAsync(x => x.UserId == id, ct);
        if (lecturer is not null) lecturer.TrangThaiTaiKhoan = isActive;
        if (!isActive) await RevokeRefreshTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> ResetPasswordAsync(Guid id, string temporaryPassword, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return "Không tìm thấy tài khoản.";

        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        await RevokeRefreshTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> ChangeRoleAsync(Guid id, string roleName, Guid actorId, CancellationToken ct)
    {
        if (id == actorId) return "Không thể thay đổi vai trò của chính tài khoản đang đăng nhập.";
        if (roleName is not (RoleNames.Admin or RoleNames.CanBoQuanLy))
            return "Chỉ được đổi vai trò tài khoản nội bộ giữa Admin và CanBoQuanLy.";
        if (await db.SinhViens.AnyAsync(x => x.UserId == id, ct)
            || await db.GiangViens.AnyAsync(x => x.UserId == id, ct))
            return "Vai trò sinh viên/giảng viên được xác định theo hồ sơ và không thể đổi tại đây.";

        var user = await db.Users.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return "Không tìm thấy tài khoản.";
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, ct);
        if (role is null) return "Vai trò chưa được cấu hình.";

        db.UserRoles.RemoveRange(user.UserRoles);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await RevokeRefreshTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> UpdateMyProfileAsync(Guid userId, UpdateMyProfileRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return "Không tìm thấy tài khoản.";

        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(x => x.Id != userId && x.Email == email, ct))
            return "Email đã được tài khoản khác sử dụng.";
        if (await db.SinhViens.AnyAsync(x => x.UserId != userId && x.Email == email, ct))
            return "Email đã được hồ sơ sinh viên khác sử dụng.";
        if (await db.GiangViens.AnyAsync(x => x.UserId != userId && x.Email == email, ct))
            return "Email đã được hồ sơ giảng viên khác sử dụng.";

        user.FullName = request.FullName.Trim();
        user.Email = email;
        var student = await db.SinhViens.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (student is not null)
        {
            student.HoTen = user.FullName;
            student.Email = email;
            student.SoDienThoai = request.PhoneNumber?.Trim();
            ApplyPersonalProfile(student, request);
        }
        var lecturer = await db.GiangViens.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (lecturer is not null)
        {
            lecturer.HoTen = user.FullName;
            lecturer.Email = email;
            lecturer.SoDienThoai = request.PhoneNumber?.Trim();
            ApplyPersonalProfile(lecturer, request);
        }

        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<bool> CanManageTargetAsync(Guid id, bool isAdmin, CancellationToken ct)
    {
        if (isAdmin) return await db.Users.AnyAsync(x => x.Id == id, ct);
        return await db.Users.AnyAsync(x => x.Id == id && x.UserRoles.Any(ur =>
            ur.Role.Name == RoleNames.SinhVien || ur.Role.Name == RoleNames.GiangVien), ct);
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var token in tokens) token.RevokedAtUtc = DateTime.UtcNow;
    }

    private static AcademicProfileResponse ToProfile(SinhVien profile) => new(
        profile.AnhDaiDien,
        profile.GioiTinh,
        profile.NgaySinh,
        profile.NoiSinh,
        profile.DanToc,
        profile.TonGiao,
        profile.QuocTich,
        profile.SoCCCD,
        profile.NgayCapCCCD,
        profile.NoiCapCCCD,
        profile.DiaChiLienHe,
        profile.HoKhauThuongTru,
        profile.CoSo,
        profile.Khoa,
        profile.ChuyenNganh,
        profile.Nganh,
        profile.LopHoc,
        profile.KhoaHoc,
        profile.BacDaoTao,
        profile.LoaiHinhDaoTao,
        profile.NgayVaoTruong,
        null,
        null,
        null);

    private static AcademicProfileResponse ToProfile(GiangVien profile) => new(
        profile.AnhDaiDien,
        profile.GioiTinh,
        profile.NgaySinh,
        profile.NoiSinh,
        profile.DanToc,
        profile.TonGiao,
        profile.QuocTich,
        profile.SoCCCD,
        profile.NgayCapCCCD,
        profile.NoiCapCCCD,
        profile.DiaChiLienHe,
        profile.HoKhauThuongTru,
        profile.CoSo,
        profile.Khoa,
        profile.ChuyenNganh,
        null,
        null,
        null,
        null,
        null,
        null,
        profile.NgayBatDauCongTac,
        profile.TrinhDoHocVan,
        profile.ChucDanh);

    private static void ApplyPersonalProfile(SinhVien profile, UpdateMyProfileRequest request)
    {
        profile.AnhDaiDien = request.AnhDaiDien?.Trim();
        profile.GioiTinh = request.GioiTinh?.Trim();
        profile.NgaySinh = request.NgaySinh;
        profile.NoiSinh = request.NoiSinh?.Trim();
        profile.DanToc = request.DanToc?.Trim();
        profile.TonGiao = request.TonGiao?.Trim();
        profile.QuocTich = request.QuocTich?.Trim();
        profile.DiaChiLienHe = request.DiaChiLienHe?.Trim();
        profile.HoKhauThuongTru = request.HoKhauThuongTru?.Trim();
    }

    private static void ApplyPersonalProfile(GiangVien profile, UpdateMyProfileRequest request)
    {
        profile.AnhDaiDien = request.AnhDaiDien?.Trim();
        profile.GioiTinh = request.GioiTinh?.Trim();
        profile.NgaySinh = request.NgaySinh;
        profile.NoiSinh = request.NoiSinh?.Trim();
        profile.DanToc = request.DanToc?.Trim();
        profile.TonGiao = request.TonGiao?.Trim();
        profile.QuocTich = request.QuocTich?.Trim();
        profile.DiaChiLienHe = request.DiaChiLienHe?.Trim();
        profile.HoKhauThuongTru = request.HoKhauThuongTru?.Trim();
    }
}
