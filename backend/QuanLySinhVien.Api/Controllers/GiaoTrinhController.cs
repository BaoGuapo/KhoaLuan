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
    // Chỉ nhận đúng các định dạng mà giao diện cho chọn (Word/PowerPoint).
    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "WORD", [".docx"] = "WORD", [".ppt"] = "PPT", [".pptx"] = "PPT"
    };
    private const long MaxFileBytes = 20 * 1024 * 1024;
    // Website chuyển tiếp upload qua proxy với giới hạn mặc định 30 MB của Kestrel.
    private const long MaxRequestBytes = 30 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GiaoTrinhController> _logger;

    public GiaoTrinhController(AppDbContext context, IWebHostEnvironment env, ILogger<GiaoTrinhController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
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
        catch (Exception ex)
        {
            // Bảng có thể đã được migration tạo sẵn; ghi log để còn lần ra lỗi thật nếu có.
            _logger.LogWarning(ex, "Không kiểm tra/tạo được bảng GiaoTrinh.");
        }
    }

    [HttpPost("LuuHangLoat")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
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

            // Kiểm tra toàn bộ file trước khi ghi, để không lưu dở dang một nửa lô.
            var requestedCodes = (model.MaMonHocs ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
            var existingCodes = (await _context.MonHocs.AsNoTracking()
                    .Where(x => requestedCodes.Contains(x.MaMonHoc))
                    .Select(x => x.MaMonHoc)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var uploads = new List<(IFormFile File, string MaMon, string Ext, string LoaiFile)>();
            for (int i = 0; i < model.Files.Count; i++)
            {
                var file = model.Files[i];
                string maMon = (model.MaMonHocs != null && i < model.MaMonHocs.Count) ? model.MaMonHocs[i]?.Trim() ?? "" : "";

                if (string.IsNullOrEmpty(maMon) || file == null || file.Length == 0) continue;

                // Mã môn được ghép vào tên file nên bắt buộc phải là môn có thật (chặn "../" thoát thư mục).
                if (!existingCodes.Contains(maMon))
                    return BadRequest(new { message = $"Mã môn học '{maMon}' không tồn tại." });

                string ext = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.TryGetValue(ext, out var loaiFile))
                    return BadRequest(new { message = $"File '{Path.GetFileName(file.FileName)}' không hợp lệ. Chỉ nhận .doc, .docx, .ppt, .pptx." });
                if (file.Length > MaxFileBytes)
                    return BadRequest(new { message = $"File '{Path.GetFileName(file.FileName)}' vượt quá {MaxFileBytes / 1024 / 1024} MB." });

                uploads.Add((file, maMon, ext.ToLowerInvariant(), loaiFile));
            }

            var resultList = new List<GiaoTrinh>();
            foreach (var (file, maMon, ext, loaiFile) in uploads)
            {
                string uniqueFileName = $"{maMon}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                // Lớp bảo vệ thứ hai: đường dẫn cuối cùng phải nằm trong thư mục upload.
                if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(uploadFolder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = $"Mã môn học '{maMon}' không hợp lệ." });

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var entity = new GiaoTrinh
                {
                    MaMonHoc = maMon,
                    TenFile = Path.GetFileName(file.FileName),
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
            _logger.LogError(ex, "Lỗi khi lưu giáo trình.");
            return StatusCode(500, new { message = "Lỗi hệ thống khi lưu file giáo trình." });
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
            _logger.LogError(ex, "Lỗi khi lấy danh sách giáo trình.");
            return StatusCode(500, new { message = "Lỗi lấy dữ liệu giáo trình." });
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
            _logger.LogError(ex, "Lỗi khi xóa giáo trình.");
            return StatusCode(500, new { message = "Lỗi xóa giáo trình." });
        }
    }
}

public class GiaoTrinhUploadRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public List<string> MaMonHocs { get; set; } = new();
}