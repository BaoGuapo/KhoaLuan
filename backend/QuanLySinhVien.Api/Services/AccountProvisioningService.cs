using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Users;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Services;

public interface IAccountProvisioningService
{
    Task<AccountProvisioningResult> ProvisionSinhVienAsync(string maSinhVien, string initialPassword, CancellationToken ct);
    Task<AccountProvisioningResult> ProvisionGiangVienAsync(string maGiangVien, string email, string initialPassword, CancellationToken ct);
}

public enum AccountProvisioningStatus
{
    Created,
    ProfileNotFound,
    ProfileInactive,
    ProfileDataInvalid,
    AlreadyHasAccount,
    UserNameAlreadyExists,
    EmailAlreadyExists,
    RoleNotConfigured,
    DataConflict
}

public sealed record AccountProvisioningResult(
    AccountProvisioningStatus Status,
    ProvisionedAccountResponse? Account = null);

public sealed class AccountProvisioningService(AppDbContext db) : IAccountProvisioningService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AccountProvisioningResult> ProvisionSinhVienAsync(
        string maSinhVien,
        string initialPassword,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var code = maSinhVien.Trim();
            var profile = await db.SinhViens.SingleOrDefaultAsync(x => x.MaSinhVien == code, ct);
            if (profile is null) return new(AccountProvisioningStatus.ProfileNotFound);
            if (!profile.TrangThaiTaiKhoan) return new(AccountProvisioningStatus.ProfileInactive);
            if (profile.UserId.HasValue) return new(AccountProvisioningStatus.AlreadyHasAccount);
            if (string.IsNullOrWhiteSpace(profile.Email)) return new(AccountProvisioningStatus.ProfileDataInvalid);

            return await CreateAndLinkAsync(
                code,
                profile.HoTen,
                profile.Email,
                initialPassword,
                RoleNames.SinhVien,
                userId => profile.UserId = userId,
                transaction,
                ct);
        }
        catch (DbUpdateException)
        {
            return new(AccountProvisioningStatus.DataConflict);
        }
    }

    public async Task<AccountProvisioningResult> ProvisionGiangVienAsync(
        string maGiangVien,
        string email,
        string initialPassword,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var code = maGiangVien.Trim();
            var profile = await db.GiangViens.SingleOrDefaultAsync(x => x.MaGiangVien == code, ct);
            if (profile is null) return new(AccountProvisioningStatus.ProfileNotFound);
            if (!profile.TrangThaiTaiKhoan) return new(AccountProvisioningStatus.ProfileInactive);
            if (profile.UserId.HasValue) return new(AccountProvisioningStatus.AlreadyHasAccount);
            if (string.IsNullOrWhiteSpace(email)) return new(AccountProvisioningStatus.ProfileDataInvalid);

            return await CreateAndLinkAsync(
                code,
                profile.HoTen,
                email,
                initialPassword,
                RoleNames.GiangVien,
                userId => profile.UserId = userId,
                transaction,
                ct);
        }
        catch (DbUpdateException)
        {
            return new(AccountProvisioningStatus.DataConflict);
        }
    }

    private async Task<AccountProvisioningResult> CreateAndLinkAsync(
        string userName,
        string fullName,
        string email,
        string initialPassword,
        string roleName,
        Action<Guid> linkProfile,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct)
    {
        var normalizedEmail = email.Trim();
        if (await db.Users.AnyAsync(x => x.UserName == userName, ct))
            return new(AccountProvisioningStatus.UserNameAlreadyExists);
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, ct))
            return new(AccountProvisioningStatus.EmailAlreadyExists);

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, ct);
        if (role is null) return new(AccountProvisioningStatus.RoleNotConfigured);

        var user = new User
        {
            UserName = userName,
            Email = normalizedEmail,
            FullName = fullName.Trim(),
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, initialPassword);

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
        linkProfile(user.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new(
            AccountProvisioningStatus.Created,
            new ProvisionedAccountResponse(user.Id, user.UserName, user.FullName, user.Email, roleName));
    }
}
