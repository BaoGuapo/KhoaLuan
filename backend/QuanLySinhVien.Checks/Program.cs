using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Api.Controllers;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Contracts.Training;
using QuanLySinhVien.Api.Services;

if (args.Contains("--class-periods")) { ClassPeriodChecks.Run(); return; }
if (args.Contains("--cloudinary")) { await CloudinaryPhotoChecks.Run(); return; }
if (args.Contains("--attendance")) { await AttendanceChecks.Run(); return; }
if (args.Contains("--attendance-web")) { await AttendanceWebChecks.Run(); return; }
if (args.Contains("--attendance-api")) { await AttendanceApiBindingChecks.Run(); return; }
if (args.Contains("--web-smoke")) { await TrainingWebChecks.Run(); return; }
if (args.Contains("--student-smoke") || args.Contains("--student-preview")) { await StudentUiChecks.Run(args.Contains("--student-preview")); return; }
if (args.Contains("--schema") || args.Contains("--db-read") || args.Contains("--db-test") || args.Contains("--demo-seed"))
{
    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "d43cef33-5d2c-41ff-89c3-7322e0fef7cc", "secrets.json");
    using var config = JsonDocument.Parse(File.ReadAllText(path));
    var connection = config.RootElement.GetProperty("ConnectionStrings:DefaultConnection").GetString();
    try
    {
        if (args.Contains("--demo-seed"))
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection, x => x.EnableRetryOnFailure()).Options);
            var environment = new DemoSeedEnvironment();
            await DevelopmentTrainingSeed.SeedAsync(db, environment);
            var codes = TrainingRules.ClassCodes("DEMO-LTCB", 8);
            var firstClasses = await db.LopHocPhanDuKiens.AsNoTracking().Where(x => codes.Contains(x.MaLopHocPhan)).OrderBy(x => x.MaLopHocPhan)
                .Select(x => new { x.MaLopHocPhan, x.DaPhanCong }).ToListAsync();
            var firstUsers = await db.Users.AsNoTracking().Where(x => x.UserName == DevelopmentTrainingSeed.Student || x.UserName == DevelopmentTrainingSeed.Lecturer)
                .OrderBy(x => x.UserName).Select(x => new { x.UserName, x.PasswordHash, x.MustChangePassword }).ToListAsync();
            await DevelopmentTrainingSeed.SeedAsync(db, environment);
            var secondClasses = await db.LopHocPhanDuKiens.AsNoTracking().Where(x => codes.Contains(x.MaLopHocPhan)).OrderBy(x => x.MaLopHocPhan)
                .Select(x => new { x.MaLopHocPhan, x.DaPhanCong }).ToListAsync();
            var secondUsers = await db.Users.AsNoTracking().Where(x => x.UserName == DevelopmentTrainingSeed.Student || x.UserName == DevelopmentTrainingSeed.Lecturer)
                .OrderBy(x => x.UserName).Select(x => new { x.UserName, x.PasswordHash, x.MustChangePassword }).ToListAsync();
            if (firstClasses.Count != 8 || !firstClasses.SequenceEqual(secondClasses) || firstUsers.Count != 2 || !firstUsers.SequenceEqual(secondUsers)) throw new Exception("Demo seed is not idempotent.");
            Console.WriteLine("PASS: seed created eight classes and two linked demo accounts; repeat seed preserved states/password hashes.");
            environment.EnvironmentName = "Production";
            await DevelopmentTrainingSeed.SeedAsync(db, environment);
            Console.WriteLine("PASS: Production seed disabled.");
            return;
        }
        if (args.Contains("--db-test")) { await TrainingDatabaseChecks.Run(connection!); return; }
        if (args.Contains("--db-read"))
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
            var catalog = await new TrainingWorkflowController(db).Catalog(default);
            if (catalog is not OkObjectResult) throw new Exception("Catalog failed.");
            Console.WriteLine("PASS: catalog reads actual Azure SQL schema.");
            Console.WriteLine($"Applied migrations: {(await db.Database.GetAppliedMigrationsAsync()).Count()}; pending: {(await db.Database.GetPendingMigrationsAsync()).Count()}");
            // Materialize legacy records to detect CLR/provider mismatches without displaying personal information.
            await db.LopHocPhans.AsNoTracking().Take(1).ToListAsync();
            await db.LichHocs.AsNoTracking().Take(1).ToListAsync();
            Console.WriteLine("PASS: class and schedule materialization.");
            if (await db.SinhViens.AnyAsync(x => x.MaSinhVien == DevelopmentTrainingSeed.Student))
            {
                var controller = new DaoTaoController(db, new TrainingEnrollmentService(db));
                controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        [new(System.Security.Claims.ClaimTypes.Name, DevelopmentTrainingSeed.Student), new(System.Security.Claims.ClaimTypes.Role, "SinhVien")], "ReadCheck"))
                } };
                if (await controller.MyEnrollments(default) is not OkObjectResult || await controller.GetDanhSachMonHocDangKy("moi", default) is not OkObjectResult)
                    throw new Exception("Student read APIs failed.");
                Console.WriteLine("PASS: student offers and own enrollments queries (read-only).");
            }
            return;
        }
        await using var sql = new SqlConnection(connection);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = "SELECT TABLE_NAME,COLUMN_NAME,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('NganhHoc','KhoaTuyenSinh','ChuongTrinhDaoTao','LopHocPhan','MonHoc','HocKyThucTe','LichHoc','BuoiHoc','DanhSachSinhVienLop') ORDER BY TABLE_NAME,ORDINAL_POSITION";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)));
    }
    catch (SqlException ex) { Console.WriteLine($"Database schema check failed (SQL error {ex.Number})."); Environment.ExitCode = 1; }
    return;
}
RegistrationRuleChecks.Run();
var checks = 0;
void Check(bool condition, string description)
{ if (!condition) throw new Exception(description); checks++; Console.WriteLine($"PASS: {description}"); }
Check(TrainingRules.ClassCodes("LTCB-K26", 8).SequenceEqual(Enumerable.Range(1, 8).Select(i => $"LTCB-K26-{i:00}")), "8 distinct classes for one course");
Check(TrainingRules.ClassCodes("MON", 1).Length == 1 && TrainingRules.ClassCodes("MON", 50).Length == 50, "class-count boundaries");
try { TrainingRules.ClassCodes("MON", 51); throw new Exception("Expected rejection."); } catch (ArgumentException) { checks++; Console.WriteLine("PASS: reject more than 50 classes"); }
Check(TrainingRules.ValidateCourses([new("MH01", 1, true), new("mh01", 2, false)]) is not null, "reject duplicate curriculum course");
Check(TrainingRules.ValidateCourses([new("MH01", 0, true)]) is not null, "reject semester outside range");
Check(TrainingRules.ValidateCourses([new("MH01", 1, true), new("MH02", 2, false)]) is null, "mandatory and elective curriculum courses");
Check(TrainingRules.SessionDates(new(2026, 10, 5), new(2026, 10, 18), 2).Count() == 2, "weekly sessions across semester dates");
Check(TrainingRules.SessionDates(new(2026, 10, 5), new(2026, 10, 18), 8).Count() == 2, "Sunday uses weekday 8");
using var template = new MemoryStream(CurriculumExcel.Template());
var preview = CurriculumExcel.Read(template);
Check(preview.Errors.Count == 0 && preview.MonHocs.Count == 1 && preview.MonHocs[0].BatBuoc && preview.MonHocs[0].ThuTuHocKy == 1, "XLSX template round-trip");
Console.WriteLine($"{checks} checks passed.");

sealed class DemoSeedEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "DemoSeedChecks";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}
