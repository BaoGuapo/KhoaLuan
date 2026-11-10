using Microsoft.AspNetCore.Authorization;
using QuanLySinhVien.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QuanLySinhVien.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
    [ApiController]
    public class KhoaHocController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KhoaHocController(AppDbContext context)
        {
            _context = context;
        }

        private async Task EnsureTableCreatedAsync()
        {
            string sqlInit = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KhoaHoc')
                BEGIN
                    CREATE TABLE KhoaHoc (
                        MaKhoa NVARCHAR(50) PRIMARY KEY,
                        TenKhoa NVARCHAR(100) NOT NULL,
                        MaNganh NVARCHAR(50) NOT NULL,
                        NamBatDau INT NOT NULL,
                        ThoiGianDaoTao FLOAT NOT NULL
                    );
                END";
            await _context.Database.ExecuteSqlRawAsync(sqlInit);
        }

        // 1. Lấy danh sách Khóa học (Kiểm tra an toàn bảng NganhDaoTao)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                await EnsureTableCreatedAsync();

                var list = new List<KhoaHocDto>();
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    // Truy vấn tự động nhận biết bảng NganhDaoTao nếu chưa tồn tại
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.NganhDaoTao', 'U') IS NOT NULL
                        BEGIN
                            SELECT k.MaKhoa, k.TenKhoa, k.MaNganh, ISNULL(n.TenNganh, k.MaNganh) AS TenNganh, k.NamBatDau, k.ThoiGianDaoTao
                            FROM KhoaHoc k
                            LEFT JOIN NganhDaoTao n ON k.MaNganh = n.MaNganh
                        END
                        ELSE
                        BEGIN
                            SELECT k.MaKhoa, k.TenKhoa, k.MaNganh, k.MaNganh AS TenNganh, k.NamBatDau, k.ThoiGianDaoTao
                            FROM KhoaHoc k
                        END";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new KhoaHocDto
                            {
                                MaKhoa = reader["MaKhoa"]?.ToString() ?? "",
                                TenKhoa = reader["TenKhoa"]?.ToString() ?? "",
                                MaNganh = reader["MaNganh"]?.ToString() ?? "",
                                TenNganh = reader["TenNganh"]?.ToString() ?? "",
                                NamBatDau = reader["NamBatDau"] != DBNull.Value ? Convert.ToInt32(reader["NamBatDau"]) : 2026,
                                ThoiGianDaoTao = reader["ThoiGianDaoTao"] != DBNull.Value ? Convert.ToDouble(reader["ThoiGianDaoTao"]) : 4.0
                            });
                        }
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi CSDL KhoaHoc: {ex.Message}" });
            }
        }

        // 2. Thêm mới Khóa học
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] KhoaHocInput model)
        {
            try
            {
                await EnsureTableCreatedAsync();

                if (string.IsNullOrWhiteSpace(model.MaKhoa) || string.IsNullOrWhiteSpace(model.TenKhoa) || string.IsNullOrWhiteSpace(model.MaNganh))
                    return BadRequest(new { message = "Vui lòng nhập đầy đủ Mã khóa, Tên khóa và Ngành đào tạo!" });

                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.AddParameter("@ma", model.MaKhoa.Trim());
                    cmd.CommandText = "SELECT COUNT(1) FROM KhoaHoc WHERE MaKhoa = @ma";
                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (count > 0)
                        return BadRequest(new { message = $"Mã khóa [{model.MaKhoa}] đã tồn tại!" });
                }

                string sqlInsert = @"
                    INSERT INTO KhoaHoc (MaKhoa, TenKhoa, MaNganh, NamBatDau, ThoiGianDaoTao)
                    VALUES ({0}, {1}, {2}, {3}, {4});";

                await _context.Database.ExecuteSqlRawAsync(sqlInsert, model.MaKhoa.Trim(), model.TenKhoa.Trim(), model.MaNganh.Trim(), model.NamBatDau, model.ThoiGianDaoTao);
                return Ok(new { message = "Thêm khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi tạo khóa học: {ex.Message}" });
            }
        }

        // 3. Cập nhật thông tin Khóa học
        [HttpPut("{maKhoa}")]
        public async Task<IActionResult> Update(string maKhoa, [FromBody] KhoaHocInput model)
        {
            try
            {
                await EnsureTableCreatedAsync();

                string sqlUpdate = @"
                    UPDATE KhoaHoc 
                    SET TenKhoa = {1}, MaNganh = {2}, NamBatDau = {3}, ThoiGianDaoTao = {4}
                    WHERE MaKhoa = {0};";

                await _context.Database.ExecuteSqlRawAsync(sqlUpdate, maKhoa.Trim(), model.TenKhoa.Trim(), model.MaNganh.Trim(), model.NamBatDau, model.ThoiGianDaoTao);
                return Ok(new { message = "Cập nhật khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật: {ex.Message}" });
            }
        }

        // 4. Xóa Khóa học
        [HttpDelete("{maKhoa}")]
        public async Task<IActionResult> Delete(string maKhoa)
        {
            try
            {
                await EnsureTableCreatedAsync();

                string sqlDelete = "DELETE FROM KhoaHoc WHERE MaKhoa = {0};";
                await _context.Database.ExecuteSqlRawAsync(sqlDelete, maKhoa.Trim());
                return Ok(new { message = "Xóa khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi xóa: {ex.Message}" });
            }
        }
    }

    public class KhoaHocDto
    {
        public string MaKhoa { get; set; } = string.Empty;
        public string TenKhoa { get; set; } = string.Empty;
        public string MaNganh { get; set; } = string.Empty;
        public string TenNganh { get; set; } = string.Empty;
        public int NamBatDau { get; set; }
        public double ThoiGianDaoTao { get; set; }
    }

    public class KhoaHocInput
    {
        public string MaKhoa { get; set; } = string.Empty;
        public string TenKhoa { get; set; } = string.Empty;
        public string MaNganh { get; set; } = string.Empty;
        public int NamBatDau { get; set; }
        public double ThoiGianDaoTao { get; set; }
    }
}