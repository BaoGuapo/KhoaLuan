using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MonHocController : ControllerBase
{
    private readonly AppDbContext _context;
    public MonHocController(AppDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _context.MonHocs.ToListAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi server: {ex.Message}");
        }
    }

    [HttpGet("GetByNganh")]
    public async Task<IActionResult> GetByNganh(string maNganh)
    {
        var data = await _context.MonHocs
            .Include(m => m.LopHocPhans)
            .Where(m => m.MaNganh == maNganh)
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) 
    {
        var mon = await _context.MonHocs.FindAsync(id);
        if (mon == null) return NotFound();
        return Ok(mon);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMonHoc(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Mã môn học không hợp lệ." });

            string sql = @"
                IF OBJECT_ID('dbo.DanhSachSinhVienLop', 'U') IS NOT NULL
                    DELETE FROM DanhSachSinhVienLop WHERE MaLopHocPhan IN (SELECT MaLopHocPhan FROM LopHocPhan WHERE MaMonHoc = {0});
                IF OBJECT_ID('dbo.LopHocPhan', 'U') IS NOT NULL
                    DELETE FROM LopHocPhan WHERE MaMonHoc = {0};
                IF OBJECT_ID('dbo.MonKeHoachDaoTao', 'U') IS NOT NULL
                    DELETE FROM MonKeHoachDaoTao WHERE MaMonHoc = {0};
                DELETE FROM MonHoc WHERE MaMonHoc = {0};
            ";
            await _context.Database.ExecuteSqlRawAsync(sql, id);
            return Ok(new { message = "Đã xóa môn học thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi hệ thống khi xóa: {ex.Message}" });
        }
    }

    [HttpGet("GetMonHocKeHoach/{maNganh}")]
    public async Task<IActionResult> GetMonHocKeHoach(string maNganh)
    {
        var data = await _context.MonHocs
            .Where(m => m.MaNganh == maNganh)
            .Select(m => new { m.MaMonHoc, m.TenMonHoc, m.SoTinChi, m.LaMonTuChon })
            .ToListAsync();
        return Ok(data);
    }

    // POST: api/MonHoc (Thêm môn học mới)
        [HttpPost]
        public async Task<IActionResult> CreateMonHoc([FromBody] MonHoc model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.MaMonHoc) || string.IsNullOrWhiteSpace(model.TenMonHoc))
                    return BadRequest(new { message = "Vui lòng điền đầy đủ Mã môn học và Tên môn học!" });

                string maMonTrim = model.MaMonHoc.Trim();
                string tenMonTrim = model.TenMonHoc.Trim();

                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    // 1. Kiểm tra trùng Mã môn
                    cmd.CommandText = $"SELECT COUNT(1) FROM MonHoc WHERE LOWER(MaMonHoc) = LOWER('{maMonTrim}')";
                    int countMa = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (countMa > 0)
                        return BadRequest(new { message = $"Mã môn học '{maMonTrim}' đã tồn tại trong hệ thống!" });
                    
                    // 2. Kiểm tra trùng Tên môn
                    cmd.CommandText = $"SELECT COUNT(1) FROM MonHoc WHERE LOWER(TenMonHoc) = LOWER(N'{tenMonTrim}')";
                    int countTen = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (countTen > 0)
                        return BadRequest(new { message = $"Tên môn học '{tenMonTrim}' đã tồn tại trong hệ thống!" });
                }

                // Tự động tính số tín chỉ Lý thuyết & Thực hành để tránh lỗi NULL trong CSDL
                int stcLyThuyet = model.SoTietLyThuyet > 0 ? (model.SoTietLyThuyet / 15) : model.SoTinChi;
                int stcThucHanh = model.SoTietThucHanh > 0 ? (model.SoTietThucHanh / 30) : 0;

                // Thêm dữ liệu với kiểm tra linh hoạt cột trong SQL Server
                string sqlInsert = @"
                    IF COL_LENGTH('dbo.MonHoc', 'SoTinChiLyThuyet') IS NOT NULL AND COL_LENGTH('dbo.MonHoc', 'SoTinChiThucHanh') IS NOT NULL
                    BEGIN
                        INSERT INTO MonHoc (MaMonHoc, TenMonHoc, MaNganh, SoTinChi, SoTietLyThuyet, SoTietThucHanh, SoTinChiLyThuyet, SoTinChiThucHanh, LaMonTuChon) 
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})
                    END
                    ELSE IF COL_LENGTH('dbo.MonHoc', 'SoTinChiLyThuyet') IS NOT NULL
                    BEGIN
                        INSERT INTO MonHoc (MaMonHoc, TenMonHoc, MaNganh, SoTinChi, SoTietLyThuyet, SoTietThucHanh, SoTinChiLyThuyet, LaMonTuChon) 
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {8})
                    END
                    ELSE
                    BEGIN
                        INSERT INTO MonHoc (MaMonHoc, TenMonHoc, MaNganh, SoTinChi, SoTietLyThuyet, SoTietThucHanh, LaMonTuChon) 
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {8})
                    END";

                await _context.Database.ExecuteSqlRawAsync(
                    sqlInsert, 
                    maMonTrim, 
                    tenMonTrim, 
                    model.MaNganh, 
                    model.SoTinChi, 
                    model.SoTietLyThuyet, 
                    model.SoTietThucHanh, 
                    stcLyThuyet, 
                    stcThucHanh, 
                    model.LaMonTuChon
                );

                return Ok(new { message = "Thêm môn học thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server khi thêm môn: {ex.Message}" });
            }
        }

        // PUT: api/MonHoc/{id} (Cập nhật môn học)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMonHoc(string id, [FromBody] MonHoc model)
        {
            try
            {
                string tenMonTrim = model.TenMonHoc.Trim();

                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    // 1. Kiểm tra môn học tồn tại
                    cmd.CommandText = $"SELECT COUNT(1) FROM MonHoc WHERE MaMonHoc = '{id}'";
                    int exists = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (exists == 0) return NotFound(new { message = "Không tìm thấy môn học!" });

                    // 2. Kiểm tra trùng Tên môn học với môn khác
                    cmd.CommandText = $"SELECT COUNT(1) FROM MonHoc WHERE LOWER(TenMonHoc) = LOWER(N'{tenMonTrim}') AND MaMonHoc != '{id}'";
                    int countTen = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (countTen > 0)
                        return BadRequest(new { message = $"Tên môn học '{tenMonTrim}' đã trùng với một môn học khác trong hệ thống!" });
                }

                int stcLyThuyet = model.SoTietLyThuyet > 0 ? (model.SoTietLyThuyet / 15) : model.SoTinChi;
                int stcThucHanh = model.SoTietThucHanh > 0 ? (model.SoTietThucHanh / 30) : 0;

                string sqlUpdate = @"
                    IF COL_LENGTH('dbo.MonHoc', 'SoTinChiLyThuyet') IS NOT NULL AND COL_LENGTH('dbo.MonHoc', 'SoTinChiThucHanh') IS NOT NULL
                    BEGIN
                        UPDATE MonHoc 
                        SET TenMonHoc = {1}, MaNganh = {2}, SoTinChi = {3}, SoTietLyThuyet = {4}, SoTietThucHanh = {5}, SoTinChiLyThuyet = {6}, SoTinChiThucHanh = {7}, LaMonTuChon = {8}
                        WHERE MaMonHoc = {0}
                    END
                    ELSE
                    BEGIN
                        UPDATE MonHoc 
                        SET TenMonHoc = {1}, MaNganh = {2}, SoTinChi = {3}, SoTietLyThuyet = {4}, SoTietThucHanh = {5}, LaMonTuChon = {8}
                        WHERE MaMonHoc = {0}
                    END";
                
                await _context.Database.ExecuteSqlRawAsync(
                    sqlUpdate, 
                    id, 
                    tenMonTrim, 
                    model.MaNganh, 
                    model.SoTinChi, 
                    model.SoTietLyThuyet, 
                    model.SoTietThucHanh, 
                    stcLyThuyet, 
                    stcThucHanh, 
                    model.LaMonTuChon
                );

                return Ok(new { message = "Cập nhật môn học thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server khi cập nhật: {ex.Message}" });
            }
        }
}