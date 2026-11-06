using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Contracts.Academic;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using QuanLySinhVien.Api.Services;

namespace QuanLySinhVien.Api.Controllers;

/// <summary>
/// Giảng viên phụ trách nhập điểm tổng kết thang 10 cho lớp học phần, lưu nháp rồi công bố.
/// Sinh viên chỉ thấy điểm sau khi công bố; đã công bố thì giảng viên không sửa được nữa.
/// </summary>
[ApiController, Route("api/v1/giang-day/lop/{maLopHocPhan}/diem")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy + "," + RoleNames.GiangVien)]
public sealed class ClassGradeController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClassGradeSheetResponse>> Get(string maLopHocPhan, CancellationToken ct)
    {
        var lop = await FindClassAsync(maLopHocPhan, ct);
        if (lop is null) return NotFound(new { message = "Không tìm thấy lớp học phần." });
        if (!CanAccess(lop.MaGiangVien)) return Forbid();
        if (lop.MaHocKy is null) return Conflict(new { message = "Lớp học phần chưa gắn học kỳ nên chưa nhập điểm được." });
        return Ok(await BuildSheetAsync(lop, ct));
    }

    [HttpPut]
    public async Task<IActionResult> Save(string maLopHocPhan, SaveClassGradesRequest request, CancellationToken ct)
    {
        var lop = await FindClassAsync(maLopHocPhan, ct);
        if (lop is null) return NotFound(new { message = "Không tìm thấy lớp học phần." });
        if (!CanAccess(lop.MaGiangVien)) return Forbid();
        if (lop.MaHocKy is null) return Conflict(new { message = "Lớp học phần chưa gắn học kỳ nên chưa nhập điểm được." });

        var invalid = request.Diem.FirstOrDefault(x => x.DiemTongKet is { } d && (d < 0 || d > 10 || decimal.Round(d, 1) != d));
        if (invalid is not null)
            return BadRequest(new { message = $"Điểm của sinh viên {invalid.MaSinhVien} phải từ 0 đến 10, lẻ tối đa 0,1." });

        var students = await StudentsAsync(maLopHocPhan, ct);
        var unknown = request.Diem.FirstOrDefault(x => !students.ContainsKey(x.MaSinhVien));
        if (unknown is not null)
            return BadRequest(new { message = $"Sinh viên {unknown.MaSinhVien} không thuộc lớp học phần này." });

        var existing = await Results(lop).ToDictionaryAsync(x => x.MaSinhVien, ct);
        if (existing.Values.Any(x => x.DaCongBo))
            return Conflict(new { message = "Điểm lớp này đã công bố, không thể sửa. Liên hệ phòng đào tạo nếu cần điều chỉnh." });

        var now = DateTime.UtcNow;
        foreach (var entry in request.Diem)
        {
            existing.TryGetValue(entry.MaSinhVien, out var row);
            var camThi = students[entry.MaSinhVien].CamThi;
            if (entry.DiemTongKet is null && !camThi)
            {
                // Xóa điểm nháp khi giảng viên để trống ô.
                if (row is not null) db.KetQuaHocPhans.Remove(row);
                continue;
            }
            if (row is null)
            {
                row = new KetQuaHocPhan { MaSinhVien = entry.MaSinhVien, MaMonHoc = lop.MaMonHoc, MaHocKy = lop.MaHocKy };
                db.KetQuaHocPhans.Add(row);
            }
            row.DiemTongKet = entry.DiemTongKet;
            row.DiemChu = camThi ? "F" : AcademicTranscript.DiemChuTuDiem10(entry.DiemTongKet!.Value);
            row.NguoiNhap = User.Identity?.Name;
            row.NhapLucUtc = now;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã lưu điểm nháp. Sinh viên chưa thấy điểm cho tới khi công bố." });
    }

    [HttpPost("cong-bo")]
    public async Task<IActionResult> Publish(string maLopHocPhan, CancellationToken ct)
    {
        var lop = await FindClassAsync(maLopHocPhan, ct);
        if (lop is null) return NotFound(new { message = "Không tìm thấy lớp học phần." });
        if (!CanAccess(lop.MaGiangVien)) return Forbid();
        if (lop.MaHocKy is null) return Conflict(new { message = "Lớp học phần chưa gắn học kỳ nên chưa nhập điểm được." });

        var students = await StudentsAsync(maLopHocPhan, ct);
        var rows = await Results(lop).ToListAsync(ct);
        var now = DateTime.UtcNow;
        // Sinh viên cấm thi chưa có dòng điểm thì tạo F để bảng điểm đầy đủ.
        foreach (var (code, _) in students.Where(x => x.Value.CamThi && rows.All(r => r.MaSinhVien != x.Key)))
        {
            var row = new KetQuaHocPhan { MaSinhVien = code, MaMonHoc = lop.MaMonHoc, MaHocKy = lop.MaHocKy, DiemChu = "F",
                NguoiNhap = User.Identity?.Name, NhapLucUtc = now };
            db.KetQuaHocPhans.Add(row);
            rows.Add(row);
        }
        var missing = students.Keys.Where(code => rows.All(r => r.MaSinhVien != code)).ToList();
        if (missing.Count > 0)
            return Conflict(new { message = $"Còn {missing.Count} sinh viên chưa có điểm ({string.Join(", ", missing.Take(5))}{(missing.Count > 5 ? "…" : "")})." });

        foreach (var row in rows) row.DaCongBo = true;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = $"Đã công bố điểm cho {rows.Count} sinh viên." });
    }

    private sealed record ClassInfo(string MaLopHocPhan, string MaMonHoc, string TenMonHoc, string? MaHocKy, string MaGiangVien, string HoTenGiangVien);

    private Task<ClassInfo?> FindClassAsync(string code, CancellationToken ct) =>
        db.LopHocPhans.AsNoTracking()
            .Where(x => x.MaLopHocPhan == code)
            .Select(x => new ClassInfo(x.MaLopHocPhan, x.MaMonHoc, x.MonHoc != null ? x.MonHoc.TenMonHoc : x.MaMonHoc,
                x.MaHocKy, x.MaGiangVien, x.GiangVien != null ? x.GiangVien.HoTen : x.MaGiangVien))
            .SingleOrDefaultAsync(ct);

    private bool CanAccess(string lecturerCode) =>
        User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.CanBoQuanLy)
        || string.Equals(User.Identity?.Name, lecturerCode, StringComparison.OrdinalIgnoreCase);

    private sealed record StudentInfo(string HoTen, bool CamThi, int SoBuoiVang);

    private Task<Dictionary<string, StudentInfo>> StudentsAsync(string code, CancellationToken ct) =>
        db.DanhSachSinhVienLops.AsNoTracking()
            .Where(x => x.MaLopHocPhan == code)
            // Chọn cột trong SQL; bộ chọn của ToDictionaryAsync chạy phía client nên không tự nạp SinhVien.
            .Select(x => new { x.MaSinhVien, x.SinhVien.HoTen, x.TrangThaiCanhBao, x.TongSoBuoiVang })
            .ToDictionaryAsync(x => x.MaSinhVien,
                x => new StudentInfo(x.HoTen, x.TrangThaiCanhBao == "CamThi", x.TongSoBuoiVang), ct);

    private IQueryable<KetQuaHocPhan> Results(ClassInfo lop) =>
        db.KetQuaHocPhans.Where(x => x.MaMonHoc == lop.MaMonHoc && x.MaHocKy == lop.MaHocKy
            && db.DanhSachSinhVienLops.Any(e => e.MaLopHocPhan == lop.MaLopHocPhan && e.MaSinhVien == x.MaSinhVien));

    private async Task<ClassGradeSheetResponse> BuildSheetAsync(ClassInfo lop, CancellationToken ct)
    {
        var students = await StudentsAsync(lop.MaLopHocPhan, ct);
        var results = await Results(lop).AsNoTracking().ToDictionaryAsync(x => x.MaSinhVien, ct);
        var semester = await db.HocKys.AsNoTracking().Where(x => x.MaHocKy == lop.MaHocKy)
            .Select(x => x.TenHocKy).SingleOrDefaultAsync(ct);
        var rows = students.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x =>
        {
            results.TryGetValue(x.Key, out var r);
            return new ClassGradeRowResponse(x.Key, x.Value.HoTen, r?.DiemTongKet,
                x.Value.CamThi ? "F" : r?.DiemChu, x.Value.CamThi, x.Value.SoBuoiVang);
        }).ToList();
        return new ClassGradeSheetResponse(lop.MaLopHocPhan, lop.MaMonHoc, lop.TenMonHoc, lop.MaHocKy!,
            semester ?? lop.MaHocKy!, lop.HoTenGiangVien, results.Values.Any(x => x.DaCongBo),
            results.Values.Max(x => x.NhapLucUtc), rows);
    }
}
