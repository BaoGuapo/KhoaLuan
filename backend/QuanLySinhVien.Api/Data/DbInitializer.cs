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
