using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;
using System;
using System.Threading.Tasks;

namespace QuanLySinhVien.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
[ApiController]
public class NganhDaoTaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public NganhDaoTaoController(AppDbContext context)
    {
        _context = context;
    }

    // Hàm tự động xác định tên bảng (NganhDaoTao hoặc NganhDaoTaos) & khởi tạo nếu chưa có
    private async Task<string> GetTableNameAsync()
    {
        var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.NganhDaoTao', 'U') IS NOT NULL 
                SELECT 'NganhDaoTao'
            ELSE IF OBJECT_ID('dbo.NganhDaoTaos', 'U') IS NOT NULL 
                SELECT 'NganhDaoTaos'
            ELSE
            BEGIN
                CREATE TABLE NganhDaoTao (
                    MaNganh NVARCHAR(50) PRIMARY KEY,
                    TenNganh NVARCHAR(100) NOT NULL
                );
                SELECT 'NganhDaoTao';
            END";

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "NganhDaoTao";
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            string tableName = await GetTableNameAsync();
            string sql = $"SELECT MaNganh, TenNganh FROM {tableName}";
            var list = await _context.NganhDaoTaos.FromSqlRaw(sql).ToListAsync();
            return Ok(list);
        }
        catch
        {
            // Fallback nếu EF Core DbContext dùng DbSet mặc định
            return Ok(await _context.NganhDaoTaos.ToListAsync());
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            string tableName = await GetTableNameAsync();
            string sql = $"DELETE FROM {tableName} WHERE MaNganh = {{0}}";
            await _context.Database.ExecuteSqlRawAsync(sql, id.Trim());
            return Ok(new { message = "Đã xóa ngành thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa ngành: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateNganh([FromBody] NganhDaoTao model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.MaNganh) || string.IsNullOrWhiteSpace(model.TenNganh))
                return BadRequest(new { message = "Vui lòng điền đầy đủ Mã ngành và Tên ngành!" });

            string maNganhTrim = model.MaNganh.Trim();
            string tenNganhTrim = model.TenNganh.Trim();

            string tableName = await GetTableNameAsync();

            var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            
            using (var cmd = conn.CreateCommand())
            {
                // 1. Kiểm tra trùng Mã ngành
                cmd.CommandText = $"SELECT COUNT(1) FROM {tableName} WHERE LOWER(MaNganh) = LOWER('{maNganhTrim}')";
                int countMa = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (countMa > 0)
                    return BadRequest(new { message = $"Mã ngành '{maNganhTrim}' đã tồn tại trong hệ thống!" });
                
                // 2. Kiểm tra trùng Tên ngành
                cmd.CommandText = $"SELECT COUNT(1) FROM {tableName} WHERE LOWER(TenNganh) = LOWER(N'{tenNganhTrim}')";
                int countTen = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (countTen > 0)
                    return BadRequest(new { message = $"Tên ngành '{tenNganhTrim}' đã tồn tại trong hệ thống!" });
            }

            // Thực thi chèn dữ liệu
            string sqlInsert = $"INSERT INTO {tableName} (MaNganh, TenNganh) VALUES ({{0}}, {{1}})";
            await _context.Database.ExecuteSqlRawAsync(sqlInsert, maNganhTrim, tenNganhTrim);

            return Ok(new { message = "Thêm ngành đào tạo thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
        }
    }

    [HttpPut("{maNganh}")]
    public async Task<IActionResult> UpdateNganh(string maNganh, [FromBody] NganhDaoTao model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.TenNganh))
                return BadRequest(new { message = "Vui lòng nhập Tên ngành!" });

            string tenNganhTrim = model.TenNganh.Trim();
            string tableName = await GetTableNameAsync();

            var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                // 1. Kiểm tra tồn tại ngành cần sửa
                cmd.CommandText = $"SELECT COUNT(1) FROM {tableName} WHERE MaNganh = '{maNganh.Trim()}'";
                int exists = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (exists == 0) return NotFound(new { message = "Không tìm thấy ngành cần sửa!" });

                // 2. Kiểm tra trùng Tên ngành với ngành khác
                cmd.CommandText = $"SELECT COUNT(1) FROM {tableName} WHERE LOWER(TenNganh) = LOWER(N'{tenNganhTrim}') AND MaNganh != '{maNganh.Trim()}'";
                int countTen = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (countTen > 0)
                    return BadRequest(new { message = $"Tên ngành '{tenNganhTrim}' đã được sử dụng cho ngành khác!" });
            }

            string sqlUpdate = $"UPDATE {tableName} SET TenNganh = {{1}} WHERE MaNganh = {{0}}";
            await _context.Database.ExecuteSqlRawAsync(sqlUpdate, maNganh.Trim(), tenNganhTrim);

            return Ok(new { message = "Cập nhật ngành đào tạo thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
        }
    }
}