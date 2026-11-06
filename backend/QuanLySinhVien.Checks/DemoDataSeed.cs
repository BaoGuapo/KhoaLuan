using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

/// <summary>
/// Xoá toàn bộ dữ liệu nghiệp vụ của một database phát triển rồi nạp bộ dữ liệu demo đầy đủ:
/// 2 ngành, khóa K2024 đang học kỳ 5, hai học kỳ đã có điểm, học kỳ hiện tại có điểm danh tới hôm qua
/// và học kỳ sau có lớp chờ phân công. Mọi tài khoản dùng chung mật khẩu <see cref="Password"/>.
/// Chỉ chạy khi tên database được xác nhận lại và không phải Azure SQL.
/// </summary>
internal static class DemoDataSeed
{
    public const string Password = "Demo@12345";

    // Hồ sơ khuôn mặt và ảnh thẻ đã đăng ký thật được giữ lại, chuyển sang sinh viên mới.
    private static readonly (string Old, string New, string HoTen, string GioiTinh)[] FaceTransfers =
    [
        ("HKH-SV01", "2001240001", "Nguyễn Minh Anh", "Nam"),
        ("HKH-SV02", "2001240002", "Trần Hoàng Nam", "Nam"),
        ("HKH-SV05", "2001240003", "Hoàng Ngọc Linh", "Nữ"),
    ];

    private sealed record Major(string Code, string Name, string Specialization, string StudentPrefix, string HomeClass);
    private sealed record Course(string Code, string Name, string Major, int Ly, int Th, int Semester, bool Elective = false);
    private sealed record Semester(int Order, string Code, string Name, DateTime Start, DateTime End);
    private sealed record Lecturer(string Code, string Name, string Gender, string Major, string Degree, string Title);
    private sealed record Student(string Code, string Name, string Gender, string Major, string HomeClass, double Reliability, double Ability);

    private static readonly Major[] Majors =
    [
        new("CNTT", "Công nghệ thông tin", "Kỹ thuật phần mềm", "20012400", "16DHTH"),
        new("QTKD", "Quản trị kinh doanh", "Quản trị kinh doanh", "20132400", "16DHQT"),
    ];

    private static readonly Semester[] Semesters =
    [
        new(3, "HK1-2526", "HK1 (2025 - 2026)", new(2025, 9, 8), new(2025, 12, 21)),
        new(4, "HK2-2526", "HK2 (2025 - 2026)", new(2026, 2, 9), new(2026, 5, 24)),
        new(5, "HK1-2627", "HK1 (2026 - 2027)", new(2026, 9, 7), new(2026, 12, 20)),
        new(6, "HK2-2627", "HK2 (2026 - 2027)", new(2027, 2, 1), new(2027, 5, 16)),
    ];

    private static readonly Course[] Courses =
    [
        new("IT301", "Cơ sở dữ liệu", "CNTT", 2, 1, 3),
        new("IT302", "Cấu trúc dữ liệu và giải thuật", "CNTT", 2, 1, 3),
        new("IT303", "Lập trình hướng đối tượng", "CNTT", 2, 1, 3),
        new("IT401", "Lập trình Web", "CNTT", 2, 1, 4),
        new("IT402", "Mạng máy tính", "CNTT", 2, 1, 4),
        new("IT403", "Phân tích thiết kế hệ thống", "CNTT", 3, 0, 4),
        new("IT501", "Lập trình di động", "CNTT", 2, 1, 5),
        new("IT502", "Công nghệ phần mềm", "CNTT", 3, 0, 5),
        new("IT503", "Trí tuệ nhân tạo", "CNTT", 2, 1, 5),
        new("IT504", "An toàn thông tin", "CNTT", 2, 1, 5, Elective: true),
        new("IT601", "Điện toán đám mây", "CNTT", 2, 1, 6),
        new("IT602", "Đồ án chuyên ngành", "CNTT", 0, 2, 6),
        new("BA301", "Quản trị học", "QTKD", 3, 0, 3),
        new("BA302", "Marketing căn bản", "QTKD", 3, 0, 3),
        new("BA401", "Quản trị nhân sự", "QTKD", 3, 0, 4),
        new("BA402", "Tài chính doanh nghiệp", "QTKD", 3, 0, 4),
        new("BA501", "Quản trị chiến lược", "QTKD", 3, 0, 5),
        new("BA502", "Hành vi người tiêu dùng", "QTKD", 3, 0, 5),
        new("BA601", "Quản trị dự án", "QTKD", 3, 0, 6),
    ];

