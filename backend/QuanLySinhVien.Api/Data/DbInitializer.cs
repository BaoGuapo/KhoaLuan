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

        // Mồi dữ liệu 10 Giảng viên (5 CNTT, 5 QTKD)
        var danhSachGiangVienMoi = new[]
        {
            // --- 5 Giảng viên ngành CNTT ---
            new GiangVien { 
                MaGiangVien = "GV_CNTT01", HoTen = "TS. Lê Tiến Đạt", Khoa = "CNTT", ChuyenNganh = "Kỹ thuật phần mềm", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Giảng viên chính", Email = "datlt@huit.edu.vn", SoDienThoai = "0901234501", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2015, 8, 15), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1980, 5, 20), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_CNTT02", HoTen = "ThS. Nguyễn Thị Minh Ngọc", Khoa = "CNTT", ChuyenNganh = "Hệ thống thông tin", 
                TrinhDoHocVan = "Thạc sĩ", ChucDanh = "Giảng viên", Email = "ngocntm@huit.edu.vn", SoDienThoai = "0901234502", 
                GioiTinh = "Nữ", NgayBatDauCongTac = new DateTime(2018, 9, 1), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1990, 10, 12), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_CNTT03", HoTen = "PGS. TS. Phạm Hoàng Anh", Khoa = "CNTT", ChuyenNganh = "Khoa học máy tính", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Phó Giáo sư", Email = "anhph@huit.edu.vn", SoDienThoai = "0901234503", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2010, 2, 10), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1975, 3, 5), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_CNTT04", HoTen = "ThS. Trần Trọng Cung", Khoa = "CNTT", ChuyenNganh = "Mạng máy tính", 
                TrinhDoHocVan = "Thạc sĩ", ChucDanh = "Giảng viên", Email = "cungtt@huit.edu.vn", SoDienThoai = "0901234504", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2020, 1, 15), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1992, 7, 22), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_CNTT05", HoTen = "TS. Vũ Kim Yến", Khoa = "CNTT", ChuyenNganh = "Trí tuệ nhân tạo", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Giảng viên", Email = "yenvk@huit.edu.vn", SoDienThoai = "0901234505", 
                GioiTinh = "Nữ", NgayBatDauCongTac = new DateTime(2017, 11, 1), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1985, 12, 30), QuocTich = "Việt Nam", DanToc = "Kinh"
            },

            // --- 5 Giảng viên ngành QTKD ---
            new GiangVien { 
                MaGiangVien = "GV_QTKD01", HoTen = "TS. Hoàng Quốc Bảo", Khoa = "QTKD", ChuyenNganh = "Quản trị doanh nghiệp", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Trưởng khoa", Email = "baohq@huit.edu.vn", SoDienThoai = "0901234506", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2012, 5, 1), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1978, 8, 14), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_QTKD02", HoTen = "ThS. Đặng Thu Thảo", Khoa = "QTKD", ChuyenNganh = "Marketing", 
                TrinhDoHocVan = "Thạc sĩ", ChucDanh = "Giảng viên", Email = "thaodt@huit.edu.vn", SoDienThoai = "0901234507", 
                GioiTinh = "Nữ", NgayBatDauCongTac = new DateTime(2019, 9, 5), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1991, 4, 18), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_QTKD03", HoTen = "ThS. Bùi Gia Huy", Khoa = "QTKD", ChuyenNganh = "Kinh doanh quốc tế", 
                TrinhDoHocVan = "Thạc sĩ", ChucDanh = "Giảng viên", Email = "huybg@huit.edu.vn", SoDienThoai = "0901234508", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2021, 2, 20), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1994, 11, 2), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_QTKD04", HoTen = "TS. Lý Lan Phương", Khoa = "QTKD", ChuyenNganh = "Quản trị nhân sự", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Giảng viên chính", Email = "phuongll@huit.edu.vn", SoDienThoai = "0901234509", 
                GioiTinh = "Nữ", NgayBatDauCongTac = new DateTime(2014, 10, 10), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1982, 1, 25), QuocTich = "Việt Nam", DanToc = "Kinh"
            },
            new GiangVien { 
                MaGiangVien = "GV_QTKD05", HoTen = "PGS. TS. Hồ Tài Phát", Khoa = "QTKD", ChuyenNganh = "Tài chính", 
                TrinhDoHocVan = "Tiến sĩ", ChucDanh = "Phó Giáo sư", Email = "phatht@huit.edu.vn", SoDienThoai = "0901234510", 
                GioiTinh = "Nam", NgayBatDauCongTac = new DateTime(2008, 3, 15), TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1970, 6, 9), QuocTich = "Việt Nam", DanToc = "Kinh"
            }
        };
        foreach (var gv in danhSachGiangVienMoi)
        {
            if (!await db.GiangViens.AnyAsync(x => x.MaGiangVien == gv.MaGiangVien))
            {
                db.GiangViens.Add(gv);
            }
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
                    FullName = account.FullName,
                    MustChangePassword = true
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
        // Database đã nạp bộ dữ liệu demo đầy đủ (Checks --reset-demo-data) tắt bộ mẫu DEMO-* để không trộn dữ liệu.
        if (scope.ServiceProvider.GetRequiredService<IConfiguration>().GetValue("DevelopmentSeed:TrainingDemo", true))
            await DevelopmentTrainingSeed.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
    }
    public static async Task SeedMarketingGiangVienAsync(AppDbContext context)
        {
            if (await context.Set<GiangVien>().AnyAsync(g => g.MaGiangVien == "GV_MKT01"))
    {
        return; // Nếu đã có thì không thêm nữa
    }

    var dsGiangVienMKT = new List<GiangVien>
    {
        new GiangVien { MaGiangVien = "GV_MKT01", HoTen = "Nguyễn Thị Mai Phương", TrinhDoHocVan = "Tiến sĩ", ChuyenNganh = "Marketing", Email = "phuongntm@huit.edu.vn", SoDienThoai = "0901234567", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT02", HoTen = "Trần Hoàng Nam", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "namth@huit.edu.vn", SoDienThoai = "0902345678", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT03", HoTen = "Lê Cẩm Tú", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "tulc@huit.edu.vn", SoDienThoai = "0903456789", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT04", HoTen = "Phạm Vũ Bảo", TrinhDoHocVan = "PGS.TS", ChuyenNganh = "Marketing", Email = "baopv@huit.edu.vn", SoDienThoai = "0904567890", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT05", HoTen = "Đỗ Thị Thu Trang", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "trangdtt@huit.edu.vn", SoDienThoai = "0905678901", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT06", HoTen = "Võ Minh Khoa", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "khoavm@huit.edu.vn", SoDienThoai = "0906789012", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT07", HoTen = "Bùi Hoàng Anh", TrinhDoHocVan = "Tiến sĩ", ChuyenNganh = "Marketing", Email = "anhbh@huit.edu.vn", SoDienThoai = "0907890123", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT08", HoTen = "Ngô Phương Linh", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "linhnp@huit.edu.vn", SoDienThoai = "0908901234", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT09", HoTen = "Đặng Quốc Huy", TrinhDoHocVan = "Tiến sĩ", ChuyenNganh = "Marketing", Email = "huydq@huit.edu.vn", SoDienThoai = "0909012345", TrangThaiTaiKhoan = true },
        new GiangVien { MaGiangVien = "GV_MKT10", HoTen = "Nguyễn Phan Khánh Vân", TrinhDoHocVan = "Thạc sĩ", ChuyenNganh = "Marketing", Email = "vannpk@huit.edu.vn", SoDienThoai = "0901122334", TrangThaiTaiKhoan = true }
    };

    await context.Set<GiangVien>().AddRangeAsync(dsGiangVienMKT);
    await context.SaveChangesAsync();
        }

    private sealed record DevelopmentAccount(
        string UserName,
        string Email,
        string FullName,
        string Password,
        string Role);
}