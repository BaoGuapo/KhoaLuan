using System.ComponentModel.DataAnnotations;

namespace QuanLySinhVien.Api.Models.Entities;

public class NganhDaoTao
{
    [Key]
    public string MaNganh { get; set; } = string.Empty;
    
    public string TenNganh { get; set; } = string.Empty;
}