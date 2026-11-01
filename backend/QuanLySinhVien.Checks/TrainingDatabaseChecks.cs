using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Training;
using QuanLySinhVien.Api.Controllers;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;
using System.Text.Json;

public static class TrainingDatabaseChecks
{
    public static async Task Run(string connection)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection, x => x.EnableRetryOnFailure()).Options);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var major = "T-N-" + suffix; var intake = "T-K-" + suffix; var semester = "T-H-" + suffix;
        var subject = "T-M-" + suffix; var lecturer = "T-G-" + suffix; var room = "T-P-" + suffix;
        var student = "T-S-" + suffix; var otherStudent = "T-X-" + suffix;
        var prefix = "T-L-" + suffix; var codes = TrainingRules.ClassCodes(prefix, 8);
        Guid planId = default; Guid applicationId = default; var owned = false; var checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; Console.WriteLine("PASS: " + name); }
        static int Status(IActionResult result) => result switch { ObjectResult r => r.StatusCode ?? 200, StatusCodeResult r => r.StatusCode, _ => 0 };
        static Guid Id(IActionResult result) => JsonSerializer.SerializeToElement(((OkObjectResult)result).Value).GetProperty("Id").GetGuid();
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NganhHoc(maNganh,tenNganh) VALUES ({major},N'Ngành kiểm thử tạm')");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO KhoaTuyenSinh(maKhoa,namBatDau,namKetThuc) VALUES ({intake},2090,2094)");
                db.MonHocs.Add(new MonHoc { MaMonHoc = subject, TenMonHoc = "Môn kiểm thử tạm", SoTinChiLyThuyet = 3 });
                db.HocKys.Add(new HocKyThucTe { MaHocKy = semester, TenHocKy = "Học kỳ kiểm thử tạm", NgayBatDau = new(2090, 9, 4), NgayKetThuc = new(2090, 10, 30) });
                db.PhongHocs.Add(new PhongHoc { MaPhong = room, TenPhong = "Phòng kiểm thử tạm" });
                db.GiangViens.Add(new GiangVien { MaGiangVien = lecturer, HoTen = "Giảng viên kiểm thử tạm", ChuyenNganh = major });
                db.SinhViens.AddRange(
                    new SinhVien { MaSinhVien = student, HoTen = "Sinh viên kiểm thử tạm", Email = $"{student}@local.test", Nganh = major, KhoaHoc = intake },
                    new SinhVien { MaSinhVien = otherStudent, HoTen = "Sinh viên kiểm thử khác khóa", Email = $"{otherStudent}@local.test", Nganh = major, KhoaHoc = "2089" });
                await db.SaveChangesAsync(); await transaction.CommitAsync();
            });
            owned = true;
            db.HoSoHocVus.AddRange(new HoSoHocVu { MaSinhVien = student, ThuTuHocKy = 1, GpaTichLuy = 2.5m }, new HoSoHocVu { MaSinhVien = otherStudent, ThuTuHocKy = 1, GpaTichLuy = 2.5m });
            db.DotDangKyHocPhans.Add(new DotDangKyHocPhan { MaHocKy = semester, TenDot = "Đợt kiểm thử tạm", MoLucUtc = DateTime.UtcNow.AddHours(-1), DongLucUtc = DateTime.UtcNow.AddHours(1) });
            await db.SaveChangesAsync();
            var controller = new TrainingWorkflowController(db);
            var created = await controller.CreateCurriculum(new(major, intake, "Khoa kiểm thử tạm", "Chương trình kiểm thử tạm", [new(subject, 1, true)]), default);
            Check(Status(created) == 200, "save curriculum and sync legacy table"); planId = Id(created);
            Check(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ChuongTrinhDaoTao WHERE maNganh={major} AND maKhoa={intake}").SingleAsync() == 1, "legacy curriculum row exists");
            Check(Status(await controller.CreateCurriculum(new(major, intake, "Khoa", "Bản trùng", [new(subject, 1, true)]), default)) == 409, "reject duplicate major/intake curriculum");
            Check(Status(await controller.Apply(new(planId, semester, 2), default)) == 400, "reject applying semester without courses");
            var applied = await controller.Apply(new(planId, semester, 1), default); Check(Status(applied) == 200, "apply curriculum semester"); applicationId = Id(applied);
            Check(Status(await controller.Apply(new(planId, semester, 1), default)) == 409, "reject duplicate semester application");
            Check(Status(await controller.OpenClasses(applicationId, new(subject, prefix, 8, 1), default)) == 200, "open eight pending classes for same course");
            Check(await db.LopHocPhanDuKiens.CountAsync(x => codes.Contains(x.MaLopHocPhan)) == 8, "all eight classes persisted atomically");
            Check(Status(await controller.OpenClasses(applicationId, new(subject, prefix, 8, 1), default)) == 409, "reject duplicate class batch");
            Check(Status(await controller.Publish(codes[0], default)) == 404, "cannot publish unassigned class");
            Check(Status(await controller.Assign(codes[0], new(lecturer, room, 2, 1, 3), default)) == 200, "assign lecturer and create weekly sessions");
            Check(await db.BuoiHocs.CountAsync(x => x.LichHoc.MaLopHocPhan == codes[0]) > 0, "weekly sessions saved with legacy tinyint status");
            Check(Status(await controller.Assign(codes[1], new(lecturer, room, 2, 1, 3), default)) == 409, "reject room/lecturer schedule collision");
            Check(Status(await controller.Assign(codes[1], new(lecturer, room, 3, 1, 3), default)) == 200, "assign second class at non-conflicting time");
            Check(Status(await controller.Assign(codes[0], new(lecturer, room, 4, 1, 3), default)) == 409, "reject assigning class twice");
            var enrollment = new TrainingEnrollmentService(db);
            Check((await enrollment.EnrollAsync(student, codes[0], default)).StatusCode == 409, "reject enrollment before registration opens");
            Check(Status(await controller.Publish(codes[0], default)) == 200 && Status(await controller.Publish(codes[1], default)) == 200, "publish scheduled classes");
            Check((await enrollment.EnrollAsync(otherStudent, codes[0], default)).StatusCode == 409, "reject wrong student intake");
            Check((await enrollment.EnrollAsync(student, codes[0], default)).StatusCode == 200, "enroll matching major/intake student");
            Check((await enrollment.EnrollAsync(student, codes[1], default)).StatusCode == 409, "reject same course in another class in same semester");
            Check(await db.DanhSachSinhVienLops.CountAsync(x => x.MaSinhVien == student) == 1, "duplicate enrollment did not write extra record");
            Check(Status(await controller.Applications(default)) == 200, "workspace projection reads scheduled legacy data");
            await db.BuoiHocs.AsNoTracking().Where(x => x.LichHoc.MaLopHocPhan == codes[0]).ToListAsync();
            Console.WriteLine($"{checks} database workflow checks passed.");
        }
        finally
        {
            if (owned)
            {
                await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync();
                    await db.DanhSachSinhVienLops.Where(x => x.MaSinhVien == student || x.MaSinhVien == otherStudent).ExecuteDeleteAsync();
                    await db.HoSoHocVus.Where(x => x.MaSinhVien == student || x.MaSinhVien == otherStudent).ExecuteDeleteAsync();
                    await db.DotDangKyHocPhans.Where(x => x.MaHocKy == semester).ExecuteDeleteAsync();
                    await db.BuoiHocs.Where(x => codes.Contains(x.LichHoc.MaLopHocPhan)).ExecuteDeleteAsync();
                    await db.LichHocs.Where(x => codes.Contains(x.MaLopHocPhan)).ExecuteDeleteAsync();
                    await db.LopHocPhans.Where(x => codes.Contains(x.MaLopHocPhan)).ExecuteDeleteAsync();
                    await db.LopHocPhanDuKiens.Where(x => codes.Contains(x.MaLopHocPhan)).ExecuteDeleteAsync();
                    await db.ApDungHocKys.Where(x => x.KeHoach.MaNganh == major && x.KeHoach.MaKhoa == intake).ExecuteDeleteAsync();
                    await db.MonKeHoachDaoTaos.Where(x => x.KeHoach.MaNganh == major && x.KeHoach.MaKhoa == intake).ExecuteDeleteAsync();
                    await db.KeHoachDaoTaos.Where(x => x.MaNganh == major && x.MaKhoa == intake).ExecuteDeleteAsync();
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ChuongTrinhDaoTao WHERE maNganh={major} AND maKhoa={intake}");
                    await db.SinhViens.Where(x => x.MaSinhVien == student || x.MaSinhVien == otherStudent).ExecuteDeleteAsync();
                    await db.GiangViens.Where(x => x.MaGiangVien == lecturer).ExecuteDeleteAsync();
                    await db.PhongHocs.Where(x => x.MaPhong == room).ExecuteDeleteAsync();
                    await db.HocKys.Where(x => x.MaHocKy == semester).ExecuteDeleteAsync();
                    await db.MonHocs.Where(x => x.MaMonHoc == subject).ExecuteDeleteAsync();
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM KhoaTuyenSinh WHERE maKhoa={intake}");
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM NganhHoc WHERE maNganh={major}");
                    await transaction.CommitAsync();
                });
                Console.WriteLine("Temporary test records removed; existing project data preserved.");
            }
        }
    }
}
