using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Data;

public static class DevelopmentTrainingSeed
{
    public const string Major = "DEMO-CNTT";
    public const string Intake = "DEMO-K2026";
    public const string Semester = "DEMO-HK1-2026";
    public const string Course = "DEMO-LTCB";
    public const string Lecturer = "DEMO-GV01";
    public const string Student = "DEMO-SV01";
    // Chỉ là mật khẩu mẫu Development; không phải credential Azure/JWT.
    public const string InitialPassword = "Demo@12345";

    public static async Task SeedAsync(AppDbContext db, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) return;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource=N'QLSV.DevelopmentTrainingSeed', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=30000;
                IF @result < 0 THROW 51000, 'Cannot acquire development seed lock.', 1;
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                IF NOT EXISTS(SELECT 1 FROM NganhHoc WHERE maNganh={Major})
                  INSERT INTO NganhHoc(maNganh,tenNganh) VALUES({Major},N'Công nghệ thông tin (Demo)');
                IF NOT EXISTS(SELECT 1 FROM KhoaTuyenSinh WHERE maKhoa={Intake})
                  INSERT INTO KhoaTuyenSinh(maKhoa,namBatDau,namKetThuc) VALUES({Intake},2026,2030);
                """);
            if (!await db.MonHocs.AnyAsync(x => x.MaMonHoc == Course)) db.MonHocs.Add(new MonHoc
                { MaMonHoc = Course, TenMonHoc = "Lập trình cơ bản (Demo)", SoTinChiLyThuyet = 3 });
            if (!await db.MonHocs.AnyAsync(x => x.MaMonHoc == "DEMO-TINHOC")) db.MonHocs.Add(new MonHoc
                { MaMonHoc = "DEMO-TINHOC", TenMonHoc = "Tin học ứng dụng (Demo)", SoTinChiLyThuyet = 2, SoTinChiThucHanh = 1 });
            if (!await db.HocKys.AnyAsync(x => x.MaHocKy == Semester)) db.HocKys.Add(new HocKyThucTe
                { MaHocKy = Semester, TenHocKy = "HK1 2026–2027 (Demo)", NgayBatDau = new(2026, 9, 7), NgayKetThuc = new(2026, 12, 27) });
            if (!await db.PhongHocs.AnyAsync(x => x.MaPhong == "DEMO-P101")) db.PhongHocs.Add(new PhongHoc
                { MaPhong = "DEMO-P101", TenPhong = "Phòng thực hành 101 (Demo)", DiaChiCoSo = "Cơ sở Demo" });
            var lecturer = await db.GiangViens.SingleOrDefaultAsync(x => x.MaGiangVien == Lecturer);
            if (lecturer is null)
            {
                lecturer = new GiangVien { MaGiangVien = Lecturer, HoTen = "Nguyễn Minh An (Demo)", Email = "demo-gv01@local.test",
                    Khoa = "Khoa Công nghệ thông tin (Demo)", ChuyenNganh = Major, TrinhDoHocVan = "Thạc sĩ" };
                db.GiangViens.Add(lecturer);
            }
            var student = await db.SinhViens.SingleOrDefaultAsync(x => x.MaSinhVien == Student);
            if (student is null)
            {
                student = new SinhVien { MaSinhVien = Student, HoTen = "Trần Hoàng Nam (Demo)", Email = "demo-sv01@local.test",
                    Nganh = Major, KhoaHoc = Intake, Khoa = "Khoa Công nghệ thông tin (Demo)", LopHoc = "DEMO-26CNTT01",
                    BacDaoTao = "Đại học", LoaiHinhDaoTao = "Chính quy" };
                db.SinhViens.Add(student);
            }
            // Tạo hồ sơ trước tài khoản, không sửa hồ sơ/mật khẩu đã có.
            await db.SaveChangesAsync();
            var lecturerUser = await EnsureUser(db, Lecturer, "demo-gv01@local.test", lecturer.HoTen, RoleNames.GiangVien);
            var studentUser = await EnsureUser(db, Student, "demo-sv01@local.test", student.HoTen, RoleNames.SinhVien);
            lecturer.UserId ??= lecturerUser.Id; student.UserId ??= studentUser.Id;
            var plan = await db.KeHoachDaoTaos.Include(x => x.MonHocs).SingleOrDefaultAsync(x => x.MaNganh == Major && x.MaKhoa == Intake);
            if (plan is null)
            {
                plan = new KeHoachDaoTao { Id = Guid.Parse("b42c48fd-573d-4627-89f3-17a05d0e2601"), MaNganh = Major, MaKhoa = Intake,
                    TenKhoaDaoTao = "Khoa Công nghệ thông tin (Demo)", TenChuongTrinh = "CNTT khóa 2026 – chương trình Demo",
                    MonHocs = [new() { MaMonHoc = Course, ThuTuHocKy = 1, BatBuoc = true }, new() { MaMonHoc = "DEMO-TINHOC", ThuTuHocKy = 1, BatBuoc = false }] };
                db.KeHoachDaoTaos.Add(plan);
            }
            await db.SaveChangesAsync();
            foreach (var course in plan.MonHocs)
            {
                var rowCode = "DEMO-CTDT-" + course.MaMonHoc;
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    IF NOT EXISTS(SELECT 1 FROM ChuongTrinhDaoTao WHERE maNganh={Major} AND maKhoa={Intake} AND maMonHoc={course.MaMonHoc})
                      INSERT INTO ChuongTrinhDaoTao(maChuongTrinh,maNganh,maKhoa,maMonHoc,thuTuHocKy)
                      VALUES({rowCode},{Major},{Intake},{course.MaMonHoc},{course.ThuTuHocKy});
                    """);
            }
            var application = await db.ApDungHocKys.SingleOrDefaultAsync(x => x.KeHoachDaoTaoId == plan.Id && x.MaHocKy == Semester);
            if (application is null)
            {
                application = new ApDungHocKy { Id = Guid.Parse("b42c48fd-573d-4627-89f3-17a05d0e2602"), KeHoachDaoTaoId = plan.Id, MaHocKy = Semester, ThuTuHocKy = 1 };
                db.ApDungHocKys.Add(application);
            }
            await db.SaveChangesAsync();
            for (var index = 1; index <= 8; index++)
            {
                var code = $"DEMO-LTCB-{index:00}";
                var planned = await db.LopHocPhanDuKiens.SingleOrDefaultAsync(x => x.MaLopHocPhan == code);
                // Không reset lớp đã được học vụ thao tác ở lần demo trước.
                if (planned is not null) continue;
                planned = new LopHocPhanDuKien { MaLopHocPhan = code, ApDungHocKyId = application.Id, MaMonHoc = Course, SiSoToiDa = 40 };
                db.LopHocPhanDuKiens.Add(planned);
                if (index <= 2 && !await db.LopHocPhans.AnyAsync(x => x.MaLopHocPhan == code))
                {
                    var weekday = index == 1 ? 2 : 3;
                    var schedule = "LH-" + code;
                    db.LopHocPhans.Add(new LopHocPhan { MaLopHocPhan = code, MaMonHoc = Course, MaHocKy = Semester,
                        MaGiangVien = Lecturer, SiSoToiDa = 40, TrangThai = index == 1 ? "0" : "1" });
                    db.LichHocs.Add(new LichHoc { MaLichHoc = schedule, MaLopHocPhan = code, MaPhong = "DEMO-P101", ThuTrongTuan = weekday, TietBatDau = 1, TietKetThuc = 3 });
                    var dates = await db.HocKys.Where(x => x.MaHocKy == Semester).Select(x => new { x.NgayBatDau, x.NgayKetThuc }).SingleAsync();
                    if (dates.NgayBatDau.HasValue && dates.NgayKetThuc.HasValue)
                        db.BuoiHocs.AddRange(TrainingRules.SessionDates(dates.NgayBatDau.Value, dates.NgayKetThuc.Value, weekday).Select(date => new BuoiHoc
                            { MaBuoiHoc = $"BH-{code}-{date:yyyyMMdd}", MaLichHoc = schedule, NgayHocCuThe = date, TrangThaiBuoiHoc = "ChuaDienRa" }));
                    planned.DaPhanCong = true;
                }
            }
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        });
    }

    private static async Task<User> EnsureUser(AppDbContext db, string code, string email, string name, string roleName)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == code);
        if (user is not null) return user;
        user = new User { UserName = code, Email = email, FullName = name, MustChangePassword = true };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, InitialPassword);
        db.Users.Add(user);
        var role = await db.Roles.SingleAsync(x => x.Name == roleName);
        db.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
        return user;
    }
}
