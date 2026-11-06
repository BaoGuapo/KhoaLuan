using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LopHocPhanController : ControllerBase
{
    private readonly AppDbContext _context;

    public LopHocPhanController(AppDbContext context)
    {
        _context = context;
    }

    // ========================================================
    // 1. API Lấy danh sách nhóm lớp của 1 môn học
    // ========================================================
    [HttpGet("GetByMon")]
    public async Task<IActionResult> GetByMon([FromQuery] string maMon)
    {
        var data = await _context.LopHocPhans
            .Where(x => x.MaMonHoc == maMon)
            .OrderBy(x => x.MaLopHocPhan)
            .Select(x => new { x.MaLopHocPhan, x.SiSoToiDa,x.MaGiangVien, x.TrangThai })
            .ToListAsync();
        
        return Ok(data);
    }

    // ========================================================
    // 2. API Lấy danh sách Lớp học phần theo Ngành & Học kỳ
    // ========================================================
    [HttpGet("TheoNganhVaHocKy")]
    public async Task<IActionResult> GetTheoNganhVaHocKy([FromQuery] string maNganh, [FromQuery] string? hocKy)
    {
        try
        {
            var query = _context.LopHocPhans
                .Include(x => x.MonHoc)
                .Include(x => x.GiangVien)
                .AsQueryable();

            if (!string.IsNullOrEmpty(maNganh))
            {
                query = query.Where(x => x.MonHoc != null && x.MonHoc.MaNganh == maNganh);
            }

            var data = await query
                .OrderBy(x => x.MaLopHocPhan)
                .Select(x => new
                {
                    maLopHocPhan = x.MaLopHocPhan,
                    maMonHoc = x.MaMonHoc,
                    tenMonHoc = x.MonHoc != null ? x.MonHoc.TenMonHoc : "",
                    soTC = x.MonHoc != null ? x.MonHoc.SoTinChi : 0,
                    maGiangVien = x.MaGiangVien,
                    tenGiangVien = (x.GiangVien != null && x.GiangVien.MaGiangVien != "GV_DEFAULT") 
                                    ? x.GiangVien.HoTen 
                                    : "Chưa phân công"
                })
                .ToListAsync();

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách lớp học phần: {ex.Message}" });
        }
    }

    // ========================================================
    // 3. API Tạo nhanh hàng loạt nhóm lớp
    // ========================================================
    [HttpPost("TaoHangLoat")]
    public async Task<IActionResult> TaoHangLoat([FromBody] TaoNhanhRequest request)
    {
        if (string.IsNullOrEmpty(request.MaMonHoc) || request.SoLuong <= 0)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        var monHoc = await _context.MonHocs.FindAsync(request.MaMonHoc);
        if (monHoc == null) 
            return NotFound(new { message = "Không tìm thấy môn học." });

        var giangVienMacDinh = await _context.GiangViens.FindAsync("GV_DEFAULT");
        if (giangVienMacDinh == null)
        {
            giangVienMacDinh = new GiangVien
            {
                MaGiangVien = "GV_DEFAULT",
                HoTen = "Chưa phân công",
                TrangThaiTaiKhoan = true,
                MatKhauLegacy = "123456"
            };
            _context.GiangViens.Add(giangVienMacDinh);
            await _context.SaveChangesAsync();
        }

        var cacNhomHienTai = await _context.LopHocPhans
            .Where(x => x.MaMonHoc == request.MaMonHoc)
            .ToListAsync();

        int maxStt = 0;
        foreach(var nhom in cacNhomHienTai)
        {
            var parts = nhom.MaLopHocPhan.Split("_N");
            if (parts.Length > 1 && int.TryParse(parts.Last(), out int stt))
            {
                if (stt > maxStt) maxStt = stt;
            }
        }

        var danhSachTaoMoi = new List<LopHocPhan>();
        for (int i = 1; i <= request.SoLuong; i++)
        {
            int sttMoi = maxStt + i;
            string maNhomMoi = $"{request.MaMonHoc}_N{sttMoi:D2}"; 

            danhSachTaoMoi.Add(new LopHocPhan
            {
                MaLopHocPhan = maNhomMoi,
                MaMonHoc = request.MaMonHoc,
                SiSoToiDa = 70,     
                TrangThai = "0",    
                MaGiangVien = "GV_DEFAULT" 
            });
        }

        _context.LopHocPhans.AddRange(danhSachTaoMoi);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã tạo {request.SoLuong} nhóm lớp thành công!" });
    }

    // ========================================================
    // 4. API Xóa 1 nhóm lớp
    // ========================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var nhom = await _context.LopHocPhans.FindAsync(id);
        if (nhom == null) return NotFound();

        _context.LopHocPhans.Remove(nhom);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // ========================================================
    // API Lấy toàn bộ nhóm lớp & thời khóa biểu từ CSDL
    // ========================================================
    [HttpGet("LietKeToanBo")]
    public async Task<IActionResult> LietKeToanBo()
    {
        try
        {
            string ensureColsSql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LopHocPhan') AND name = 'LoaiPhong') ALTER TABLE LopHocPhan ADD LoaiPhong NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LopHocPhan') AND name = 'SoBuoi') ALTER TABLE LopHocPhan ADD SoBuoi INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LopHocPhan') AND name = 'NgayBatDau') ALTER TABLE LopHocPhan ADD NgayBatDau NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LopHocPhan') AND name = 'NgayKetThuc') ALTER TABLE LopHocPhan ADD NgayKetThuc NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LopHocPhan') AND name = 'LichHoc') ALTER TABLE LopHocPhan ADD LichHoc NVARCHAR(MAX) NULL;";
            
            await _context.Database.ExecuteSqlRawAsync(ensureColsSql);

            var data = await _context.LopHocPhans
                .GroupJoin(_context.MonHocs, l => l.MaMonHoc, m => m.MaMonHoc, (l, m) => new { l, m })
                .SelectMany(x => x.m.DefaultIfEmpty(), (x, m) => new { x.l, m })
                .GroupJoin(_context.GiangViens, temp => temp.l.MaGiangVien, g => g.MaGiangVien, (temp, g) => new { temp.l, temp.m, g })
                .SelectMany(y => y.g.DefaultIfEmpty(), (y, g) => new 
                {
                    maLopHocPhan = y.l.MaLopHocPhan,
                    maMonHoc = y.l.MaMonHoc,
                    tenMonHoc = y.m != null ? y.m.TenMonHoc : "Chưa xác định",
                    soTC = y.m != null ? y.m.SoTinChi : 3,
                    siSoToiDa = y.l.SiSoToiDa > 0 ? y.l.SiSoToiDa : 70, // Đọc Sĩ số tối đa từ CSDL
                    maGiangVien = y.l.MaGiangVien,
                    tenGiangVien = (g != null && g.MaGiangVien != "GV_DEFAULT") ? g.HoTen : "Chưa phân công",
                    trangThai = y.l.TrangThai,
                    loaiPhong = y.l.LoaiPhong,
                    soBuoi = y.l.SoBuoi,
                    ngayBatDau = y.l.NgayBatDau,
                    ngayKetThuc = y.l.NgayKetThuc,
                    lichHoc = y.l.LichHoc
                })
                .ToListAsync();

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi CSDL: {ex.Message}" });
        }
    }

    // ========================================================
    // API Phân công giảng viên (UPSERT: Đã bổ sung MaHocKy tránh lỗi NOT NULL)
    // ========================================================
    [HttpPost("PhanCongGiangVien")]
    public async Task<IActionResult> PhanCongGiangVien([FromBody] List<PhanCongRequest> danhSachPhanCong)
    {
        try 
        {
            if (danhSachPhanCong == null || danhSachPhanCong.Count == 0)
                return BadRequest(new { message = "Danh sách phân công trống." });

            foreach (var item in danhSachPhanCong) 
            {
                string maGV = string.IsNullOrEmpty(item.MaGiangVien) ? "GV_DEFAULT" : item.MaGiangVien;
                string maMon = item.MaMonHoc;
                string maHk = string.IsNullOrEmpty(item.MaHocKy) ? "HK1" : item.MaHocKy; // Mặc định HK1 nếu trống
                
                if (string.IsNullOrEmpty(maMon) && item.MaLopHocPhan.Contains("_N"))
                {
                    maMon = item.MaLopHocPhan.Split("_N")[0];
                }

                // Thêm MaHocKy vào câu lệnh INSERT ({3})
                string sql = @"
                    IF EXISTS (SELECT 1 FROM LopHocPhan WHERE MaLopHocPhan = {0})
                        UPDATE LopHocPhan SET MaGiangVien = {1} WHERE MaLopHocPhan = {0};
                    ELSE
                        INSERT INTO LopHocPhan (MaLopHocPhan, MaMonHoc, MaGiangVien, SiSoToiDa, TrangThai, MaHocKy)
                        VALUES ({0}, {2}, {1}, 70, '0', {3});";

                await _context.Database.ExecuteSqlRawAsync(sql, item.MaLopHocPhan, maGV, maMon, maHk);
            }
            
            return Ok(new { message = "Lưu phân công giảng viên thành công!" });
        } 
        catch (Exception ex) 
        {
            return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }
        // Thêm API này vào GiangVienController.cs hoặc LopHocPhanController.cs
        [HttpGet("GetByNganh")]
        public async Task<IActionResult> GetByNganh([FromQuery] string maNganh)
        {
            try
                {
                
        // Lọc các giảng viên thuộc mã ngành (Ví dụ: mã giảng viên chứa "CNTT" hoặc trường MaNganh khớp)
        var danhSachGV = await _context.GiangViens
            .Where(g => g.MaGiangVien.Contains(maNganh) || g.MaGiangVien == "GV_DEFAULT")
            .ToListAsync();

        return Ok(danhSachGV);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
    }
}
    // ========================================================
    // API Lưu lịch phân công nhóm lớp (Xử lý an toàn CSDL)
    // ========================================================
    [HttpPost("LuuLichNhom")]
    public async Task<IActionResult> LuuLichNhom([FromBody] LopHocPhanRequest req)
    {
        if (req == null || string.IsNullOrEmpty(req.MaLopHocPhan))
            return BadRequest(new { message = "Dữ liệu phân công không hợp lệ." });

        try
        {
            string lichHocStr = string.Join(",", req.DanhSachCardIds ?? new List<string>());
            string maHocKyChon = string.IsNullOrEmpty(req.MaHocKy) ? "HK1" : req.MaHocKy;
            string maGV = string.IsNullOrEmpty(req.MaGiangVien) ? "GV_DEFAULT" : req.MaGiangVien;

            // Sử dụng SQL Raw để UPSERT giúp tránh triệt để lỗi sinh cột tự động của EF Core
            string sql = @"
                IF EXISTS (SELECT 1 FROM LopHocPhan WHERE MaLopHocPhan = {0})
                BEGIN
                    UPDATE LopHocPhan 
                    SET MaMonHoc = {1},
                        MaGiangVien = {2},
                        SiSoToiDa = {3},
                        LoaiPhong = {4},
                        SoBuoi = {5},
                        NgayBatDau = {6},
                        NgayKetThuc = {7},
                        LichHoc = {8},
                        TrangThai = '1',
                        MaHocKy = {9}
                    WHERE MaLopHocPhan = {0};
                END
                ELSE
                BEGIN
                    INSERT INTO LopHocPhan (MaLopHocPhan, MaMonHoc, MaGiangVien, SiSoToiDa, LoaiPhong, SoBuoi, NgayBatDau, NgayKetThuc, LichHoc, TrangThai, MaHocKy)
                    VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, '1', {9});
                END";

            await _context.Database.ExecuteSqlRawAsync(sql, 
                req.MaLopHocPhan, 
                req.MaMonHoc, 
                maGV, 
                req.SiSoToiDa, 
                req.LoaiPhong ?? "LT", 
                req.SoBuoi, 
                req.NgayBatDau ?? "", 
                req.NgayKetThuc ?? "", 
                lichHocStr, 
                maHocKyChon);

            return Ok(new { message = "Lưu phân công thành công vào CSDL SQL Server!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi CSDL khi lưu lịch nhóm: {ex.Message}" });
        }
    }
    [HttpGet("GetDanhSachDaPhanCong")]
    public async Task<IActionResult> GetDanhSachDaPhanCong()
    {
        try
        {
            var result = await (from lhp in _context.Set<LopHocPhan>()
                                join gv in _context.Set<GiangVien>() on lhp.MaGiangVien equals gv.MaGiangVien into gvGroup
                                from gv in gvGroup.DefaultIfEmpty()
                                join m in _context.Set<MonHoc>() on lhp.MaMonHoc equals m.MaMonHoc into mGroup
                                from m in mGroup.DefaultIfEmpty()
                                select new
                                {
                                    maLopHocPhan = lhp.MaLopHocPhan,
                                    maMonHoc = lhp.MaMonHoc,
                                    tenMonHoc = m != null ? m.TenMonHoc : lhp.MaMonHoc, // Lấy tên môn thực tế từ DB
                                    maGiangVien = lhp.MaGiangVien,
                                    tenGiangVien = gv != null ? gv.HoTen : "Chưa phân công",
                                    siSoToiDa = lhp.SiSoToiDa,
                                    loaiPhong = lhp.LoaiPhong ?? "LT",
                                    soBuoi = lhp.SoBuoi ?? 1,
                                    ngayBatDau = lhp.NgayBatDau,
                                    ngayKetThuc = lhp.NgayKetThuc,
                                    maHocKy = lhp.MaHocKy ?? "",
                                    trangThai = lhp.TrangThai,
                                    danhSachCardIds = !string.IsNullOrEmpty(lhp.LichHoc) 
                                                      ? lhp.LichHoc.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() 
                                                      : new List<string>()
                                }).ToListAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách phân công: {ex.Message}" });
        }
    }
    [HttpPut("{maLopHocPhan}/trang-thai-dang-ky")]
    public async Task<IActionResult> CapNhatTrangThaiDangKy(string maLopHocPhan, [FromBody] UpdateTrangThaiRequest request)
    {
        try
        {
            string newStatus = request.IsOpened ? "2" : "1";
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE LopHocPhan SET TrangThai = {0} WHERE MaLopHocPhan = {1}", 
                newStatus, maLopHocPhan);

            return Ok(new { message = "Cập nhật trạng thái mở đăng ký thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi CSDL: {ex.Message}" });
        }
    }

    // API Mở đăng ký tất cả các lớp đang chọn
    [HttpPost("MoDangKyTatCa")]
    public async Task<IActionResult> MoDangKyTatCa([FromBody] List<string> danhSachMaLop)
    {
        try
        {
            if (danhSachMaLop == null || danhSachMaLop.Count == 0)
                return BadRequest(new { message = "Danh sách lớp rỗng." });

            foreach (var maLop in danhSachMaLop)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE LopHocPhan SET TrangThai = '2' WHERE MaLopHocPhan = {0}", maLop);
            }

            return Ok(new { message = "Đã mở cổng đăng ký cho tất cả các lớp học phần!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi CSDL: {ex.Message}" });
        }
    }
}

