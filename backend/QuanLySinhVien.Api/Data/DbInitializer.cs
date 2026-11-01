using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Data;

public static class DbInitializer
{
    public static async Task SeedDevelopmentAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.Database.CanConnectAsync() || (await db.Database.GetPendingMigrationsAsync()).Any()) return;

        var roles = await db.Roles.ToDictionaryAsync(x => x.Name);
        var passwordHasher = new PasswordHasher<User>();

        // Hồ sơ nghiệp vụ phải tồn tại trước khi tài khoản đăng nhập được cấp.
        var giangVienProfile = await db.GiangViens.SingleOrDefaultAsync(x => x.MaGiangVien == "GV001");
        if (giangVienProfile is null)
        {
            giangVienProfile = new GiangVien
            {
                MaGiangVien = "GV001",
                HoTen = "Giảng viên Development",
                TrangThaiTaiKhoan = true
            };
            db.GiangViens.Add(giangVienProfile);
        }

        var sinhVienProfile = await db.SinhViens.SingleOrDefaultAsync(x => x.MaSinhVien == "2001230048");
        if (sinhVienProfile is null)
        {
            sinhVienProfile = new SinhVien
            {
                MaSinhVien = "2001230048",
                HoTen = "Sinh viên Development",
                Email = "sv2001230048@local.test",
                TrangThaiTaiKhoan = true
            };
            db.SinhViens.Add(sinhVienProfile);
        }

        // Lưu hồ sơ trước, đúng với quy trình nghiệp vụ của hệ thống.
        await db.SaveChangesAsync();

        var accounts = new[]
        {
            new DevelopmentAccount("admin", "admin@local.test", "Quản trị viên Development", "Admin@123", RoleNames.Admin),
            new DevelopmentAccount("CBQL001", "cbql001@local.test", "Cán bộ quản lý Development", "CanBo@123", RoleNames.CanBoQuanLy),
            new DevelopmentAccount("GV001", "gv001@local.test", "Giảng viên Development", "GiangVien@123", RoleNames.GiangVien),
            new DevelopmentAccount("2001230048", "sv2001230048@local.test", "Sinh viên Development", "SinhVien@123", RoleNames.SinhVien)
        };

        foreach (var account in accounts)
        {
            if (!roles.TryGetValue(account.Role, out var role))
                throw new InvalidOperationException($"Role '{account.Role}' has not been seeded.");

            var user = await db.Users.Include(x => x.UserRoles)
                .SingleOrDefaultAsync(x => x.UserName == account.UserName);
            if (user is null)
            {
                user = new User
                {
                    UserName = account.UserName,
                    Email = account.Email,
                    FullName = account.FullName
                };
                user.PasswordHash = passwordHasher.HashPassword(user, account.Password);
                db.Users.Add(user);
            }

            if (user.UserRoles.All(x => x.RoleId != role.Id))
                db.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });

            if (account.Role == RoleNames.GiangVien)
            {
                if (giangVienProfile.UserId.HasValue && giangVienProfile.UserId != user.Id)
                    throw new InvalidOperationException("Hồ sơ GV001 đã liên kết với tài khoản khác.");
                giangVienProfile.UserId = user.Id;
            }
            else if (account.Role == RoleNames.SinhVien)
            {
                if (sinhVienProfile.UserId.HasValue && sinhVienProfile.UserId != user.Id)
                    throw new InvalidOperationException("Hồ sơ 2001230048 đã liên kết với tài khoản khác.");
                sinhVienProfile.UserId = user.Id;
            }
        }

        await db.SaveChangesAsync();
    }

    private sealed record DevelopmentAccount(
        string UserName,
        string Email,
        string FullName,
        string Password,
        string Role);
}
