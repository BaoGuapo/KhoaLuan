using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using System.Security.Claims; // Bổ sung để lấy mã sinh viên từ Token

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class DaoTaoController(AppDbContext db) : ControllerBase
{
    // ==========================================================
    // 1. CÁC API CŨ CỦA BẠN (GIỮ NGUYÊN 100%)
    // ==========================================================
    [HttpGet("mon-hoc")]
    [Authorize]
    public Task<List<MonHoc>> GetMonHoc(CancellationToken ct) => db.MonHocs.AsNoTracking().OrderBy(x => x.MaMonHoc).ToListAsync(ct);

    [HttpPost("mon-hoc")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<ActionResult<MonHoc>> CreateMonHoc(MonHoc request, CancellationToken ct)
    {
        if (await db.MonHocs.AnyAsync(x => x.MaMonHoc == request.MaMonHoc, ct)) return Conflict(new { message = "Mã môn học đã tồn tại." });
        db.MonHocs.Add(request); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetMonHoc), new { maMonHoc = request.MaMonHoc }, request);
    }

    [HttpGet("lop-hoc-phan/{maLopHocPhan}")]
    [Authorize]
    public async Task<ActionResult<object>> GetLopHocPhan(string maLopHocPhan, CancellationToken ct)
    {
        var lop = await db.LopHocPhans.AsNoTracking().Include(x => x.MonHoc).Include(x => x.GiangVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
        return lop is null ? NotFound() : Ok(lop);
    }

    [HttpPost("lop-hoc-phan/{maLopHocPhan}/sinh-vien")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    public async Task<IActionResult> DangKy(string maLopHocPhan, DangKyRequest request, CancellationToken ct)
    {
        var lop = await db.LopHocPhans.Include(x => x.DanhSachSinhVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
        if (lop is null) return NotFound();
        if (lop.DanhSachSinhVien.Count >= lop.SiSoToiDa) return Conflict(new { message = "Lớp đã đủ sĩ số." });
        if (await db.DanhSachSinhVienLops.AnyAsync(x => x.MaLopHocPhan == maLopHocPhan && x.MaSinhVien == request.MaSinhVien, ct)) return Conflict(new { message = "Sinh viên đã đăng ký lớp này." });
        if (!await db.SinhViens.AnyAsync(x => x.MaSinhVien == request.MaSinhVien, ct)) return NotFound(new { message = "Không tìm thấy sinh viên." });
        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop { MaLopHocPhan = maLopHocPhan, MaSinhVien = request.MaSinhVien, ThoiGianDangKy = DateTime.UtcNow });
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("sinh-vien/{maSinhVien}/lich-hoc")]
[Authorize]
public async Task<ActionResult<object>> LichHoc(string maSinhVien, string? maHocKy, CancellationToken ct)
{
    // Nếu maSinhVien truyền vào là "me" hoặc trống, lấy tự động từ Token
    if (string.Equals(maSinhVien, "me", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(maSinhVien))
    {
        maSinhVien = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? maSinhVien;
    }

    // Truy vấn chỉ những lớp học phần mà sinh viên ĐÃ ĐĂNG KÝ THÀNH CÔNG trong bảng DanhSachSinhVienLop
    var query = db.DanhSachSinhVienLops
        .AsNoTracking()
        .Where(x => x.MaSinhVien == maSinhVien)
        .SelectMany(x => x.LopHocPhan.LichHocs.Select(l => new 
        {
            MaLopHocPhan = x.LopHocPhan.MaLopHocPhan,
            MaHocKy = x.LopHocPhan.MaHocKy,
            TenMonHoc = x.LopHocPhan.MonHoc.TenMonHoc,
            TenGiangVien = x.LopHocPhan.GiangVien.HoTen,
            ThuTrongTuan = l.ThuTrongTuan, 
            TietBatDau = l.TietBatDau,
            TietKetThuc = l.TietKetThuc,
            TenPhong = l.PhongHoc.TenPhong
        }));

    if (!string.IsNullOrWhiteSpace(maHocKy))
    {
        query = query.Where(x => x.MaHocKy == maHocKy);
    }

    var result = await query
        .OrderBy(x => x.ThuTrongTuan)
        .ThenBy(x => x.TietBatDau)
        .ToListAsync(ct);

    return Ok(result);
}


    // ==========================================================
    // 2. CÁC API BỔ SUNG CHO TÍNH NĂNG "ĐĂNG KÝ HỌC PHẦN" CỦA SINH VIÊN
    // ==========================================================

    [HttpGet("dang-ky-hoc-phan/danh-sach")]
    [Authorize(Roles = RoleNames.SinhVien)] 
    public async Task<IActionResult> GetDanhSachMonHocDangKy([FromQuery] string loaiDangKy, CancellationToken ct)
    {
        // Lấy mã sinh viên từ Token bảo mật
        var maSinhVien = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(maSinhVien)) return Unauthorized();

        // Xử lý phân luồng Học mới / Học lại / Cải thiện
        if (loaiDangKy == "moi")
        {
            // TODO: Truy vấn danh sách lớp học phần theo đúng tiến độ của khóa hiện tại
            var lops = await db.LopHocPhans.AsNoTracking()
                .Include(x => x.MonHoc)
                .Include(x => x.GiangVien)
                .Where(x => x.TrangThai == "0") // 0: Đang mở đăng ký
                .ToListAsync(ct);
                
            return Ok(new { Message = "Danh sách học mới", Data = lops });
        }
        else if (loaiDangKy == "lai" || loaiDangKy == "caithien")
        {
            // TODO: Truy vấn danh sách lớp học phần của các KHÓA SAU (ví dụ sinh viên rớt môn của K15 sẽ lấy lớp của K16)
            var lops = await db.LopHocPhans.AsNoTracking()
                .Include(x => x.MonHoc)
                .Include(x => x.GiangVien)
                .Where(x => x.TrangThai == "0") // 0: Đang mở đăng ký
                .ToListAsync(ct);
                
            return Ok(new { Message = "Danh sách học lại/cải thiện theo khóa sau", Data = lops });
        }

        return BadRequest(new { message = "Loại đăng ký không hợp lệ." });
    }

    [HttpPost("dang-ky-hoc-phan/sinh-vien-tu-dang-ky/{maLopHocPhan}")]
    [Authorize(Roles = RoleNames.SinhVien)]
    public async Task<IActionResult> SinhVienTuDangKy(string maLopHocPhan, CancellationToken ct)
    {
        // Lấy mã sinh viên tự động từ JWT Token, sinh viên không thể truyền mã của người khác
        var maSinhVien = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(maSinhVien)) return Unauthorized();

        var lop = await db.LopHocPhans.Include(x => x.DanhSachSinhVien).SingleOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, ct);
        if (lop is null) return NotFound(new { message = "Không tìm thấy lớp học phần." });
        
        // Kiểm tra sĩ số
        if (lop.DanhSachSinhVien.Count >= lop.SiSoToiDa) return Conflict(new { message = "Lớp đã đủ sĩ số." });
        
        // Kiểm tra trùng lặp
        if (await db.DanhSachSinhVienLops.AnyAsync(x => x.MaLopHocPhan == maLopHocPhan && x.MaSinhVien == maSinhVien, ct)) 
            return Conflict(new { message = "Bạn đã đăng ký lớp học phần này rồi." });
            
        // Thực hiện ghi danh
        db.DanhSachSinhVienLops.Add(new DanhSachSinhVienLop 
        { 
            MaLopHocPhan = maLopHocPhan, 
            MaSinhVien = maSinhVien, 
            ThoiGianDangKy = DateTime.UtcNow,
            TrangThaiCanhBao = "0" 
        });
        
        await db.SaveChangesAsync(ct); 
        return Ok(new { message = "Đăng ký học phần thành công!" });
    }
    // ==========================================================
