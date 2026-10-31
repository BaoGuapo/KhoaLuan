using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Reporting;
using QuanLySinhVien.Api.Data;

namespace QuanLySinhVien.Api.Services;

/// <summary>
/// Tổng hợp số liệu điểm danh của một học kỳ theo ba mức: toàn học kỳ, theo môn học
/// và theo lớp học phần. Chỉ các buổi đã diễn ra mới được tính vào tỷ lệ chuyên cần.
/// </summary>
public sealed class AttendanceReportService(AppDbContext db)
{
    /// <summary>Có mặt và đi trễ đều được tính là có dự buổi học.</summary>
    private static double Rate(int coMatVaDiTre, int luotDuKien) =>
        luotDuKien <= 0 ? 0 : (double)coMatVaDiTre / luotDuKien;

    public async Task<AttendanceReportResponse?> BuildAsync(string maHocKy, CancellationToken ct)
    {
        var semester = await db.HocKys.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaHocKy == maHocKy, ct);
        if (semester is null) return null;

        var homNay = ClassPeriods.BayGioVietNam().Date;
        var classes = await db.LopHocPhans.AsNoTracking()
            .Where(x => x.MaHocKy == maHocKy)
            .Select(x => new
            {
                x.MaLopHocPhan,
                x.MaMonHoc,
                x.MonHoc.TenMonHoc,
                HoTenGiangVien = x.GiangVien.HoTen,
                SiSo = x.DanhSachSinhVien.Count,
                SoSinhVienCanhBao = x.DanhSachSinhVien
                    .Count(d => d.TrangThaiCanhBao == "CanhBao" || d.TrangThaiCanhBao == "CamThi"),
                TongSoBuoi = db.BuoiHocs.Count(b => b.LichHoc.MaLopHocPhan == x.MaLopHocPhan),
                SoBuoiDaDienRa = db.BuoiHocs
                    .Count(b => b.LichHoc.MaLopHocPhan == x.MaLopHocPhan && b.NgayHocCuThe <= homNay),
                CoMat = db.ChiTietDiemDanhs.Count(d =>
                    d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "CoMat"),
                DiTre = db.ChiTietDiemDanhs.Count(d =>
                    d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "DiTre"),
                CoPhep = db.ChiTietDiemDanhs.Count(d =>
                    d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "CoPhep"),
                Vang = db.ChiTietDiemDanhs.Count(d =>
                    d.BuoiHoc.LichHoc.MaLopHocPhan == x.MaLopHocPhan && d.TrangThai == "Vang")
            })
            .ToListAsync(ct);

        // Lượt dự kiến của một lớp là sĩ số nhân số buổi đã diễn ra; các buổi chưa tới ngày không tính.
        var classRows = classes
            .Select(x =>
            {
                var luotDuKien = x.SiSo * x.SoBuoiDaDienRa;
                var daGhiNhan = x.CoMat + x.DiTre + x.CoPhep + x.Vang;
                return new ClassAttendanceRow(
                    x.MaLopHocPhan, x.MaMonHoc, x.TenMonHoc, x.HoTenGiangVien,
                    x.SiSo, x.SoBuoiDaDienRa,
                    x.CoMat, x.DiTre, x.CoPhep, x.Vang,
                    Math.Max(0, luotDuKien - daGhiNhan),
                    luotDuKien,
                    Rate(x.CoMat + x.DiTre, luotDuKien),
                    x.SoSinhVienCanhBao);
            })
            // Lớp chưa có lượt dự kiến xuống cuối, để đầu bảng là các lớp thật sự chuyên cần kém.
            .OrderBy(x => x.SoLuotDuKien == 0)
            .ThenBy(x => x.TyLeChuyenCan)
            .ThenBy(x => x.MaLopHocPhan)
            .ToList();

        var subjectRows = classRows
            .GroupBy(x => (x.MaMonHoc, x.TenMonHoc))
            .Select(g =>
            {
                var coMat = g.Sum(x => x.CoMat);
                var diTre = g.Sum(x => x.DiTre);
                var luotDuKien = g.Sum(x => x.SoLuotDuKien);
                return new SubjectAttendanceRow(
                    g.Key.MaMonHoc, g.Key.TenMonHoc,
                    g.Count(), g.Sum(x => x.SiSo),
                    coMat, diTre, g.Sum(x => x.CoPhep), g.Sum(x => x.Vang),
                    g.Sum(x => x.ChuaDiemDanh),
                    luotDuKien,
                    Rate(coMat + diTre, luotDuKien));
            })
            .OrderBy(x => x.SoLuotDuKien == 0)
            .ThenBy(x => x.TyLeChuyenCan)
            .ThenBy(x => x.MaMonHoc)
            .ToList();

        var sessions = await db.BuoiHocs.AsNoTracking()
            .Where(x => x.LichHoc.LopHocPhan.MaHocKy == maHocKy && x.NgayHocCuThe <= homNay)
            .Select(x => new
            {
                x.NgayHocCuThe,
                SiSo = x.LichHoc.LopHocPhan.DanhSachSinhVien.Count,
                CoMatVaDiTre = x.ChiTietDiemDanhs.Count(d => d.TrangThai == "CoMat" || d.TrangThai == "DiTre")
            })
            .ToListAsync(ct);

        var dailyRows = sessions
            .GroupBy(x => x.NgayHocCuThe.Date)
            .Select(g =>
            {
                var luotDuKien = g.Sum(x => x.SiSo);
                var coMatVaDiTre = g.Sum(x => x.CoMatVaDiTre);
                return new DailyAttendanceRow(g.Key, g.Count(), luotDuKien, coMatVaDiTre,
                    Rate(coMatVaDiTre, luotDuKien));
            })
            .OrderBy(x => x.Ngay)
            .ToList();

        var tongLuotDuKien = classRows.Sum(x => x.SoLuotDuKien);
        var overview = new AttendanceOverviewResponse(
            await db.DanhSachSinhVienLops.AsNoTracking()
                .Where(x => x.LopHocPhan.MaHocKy == maHocKy)
                .Select(x => x.MaSinhVien)
                .Distinct()
                .CountAsync(ct),
            classRows.Count,
            subjectRows.Count,
            classRows.Sum(x => x.SoBuoiDaDienRa),
            classes.Sum(x => x.TongSoBuoi),
            classRows.Sum(x => x.CoMat),
            classRows.Sum(x => x.DiTre),
            classRows.Sum(x => x.CoPhep),
            classRows.Sum(x => x.Vang),
            classRows.Sum(x => x.ChuaDiemDanh),
            Rate(classRows.Sum(x => x.CoMat + x.DiTre), tongLuotDuKien));

        return new AttendanceReportResponse(
            semester.MaHocKy, semester.TenHocKy, semester.NgayBatDau, semester.NgayKetThuc,
            overview, subjectRows, classRows, dailyRows);
    }
}