    // Mã giảng viên trùng với DbInitializer để lần khởi động backend sau không tạo thêm hồ sơ.
    private static readonly Lecturer[] Lecturers =
    [
        new("GV001", "Nguyễn Văn Thành", "Nam", "CNTT", "Thạc sĩ", "Giảng viên"),
        new("GV_CNTT01", "Lê Tiến Đạt", "Nam", "CNTT", "Tiến sĩ", "Giảng viên chính"),
        new("GV_CNTT02", "Nguyễn Thị Minh Ngọc", "Nữ", "CNTT", "Thạc sĩ", "Giảng viên"),
        new("GV_CNTT03", "Phạm Hoàng Anh", "Nam", "CNTT", "Tiến sĩ", "Phó Giáo sư"),
        new("GV_CNTT04", "Trần Trọng Cung", "Nam", "CNTT", "Thạc sĩ", "Giảng viên"),
        new("GV_CNTT05", "Vũ Kim Yến", "Nữ", "CNTT", "Tiến sĩ", "Giảng viên"),
        new("GV_QTKD01", "Hoàng Quốc Bảo", "Nam", "QTKD", "Tiến sĩ", "Giảng viên chính"),
        new("GV_QTKD02", "Đặng Thu Thảo", "Nữ", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_QTKD03", "Bùi Gia Huy", "Nam", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_QTKD04", "Lý Lan Phương", "Nữ", "QTKD", "Tiến sĩ", "Giảng viên"),
        new("GV_QTKD05", "Hồ Tài Phát", "Nam", "QTKD", "Tiến sĩ", "Phó Giáo sư"),
        new("GV_MKT01", "Nguyễn Thị Mai Phương", "Nữ", "QTKD", "Tiến sĩ", "Giảng viên"),
        new("GV_MKT02", "Trần Hoàng Nam", "Nam", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_MKT03", "Lê Cẩm Tú", "Nữ", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_MKT04", "Phạm Vũ Bảo", "Nam", "QTKD", "Tiến sĩ", "Phó Giáo sư"),
        new("GV_MKT05", "Đỗ Thị Thu Trang", "Nữ", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_MKT06", "Võ Minh Khoa", "Nam", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_MKT07", "Bùi Hoàng Anh", "Nam", "QTKD", "Tiến sĩ", "Giảng viên"),
        new("GV_MKT08", "Ngô Phương Linh", "Nữ", "QTKD", "Thạc sĩ", "Giảng viên"),
        new("GV_MKT09", "Đặng Quốc Huy", "Nam", "QTKD", "Tiến sĩ", "Giảng viên"),
        new("GV_MKT10", "Nguyễn Phan Khánh Vân", "Nữ", "QTKD", "Thạc sĩ", "Giảng viên"),
    ];

    private static readonly (string Code, string Name, string Type)[] Rooms =
    [
        ("A101", "Phòng A101", "Lý thuyết"), ("A102", "Phòng A102", "Lý thuyết"), ("A103", "Phòng A103", "Lý thuyết"),
        ("A104", "Phòng A104", "Lý thuyết"), ("A105", "Phòng A105", "Lý thuyết"), ("A106", "Phòng A106", "Lý thuyết"),
        ("B201", "Phòng thực hành B201", "Thực hành"), ("B202", "Phòng thực hành B202", "Thực hành"),
        ("B203", "Phòng thực hành B203", "Thực hành"), ("B204", "Phòng thực hành B204", "Thực hành"),
    ];

    // Ca học: sáng tiết 1–3, chiều tiết 6–8, tối tiết 11–13 (để có lớp mở được QR buổi tối khi demo).
    private static readonly (int From, int To)[] Slots = [(1, 3), (6, 8), (11, 13)];

    private static readonly string[] FamilyNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"];
    private static readonly string[] MaleMiddle = ["Văn", "Minh", "Quốc", "Hoàng", "Đức", "Gia", "Thành", "Hữu", "Công", "Tuấn"];
    private static readonly string[] FemaleMiddle = ["Thị", "Ngọc", "Thu", "Phương", "Bảo", "Khánh", "Thanh", "Mỹ", "Hồng", "Kim"];
    private static readonly string[] MaleGiven = ["Khang", "Huy", "Phúc", "Nam", "Long", "Tài", "Khoa", "Bảo", "Hiếu", "Tùng", "Dũng", "Trí", "Sơn", "Thắng", "Quân"];
    private static readonly string[] FemaleGiven = ["Linh", "Trang", "Vy", "Ngân", "Hân", "Thảo", "Nhi", "My", "Uyên", "Hà", "Quỳnh", "Tiên", "Yến", "Châu", "Giang"];
    private static readonly string[] Provinces = ["TP. Hồ Chí Minh", "Đồng Nai", "Bình Dương", "Long An", "Tiền Giang", "Bến Tre", "Đắk Lắk", "Lâm Đồng", "Bình Định", "Quảng Ngãi", "Khánh Hòa", "Cần Thơ"];