// API DÀNH CHO GIẢNG VIÊN (QUẢN LÝ LỊCH DẠY & LỚP HỌC PHẦN)
// ==========================================================

[HttpGet("giang-vien/lich-giang-day")]
[Authorize(Roles = RoleNames.GiangVien)]
public async Task<IActionResult> GetLichGiangDay(CancellationToken ct)
{
    var maGiangVien = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(maGiangVien)) return Unauthorized();

    var lichDay = await db.LichHocs
        .AsNoTracking()
        .Where(lh => lh.LopHocPhan.MaGiangVien == maGiangVien)
        .Select(lh => new
        {
            MaLopHocPhan = lh.MaLopHocPhan,
            TenMonHoc = lh.LopHocPhan.MonHoc.TenMonHoc,
            ThuTrongTuan = lh.ThuTrongTuan,
            TietBatDau = lh.TietBatDau,
            TietKetThuc = lh.TietKetThuc,
            TenPhong = lh.PhongHoc.TenPhong,
            SiSoHienTai = lh.LopHocPhan.DanhSachSinhVien.Count,
            SiSoToiDa = lh.LopHocPhan.SiSoToiDa
        })
        .OrderBy(x => x.ThuTrongTuan).ThenBy(x => x.TietBatDau)
        .ToListAsync(ct);

    return Ok(lichDay);
}

[HttpGet("giang-vien/lop-hoc-phan/{maLopHocPhan}/chi-tiet")]
[Authorize(Roles = RoleNames.GiangVien)]
public async Task<IActionResult> GetChiTietQuanLyLop(string maLopHocPhan, CancellationToken ct)
{
    var maGiangVien = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    // Lấy thông tin lớp và danh sách sinh viên kèm tình trạng điểm danh
    var lop = await db.LopHocPhans
        .AsNoTracking()
        .Where(l => l.MaLopHocPhan == maLopHocPhan && l.MaGiangVien == maGiangVien)
        .Select(l => new
        {
            l.MaLopHocPhan,
            l.MonHoc.TenMonHoc,
            DanhSachSinhVien = l.DanhSachSinhVien.Select(ds => new
            {
                ds.MaSinhVien,
                ds.SinhVien.HoTen,
                ds.SinhVien.Email,
                ds.TongSoBuoiVang,
                ds.TrangThaiCanhBao
            }).ToList()
        })
        .SingleOrDefaultAsync(ct);

    if (lop is null) return NotFound(new { message = "Lớp học phần không tồn tại hoặc bạn không có quyền truy cập." });

    return Ok(lop);
}
}

public sealed record DangKyRequest(string MaSinhVien);