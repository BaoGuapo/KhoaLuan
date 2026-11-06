using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Data;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GiangVienController : ControllerBase
{
    private readonly AppDbContext _context;

    public GiangVienController(AppDbContext context)
    {
        _context = context;
    }

    // 1. ROUTE GỐC: GET /api/GiangVien (TRẢ VỀ TẤT CẢ GIẢNG VIÊN CÓ TRONG CSDL)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.GiangViens
            .Where(g => g.TrangThaiTaiKhoan == true)
            .Select(g => new 
            { 
                maGiangVien = g.MaGiangVien, 
                hoTen = g.HoTen,
                maGV = g.MaGiangVien,
                tenGV = g.HoTen,
                chuyenNganh = g.ChuyenNganh ?? "" // BẮT BUỘC TRẢ VỀ CHUYÊN NGÀNH ĐỂ FRONTEND LỌC
            })
            .ToListAsync();

        return Ok(data);
    }

    // 2. ROUTE LỌC THEO NGÀNH (DÙNG KHI CẦN GỌI LỌC RIÊNG)
    [HttpGet("GetByChuyenNganh/{maNganh}")]
    [HttpGet("TheoNganh/{maNganh}")]
    public async Task<IActionResult> GetByChuyenNganh(string maNganh)
    {
        if (string.IsNullOrWhiteSpace(maNganh)) return Ok(new List<object>());

        var query = _context.GiangViens.Where(g => g.TrangThaiTaiKhoan == true);

        string kw = maNganh.Trim().ToUpper();
        if (kw.Contains("MKT") || kw.Contains("MARKETING"))
        {
            query = query.Where(g => g.ChuyenNganh != null && g.ChuyenNganh.Contains("Marketing"));
        }
        else if (kw.Contains("CNTT") || kw.Contains("IT"))
        {
            query = query.Where(g => g.ChuyenNganh != null && 
                                    (g.ChuyenNganh.Contains("Kỹ thuật phần mềm") || 
                                     g.ChuyenNganh.Contains("Hệ thống thông tin") || 
                                     g.ChuyenNganh.Contains("Khoa học máy tính") || 
                                     g.ChuyenNganh.Contains("Mạng máy tính") || 
                                     g.ChuyenNganh.Contains("Trí tuệ nhân tạo") ||
                                     g.ChuyenNganh.Contains("DEMO-CNTT")));
        }

        var data = await query
            .Select(g => new 
            { 
                maGiangVien = g.MaGiangVien, 
                hoTen = g.HoTen,
                maGV = g.MaGiangVien,
                tenGV = g.HoTen,
                chuyenNganh = g.ChuyenNganh ?? ""
            })
            .ToListAsync();

        return Ok(data);
    }
}