    public static async Task RunAsync(string connection, string confirmDatabase)
    {
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không chạy reset dữ liệu trên Azure SQL.");
        if (!string.Equals(target.InitialCatalog, confirmDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Tên database xác nhận ({confirmDatabase}) không khớp database đích ({target.InitialCatalog}).");

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        db.Database.SetCommandTimeout(300);
        var today = ClassPeriods.BayGioVietNam().Date;
        var random = new Random(2026);

        // Tìm theo cả mã cũ lẫn mã mới để chạy lại seed nhiều lần vẫn giữ được khuôn mặt đã chuyển.
        var faceCodes = FaceTransfers.SelectMany(x => new[] { x.Old, x.New }).ToArray();
        var faces = await db.HoSoKhuonMats.AsNoTracking().Where(x => faceCodes.Contains(x.MaSinhVien)).ToListAsync();
        var photos = await db.Set<StudentIdentityPhoto>().AsNoTracking().Where(x => faceCodes.Contains(x.MaSinhVien)).ToListAsync();
        // Dữ liệu sinh trắc không tạo lại được: dừng hẳn nếu có hồ sơ khuôn mặt nằm ngoài danh sách chuyển.
        var untracked = await db.HoSoKhuonMats.Where(x => !faceCodes.Contains(x.MaSinhVien)).Select(x => x.MaSinhVien)
            .Concat(db.Set<StudentIdentityPhoto>().Where(x => !faceCodes.Contains(x.MaSinhVien)).Select(x => x.MaSinhVien))
            .Distinct().ToListAsync();
        if (untracked.Count > 0)
            throw new InvalidOperationException($"Có hồ sơ khuôn mặt/ảnh thẻ chưa nằm trong FaceTransfers: {string.Join(", ", untracked)}. Thêm vào danh sách chuyển trước khi reset.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        await ClearAsync(db);

        // ---- Danh mục ----
        foreach (var m in Majors)
        {
            await db.Database.ExecuteSqlAsync($"INSERT INTO NganhHoc (maNganh, tenNganh) VALUES ({m.Code}, {m.Name})");
            await db.Database.ExecuteSqlAsync($"INSERT INTO KhoaHoc (MaKhoa, TenKhoa, MaNganh, NamBatDau, ThoiGianDaoTao) VALUES ({"K2024-" + m.Code}, {"Khóa 2024 - " + m.Name}, {m.Code}, {2024}, {4.0})");
            db.NganhDaoTaos.Add(new NganhDaoTao { MaNganh = m.Code, TenNganh = m.Name });
        }
        await db.Database.ExecuteSqlAsync($"INSERT INTO KhoaTuyenSinh (maKhoa, namBatDau, namKetThuc) VALUES ({"K2024"}, {2024}, {2028}), ({"K2025"}, {2025}, {2029})");

        foreach (var s in Semesters)
            db.HocKys.Add(new HocKyThucTe { MaHocKy = s.Code, TenHocKy = s.Name, LoaiHocKy = "0", NgayBatDau = s.Start, NgayKetThuc = s.End });
        foreach (var r in Rooms)
            db.PhongHocs.Add(new PhongHoc { MaPhong = r.Code, TenPhong = r.Name, LoaiPhong = r.Type, DiaChiCoSo = "Cơ sở chính" });
        foreach (var c in Courses)
            db.MonHocs.Add(new MonHoc
            {
                MaMonHoc = c.Code, TenMonHoc = c.Name, MaNganh = c.Major, LaMonTuChon = c.Elective,
                SoTinChiLyThuyet = c.Ly, SoTinChiThucHanh = c.Th, SoTinChi = c.Ly + c.Th,
                SoTietLyThuyet = c.Ly * 15, SoTietThucHanh = c.Th * 30
            });
        await db.SaveChangesAsync();

        // Kế hoạch đào tạo K2024 của mỗi ngành, đồng bộ sang bảng chương trình cũ dùng khi phân công.
        var plans = new Dictionary<string, KeHoachDaoTao>();
        foreach (var m in Majors)
        {
            var plan = new KeHoachDaoTao { MaNganh = m.Code, MaKhoa = "K2024", TenKhoaDaoTao = "Khóa 2024", TenChuongTrinh = $"Chương trình đào tạo {m.Name} - K2024" };
            foreach (var c in Courses.Where(x => x.Major == m.Code))
            {
                plan.MonHocs.Add(new MonKeHoachDaoTao { MaMonHoc = c.Code, ThuTuHocKy = c.Semester, BatBuoc = !c.Elective });
                await db.Database.ExecuteSqlAsync($"INSERT INTO ChuongTrinhDaoTao (maChuongTrinh, maNganh, maKhoa, maMonHoc, thuTuHocKy) VALUES ({m.Code + "-K2024-" + c.Code}, {m.Code}, {"K2024"}, {c.Code}, {c.Semester})");
            }
            db.KeHoachDaoTaos.Add(plan);
            plans[m.Code] = plan;
        }
        await db.SaveChangesAsync();
        db.DieuKienHocPhans.AddRange(
            new DieuKienHocPhan { KeHoachDaoTaoId = plans["CNTT"].Id, MaMonHoc = "IT401", MaMonDieuKien = "IT303", Loai = "hoctruoc" },
            new DieuKienHocPhan { KeHoachDaoTaoId = plans["CNTT"].Id, MaMonHoc = "IT502", MaMonDieuKien = "IT403", Loai = "tienquyet" },
            new DieuKienHocPhan { KeHoachDaoTaoId = plans["CNTT"].Id, MaMonHoc = "IT503", MaMonDieuKien = "IT302", Loai = "tienquyet" },
            new DieuKienHocPhan { KeHoachDaoTaoId = plans["QTKD"].Id, MaMonHoc = "BA501", MaMonDieuKien = "BA301", Loai = "tienquyet" });

        // ---- Người dùng ----
        var roles = await db.Roles.ToDictionaryAsync(x => x.Name);
        var hasher = new PasswordHasher<User>();
        User Account(string userName, string fullName, string email, string role)
        {
            var user = new User { UserName = userName, FullName = fullName, Email = email, MustChangePassword = false, IsActive = true };
            user.PasswordHash = hasher.HashPassword(user, Password);
            user.UserRoles.Add(new UserRole { User = user, RoleId = roles[role].Id });
            db.Users.Add(user);
            return user;
        }
        Account("admin", "Quản trị hệ thống", "admin@local.test", RoleNames.Admin);
        Account("CBQL001", "Phạm Thị Thu Hà", "cbql001@local.test", RoleNames.CanBoQuanLy);
        Account("CBQL002", "Lê Văn Hùng", "cbql002@local.test", RoleNames.CanBoQuanLy);
        var students = BuildStudents(random);
        // Lưu tài khoản trước để hồ sơ giảng viên/sinh viên trỏ tới UserId đã có trong database.
        var lecturerUsers = Lecturers.ToDictionary(l => l.Code,
            l => Account(l.Code, l.Name, $"{l.Code.ToLowerInvariant().Replace("_", "")}@gv.local.test", RoleNames.GiangVien));
        var studentUsers = students.ToDictionary(s => s.Code,
            s => Account(s.Code, s.Name, $"{s.Code}@sv.local.test", RoleNames.SinhVien));
        await db.SaveChangesAsync();

        foreach (var (l, i) in Lecturers.Select((x, i) => (x, i)))
        {
            var major = Majors.Single(x => x.Code == l.Major);
            var email = lecturerUsers[l.Code].Email;
            db.GiangViens.Add(new GiangVien
            {
                MaGiangVien = l.Code, HoTen = l.Name, GioiTinh = l.Gender, Email = email, SoDienThoai = $"0903{i + 1:000000}",
                Khoa = major.Name, ChuyenNganh = major.Name, TrinhDoHocVan = l.Degree, ChucDanh = l.Title,
                CoSo = "Cơ sở chính", QuocTich = "Việt Nam", DanToc = "Kinh", TrangThaiTaiKhoan = true,
                NgaySinh = new DateTime(1975 + i % 18, 1 + i % 12, 1 + i * 3 % 27), NgayBatDauCongTac = new DateTime(2008 + i % 14, 9, 1),
                UserId = lecturerUsers[l.Code].Id
            });
        }

        foreach (var (s, i) in students.Select((x, i) => (x, i)))
        {
            var major = Majors.Single(x => x.Code == s.Major);
            var email = studentUsers[s.Code].Email;
            db.SinhViens.Add(new SinhVien
            {
                MaSinhVien = s.Code, HoTen = s.Name, GioiTinh = s.Gender, Email = email, SoDienThoai = $"0977{i + 1:000000}",
                NgaySinh = new DateTime(2006, 1 + i % 12, 1 + i * 7 % 28), NoiSinh = Provinces[i % Provinces.Length],
                DanToc = "Kinh", QuocTich = "Việt Nam", TonGiao = "Không", LopHoc = s.HomeClass, CoSo = "Cơ sở chính",
                BacDaoTao = "Đại học", LoaiHinhDaoTao = "Chính quy", Khoa = major.Name, Nganh = major.Name,
                ChuyenNganh = major.Specialization, KhoaHoc = "2024 - 2028", NgayVaoTruong = new DateTime(2024, 9, 5),
                DiaChiLienHe = $"{Provinces[(i + 3) % Provinces.Length]}", TrangThaiTaiKhoan = true,
                UserId = studentUsers[s.Code].Id
            });
        }
        await db.SaveChangesAsync();

        // Gắn lại hồ sơ khuôn mặt và ảnh thẻ cũ cho sinh viên mới; khóa mã hóa không phụ thuộc mã sinh viên.
        foreach (var t in FaceTransfers)
        {
            var face = faces.FirstOrDefault(x => x.MaSinhVien == t.New) ?? faces.FirstOrDefault(x => x.MaSinhVien == t.Old);
            if (face is not null)
            {
                face.MaSinhVien = t.New;
                face.ApprovedBy = face.ApprovedAtUtc is null ? null : "CBQL001";
                face.SinhVien = null!;
                db.HoSoKhuonMats.Add(face);
            }
            var photo = photos.FirstOrDefault(x => x.MaSinhVien == t.New) ?? photos.FirstOrDefault(x => x.MaSinhVien == t.Old);
            if (photo is not null)
            {
                photo.MaSinhVien = t.New;
                photo.UpdatedBy = "CBQL001";
                photo.SinhVien = null!;
                db.Add(photo);
            }
        }

        // ---- Lớp học phần, lịch, buổi học, điểm danh, kết quả ----
        var occupied = new HashSet<string>();
        var classCount = 0;
        var rows = new List<(Student Student, string Course, string Semester, int Credits, string? Grade)>();
        foreach (var semester in Semesters)
        {
            var apply = Majors.ToDictionary(m => m.Code, m =>
            {
                var a = new ApDungHocKy { KeHoachDaoTaoId = plans[m.Code].Id, MaHocKy = semester.Code, ThuTuHocKy = semester.Order };
                db.ApDungHocKys.Add(a);
                return a;
            });
            var lecturerTurn = new Dictionary<string, int>();
            foreach (var course in Courses.Where(x => x.Semester == semester.Order))
            {
                var cohort = students.Where(x => x.Major == course.Major).ToList();
                var groups = course.Elective
                    ? [cohort.Where((_, i) => i % 2 == 0).ToList()]
                    : cohort.GroupBy(x => x.HomeClass).OrderBy(x => x.Key).Select(x => x.ToList()).ToList();
                for (var g = 0; g < groups.Count; g++)
                {
                    var code = $"{course.Code}-{semester.Code[2]}{semester.Code[^2..]}-{g + 1:00}";
                    var planned = new LopHocPhanDuKien { MaLopHocPhan = code, MaMonHoc = course.Code, ApDungHocKyId = apply[course.Major].Id, SiSoToiDa = 45 };
                    db.LopHocPhanDuKiens.Add(planned);
                    if (semester.Order == 6) continue; // Học kỳ sau: lớp chờ CBQL phân công.
                    planned.DaPhanCong = true;

                    var pool = Lecturers.Where(x => x.Major == course.Major).ToArray();
                    var turn = lecturerTurn.GetValueOrDefault(course.Major);
                    var homeClasses = course.Elective ? cohort.Select(x => x.HomeClass).Distinct().ToArray() : [groups[g][0].HomeClass];
                    var room = course.Th > course.Ly ? Rooms.Where(x => x.Type == "Thực hành") : Rooms.Where(x => x.Type == "Lý thuyết");
                    // Giảng viên GV001 luôn dạy lớp đầu tiên của học kỳ hiện tại để demo đăng nhập GV001.
                    var lecturer = semester.Order == 5 && course.Code == "IT501" && g == 0 ? Lecturers[0] : pool[turn++ % pool.Length];
                    lecturerTurn[course.Major] = turn;
                    var (day, slot, roomCode) = Allocate(occupied, semester.Code, lecturer.Code, homeClasses, room.Select(x => x.Code).ToArray(), classCount++);
                    var dates = TrainingRules.SessionDates(semester.Start, semester.End, day).ToList();
                    var schedule = $"LH-{code}";
                    db.LopHocPhans.Add(new LopHocPhan
                    {
                        MaLopHocPhan = code, MaMonHoc = course.Code, MaGiangVien = lecturer.Code, MaHocKy = semester.Code,
                        SiSoToiDa = 45, TrangThai = semester.Order == 5 ? "0" : "1", LoaiPhong = room.First().Type, SoBuoi = dates.Count,
                        LichHoc = $"{(day == 8 ? "Chủ nhật" : $"Thứ {day}")}, tiết {Slots[slot].From}-{Slots[slot].To}",
                        NgayBatDau = semester.Start.ToString("yyyy-MM-dd"), NgayKetThuc = semester.End.ToString("yyyy-MM-dd")
                    });
                    db.LichHocs.Add(new LichHoc { MaLichHoc = schedule, MaLopHocPhan = code, MaPhong = roomCode, ThuTrongTuan = day, TietBatDau = Slots[slot].From, TietKetThuc = Slots[slot].To });

                    var absences = groups[g].ToDictionary(x => x.Code, _ => 0);
                    foreach (var date in dates)
                    {
                        var held = date < today;
                        var sessionCode = $"BH-{code}-{date:yyyyMMdd}";
                        db.BuoiHocs.Add(new BuoiHoc { MaBuoiHoc = sessionCode, MaLichHoc = schedule, NgayHocCuThe = date, TrangThaiBuoiHoc = held ? "DaChot" : "ChuaDienRa" });
                        if (!held) continue;
                        var start = date.Add(ClassPeriods.GioBatDauTiet(Slots[slot].From)!.Value.ToTimeSpan()).AddHours(-7);
                        foreach (var s in groups[g])
                        {
                            var status = random.NextDouble() < s.Reliability
                                ? (random.NextDouble() < 0.08 ? "DiTre" : "CoMat")
                                : (random.NextDouble() < 0.3 ? "CoPhep" : "Vang");
                            if (status == "Vang") absences[s.Code]++;
                            var present = status is "CoMat" or "DiTre";
                            var outside = present && random.NextDouble() < 0.02;
                            db.ChiTietDiemDanhs.Add(new ChiTietDiemDanh
                            {
                                MaBuoiHoc = sessionCode, MaSinhVien = s.Code, TrangThai = status,
                                ThoiGianCheckIn = present ? start.AddMinutes(status == "DiTre" ? random.Next(16, 35) : random.Next(-10, 10)) : null,
                                KetQuaViTri = present ? (outside ? "NgoaiKhuonVien" : "TrongKhuonVien") : null,
                                KhoangCachMet = present ? (outside ? random.Next(800, 2500) : random.Next(5, 180)) : null
                            });
                        }
                    }

                    foreach (var s in groups[g])
                    {
                        var vang = absences[s.Code];
                        var level = vang >= 4 ? "CamThi" : vang >= 2 ? "CanhBao" : "BinhThuong";
                        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop
                        {
                            MaSinhVien = s.Code, MaLopHocPhan = code, LoaiDangKy = "moi",
                            ThoiGianDangKy = semester.Start.AddDays(-20), TongSoBuoiVang = vang, TrangThaiCanhBao = level
                        });
                        if (semester.Order >= 5) continue; // Học kỳ hiện tại chưa có điểm: giảng viên tự nhập.
                        decimal? score = level == "CamThi" ? null
                            : Math.Clamp(Math.Round((decimal)(s.Ability + Gaussian(random) * 0.8 - (1 - s.Reliability) * 3), 1), 0m, 10m);
                        var grade = score is null ? "F" : AcademicTranscript.DiemChuTuDiem10(score.Value);
                        db.KetQuaHocPhans.Add(new KetQuaHocPhan
                        {
                            MaSinhVien = s.Code, MaMonHoc = course.Code, MaHocKy = semester.Code, DiemTongKet = score, DiemChu = grade,
                            DaCongBo = true, NguoiNhap = lecturer.Code, NhapLucUtc = semester.End.AddDays(10)
                        });
                        rows.Add((s, course.Code, semester.Code, course.Ly + course.Th, grade));
                    }
                }
            }
            await db.SaveChangesAsync();
        }

        // Hồ sơ học vụ: đang ở học kỳ 5, GPA tích lũy tính từ các điểm đã công bố.
        foreach (var s in students)
        {
            var graded = rows.Where(x => x.Student.Code == s.Code).ToList();
            var credits = graded.Sum(x => x.Credits);
            var gpa = credits == 0 ? (decimal?)null
                : Math.Round(graded.Sum(x => AcademicTranscript.Diem4(x.Grade)!.Value * x.Credits) / credits, 2);
            db.HoSoHocVus.Add(new HoSoHocVu { MaSinhVien = s.Code, ThuTuHocKy = 5, GpaTichLuy = gpa });
        }

        db.DotDangKyHocPhans.AddRange(
            new DotDangKyHocPhan { TenDot = "Đăng ký học phần HK1 (2026 - 2027)", MaHocKy = "HK1-2627", MoLucUtc = new DateTime(2026, 8, 10, 1, 0, 0), DongLucUtc = new DateTime(2026, 8, 24, 16, 59, 0) },
            new DotDangKyHocPhan { TenDot = "Đăng ký học phần HK2 (2026 - 2027)", MaHocKy = "HK2-2627", MoLucUtc = new DateTime(2027, 1, 4, 1, 0, 0), DongLucUtc = new DateTime(2027, 1, 17, 16, 59, 0) });

        var now = DateTime.UtcNow;
        db.ThongBaos.AddRange(
            new ThongBao { TieuDe = "Kế hoạch đăng ký học phần HK2 (2026 - 2027)", NoiDung = "Sinh viên đăng ký học phần HK2 từ 04/01/2027 đến 17/01/2027 trên cổng sinh viên. Kiểm tra điều kiện tiên quyết trước khi đăng ký.", HieuLucTuUtc = now.AddDays(-2), TaoLucUtc = now.AddDays(-2) },
            new ThongBao { TieuDe = "Hướng dẫn điểm danh bằng QR và khuôn mặt", NoiDung = "Sinh viên đăng ký hồ sơ khuôn mặt trên ứng dụng di động trước khi điểm danh. Khi giảng viên mở phiên, quét mã QR trên màn chiếu rồi xác minh khuôn mặt.", HieuLucTuUtc = now.AddDays(-30), TaoLucUtc = now.AddDays(-30) },
            new ThongBao { TieuDe = "Nhắc nhở chuyên cần giữa kỳ", NoiDung = "Sinh viên vắng từ 2 buổi sẽ bị cảnh báo, từ 4 buổi bị cấm thi. Xem tình trạng chuyên cần trong mục Kết quả học tập.", HieuLucTuUtc = now.AddDays(-5), TaoLucUtc = now.AddDays(-5) },
            new ThongBao { TieuDe = "Đổi phòng lớp Lập trình di động", NoiDung = "Từ tuần sau, lớp IT501-127-01 học tại phòng thực hành B204.", MaLopHocPhan = "IT501-127-01", HieuLucTuUtc = now.AddDays(-1), TaoLucUtc = now.AddDays(-1) });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine($"Đã nạp: {Majors.Length} ngành, {Semesters.Length} học kỳ, {Courses.Length} môn, {Lecturers.Length} giảng viên, {students.Count} sinh viên, {classCount} lớp học phần.");
        Console.WriteLine($"Chi tiết điểm danh: {await db.ChiTietDiemDanhs.CountAsync()}, kết quả học phần: {await db.KetQuaHocPhans.CountAsync()}, hồ sơ khuôn mặt chuyển: {faces.Count}, ảnh thẻ chuyển: {photos.Count}.");
        foreach (var t in FaceTransfers)
            Console.WriteLine($"  Khuôn mặt {t.Old} -> {t.New} ({t.HoTen}): {(faces.Any(x => x.MaSinhVien == t.New) ? "có hồ sơ" : "không có hồ sơ")}, {(photos.Any(x => x.MaSinhVien == t.New) ? "có ảnh thẻ" : "không có ảnh thẻ")}");
    }

    /// <summary>Xoá dữ liệu mọi bảng trừ lịch sử migration, vai trò và cờ đánh dấu database cá nhân.</summary>
    private static async Task ClearAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.NganhHoc', N'U') IS NULL
                CREATE TABLE dbo.NganhHoc (maNganh varchar(50) NOT NULL PRIMARY KEY, tenNganh nvarchar(200) NOT NULL);
            IF OBJECT_ID(N'dbo.KhoaTuyenSinh', N'U') IS NULL
                CREATE TABLE dbo.KhoaTuyenSinh (maKhoa varchar(50) NOT NULL PRIMARY KEY, namBatDau int NOT NULL, namKetThuc int NOT NULL);
            IF OBJECT_ID(N'dbo.ChuongTrinhDaoTao', N'U') IS NULL
                CREATE TABLE dbo.ChuongTrinhDaoTao (
                    maChuongTrinh varchar(100) NOT NULL PRIMARY KEY,
                    maNganh varchar(50) NOT NULL REFERENCES dbo.NganhHoc(maNganh),
                    maKhoa varchar(50) NOT NULL REFERENCES dbo.KhoaTuyenSinh(maKhoa),
                    maMonHoc nvarchar(30) NOT NULL REFERENCES dbo.MonHoc(MaMonHoc),
                    thuTuHocKy int NOT NULL);
            IF OBJECT_ID(N'dbo.KhoaHoc', N'U') IS NULL
                CREATE TABLE dbo.KhoaHoc (MaKhoa nvarchar(50) PRIMARY KEY, TenKhoa nvarchar(100) NOT NULL,
                    MaNganh nvarchar(50) NOT NULL, NamBatDau int NOT NULL, ThoiGianDaoTao float NOT NULL);
            """);
        var tables = await db.Database.SqlQueryRaw<string>("""
            SELECT QUOTENAME(s.name) + '.' + QUOTENAME(t.name) AS Value
            FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.name NOT IN (N'__EFMigrationsHistory', N'Roles', N'PersonalSeederMarker')
            """).ToListAsync();
        foreach (var t in tables) await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {t} NOCHECK CONSTRAINT ALL");
        foreach (var t in tables) await db.Database.ExecuteSqlRawAsync($"DELETE FROM {t}");
        foreach (var t in tables) await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {t} WITH CHECK CHECK CONSTRAINT ALL");
        foreach (var t in tables)
            await db.Database.ExecuteSqlRawAsync($"IF OBJECTPROPERTY(OBJECT_ID('{t.Replace("'", "''")}'), 'TableHasIdentity') = 1 DBCC CHECKIDENT ('{t.Replace("'", "''")}', RESEED, 0)");
    }

    private static List<Student> BuildStudents(Random random)
    {
        var list = new List<Student>();
        foreach (var major in Majors)
        {
            var count = major.Code == "CNTT" ? 40 : 20;
            for (var i = 1; i <= count; i++)
            {
                var code = $"{major.StudentPrefix}{i:00}";
                var transfer = FaceTransfers.FirstOrDefault(x => x.New == code);
                var female = transfer.New is null ? random.NextDouble() < (major.Code == "CNTT" ? 0.3 : 0.6) : transfer.GioiTinh == "Nữ";
                var name = transfer.New is not null ? transfer.HoTen
                    : $"{Pick(random, FamilyNames)} {Pick(random, female ? FemaleMiddle : MaleMiddle)} {Pick(random, female ? FemaleGiven : MaleGiven)}";
                var homeClass = major.Code == "CNTT" ? $"{major.HomeClass}{(i <= 20 ? "01" : "02")}" : $"{major.HomeClass}01";
                list.Add(new Student(code, name, female ? "Nữ" : "Nam", major.Code, homeClass, Reliability(random, transfer.New is not null), 6.2 + random.NextDouble() * 2.8));
            }
        }
        // Tài khoản sinh viên mặc định của DbInitializer, xếp vào lớp CNTT thứ hai.
        list.Add(new Student("2001230048", "Trần Quốc Bảo", "Nam", "CNTT", "16DHTH02", 0.93, 7.6));
        return list;
    }

    // Phần lớn sinh viên đi học đều; một số ít vắng nhiều để có cảnh báo và cấm thi khi demo.
    private static double Reliability(Random random, bool faceAccount)
    {
        if (faceAccount) return 0.97;
        var roll = random.NextDouble();
        return roll < 0.75 ? 0.97 : roll < 0.93 ? 0.88 : 0.72;
    }

    private static (int Day, int Slot, string Room) Allocate(HashSet<string> occupied, string semester, string lecturer, string[] homeClasses, string[] rooms, int seed)
    {
        for (var k = 0; k < 6 * Slots.Length; k++)
        {
            var index = (seed * 5 + k) % (6 * Slots.Length);
            var day = 2 + index % 6;
            var slot = index / 6;
            if (occupied.Contains($"{semester}|{day}|{slot}|GV|{lecturer}")) continue;
            if (homeClasses.Any(h => occupied.Contains($"{semester}|{day}|{slot}|LOP|{h}"))) continue;
            var room = rooms.FirstOrDefault(r => !occupied.Contains($"{semester}|{day}|{slot}|P|{r}"));
            if (room is null) continue;
            occupied.Add($"{semester}|{day}|{slot}|GV|{lecturer}");
            occupied.Add($"{semester}|{day}|{slot}|P|{room}");
            foreach (var h in homeClasses) occupied.Add($"{semester}|{day}|{slot}|LOP|{h}");
            return (day, slot, room);
        }
        throw new InvalidOperationException($"Không xếp được lịch cho giảng viên {lecturer} trong {semester}.");
    }

    private static T Pick<T>(Random random, T[] items) => items[random.Next(items.Length)];

    private static double Gaussian(Random random) =>
        Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Sin(2.0 * Math.PI * random.NextDouble());
}