public class TaoNhanhRequest
{
    public string MaMonHoc { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}

public class PhanCongRequest
{
    public string MaMonHoc { get; set; } = string.Empty;
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string MaGiangVien { get; set; } = string.Empty;
    public string MaHocKy { get; set; } = "HK1";
}
public class LuuLichRequest
{
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string LoaiPhong { get; set; } = string.Empty;
    public int SoBuoi { get; set; }
    public int SiSoToiDa { get; set; } = 70;
    public string NgayBatDau { get; set; } = string.Empty;
    public string NgayKetThuc { get; set; } = string.Empty;
    public List<string> DanhSachCardIds { get; set; } = new List<string>();
}
public class LopHocPhanRequest
{
    public string MaLopHocPhan { get; set; } = "";
    public string MaMonHoc { get; set; } = "";
    public string MaGiangVien { get; set; } = "";
    public string TenGiangVien { get; set; } = "";
    public string MaHocKy { get; set; } = "HK1";
    public int SiSoToiDa { get; set; } = 70;
    public string LoaiPhong { get; set; } = "LT";
    public int SoBuoi { get; set; } = 1;
    public string NgayBatDau { get; set; } = "";
    public string NgayKetThuc { get; set; } = "";
    public List<string>? DanhSachCardIds { get; set; }
}
public class UpdateTrangThaiRequest
{
    public bool IsOpened { get; set; }
}