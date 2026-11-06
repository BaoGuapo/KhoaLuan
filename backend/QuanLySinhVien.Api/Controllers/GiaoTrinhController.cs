using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.CanBoQuanLy)]
public class GiaoTrinhController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public GiaoTrinhController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private async Task EnsureTableCreatedAsync()
    {
        try
        {
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GiaoTrinh')
                BEGIN
                    CREATE TABLE GiaoTrinh (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        MaMonHoc NVARCHAR(50) NOT NULL,
                        TenFile NVARCHAR(255) NOT NULL,
                        DuongDanFile NVARCHAR(500) NOT NULL,
                        LoaiFile NVARCHAR(20) NULL,
                        NgayTao DATETIME DEFAULT GETDATE()
                    );
                END";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
        catch { }
    }

    [HttpPost("LuuHangLoat")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
    public async Task<IActionResult> LuuHangLoat([FromForm] GiaoTrinhUploadRequest model)
    {
        try
        {
            await EnsureTableCreatedAsync();

            if (model == null || model.Files == null || model.Files.Count == 0)
                return BadRequest(new { message = "Không có file giáo trình nào được gửi lên server!" });

            string rootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            string uploadFolder = Path.Combine(rootPath, "uploads", "giaotrinh");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var resultList = new List<GiaoTrinh>();

            for (int i = 0; i < model.Files.Count; i++)
            {
                var file = model.Files[i];
                string maMon = (model.MaMonHocs != null && i < model.MaMonHocs.Count) ? model.MaMonHocs[i] : "";

                if (string.IsNullOrEmpty(maMon) || file == null || file.Length == 0) continue;

                string ext = Path.GetExtension(file.FileName).ToLower();
                string loaiFile = (ext == ".doc" || ext == ".docx") ? "WORD" : (ext == ".ppt" || ext == ".pptx") ? "PPT" : "KHAC";

                string uniqueFileName = $"{maMon}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var entity = new GiaoTrinh
                {
                    MaMonHoc = maMon,
                    TenFile = file.FileName,
                    DuongDanFile = $"/uploads/giaotrinh/{uniqueFileName}",
                    LoaiFile = loaiFile,
                    NgayTao = DateTime.Now
                };

                _context.GiaoTrinhs.Add(entity);
                resultList.Add(entity);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã lưu thành công {resultList.Count} giáo trình vào CSDL!", data = resultList });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi hệ thống khi lưu file: {ex.Message}" });
        }
    }

    [HttpPost("GetByDanhSachMon")]
    public async Task<IActionResult> GetByDanhSachMon([FromBody] List<string> maMonHocs)
    {
        try
        {
            await EnsureTableCreatedAsync();
            if (maMonHocs == null || maMonHocs.Count == 0) return Ok(new List<GiaoTrinh>());

            var data = await _context.GiaoTrinhs
                .Where(x => maMonHocs.Contains(x.MaMonHoc))
                .OrderByDescending(x => x.NgayTao)
                .ToListAsync();

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy dữ liệu giáo trình: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await EnsureTableCreatedAsync();
            var gt = await _context.GiaoTrinhs.FindAsync(id);
            if (gt == null) return NotFound(new { message = "Không tìm thấy giáo trình." });

            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string physicalPath = Path.Combine(rootPath, gt.DuongDanFile.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.GiaoTrinhs.Remove(gt);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa giáo trình thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa giáo trình: {ex.Message}" });
        }
    }
}

public class GiaoTrinhUploadRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public List<string> MaMonHocs { get; set; } = new();
}