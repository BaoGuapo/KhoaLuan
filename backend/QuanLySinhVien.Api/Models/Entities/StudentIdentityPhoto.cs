namespace QuanLySinhVien.Api.Models.Entities;

// Official portrait supplied by authorized staff, never the student's avatar URL.
public sealed class StudentIdentityPhoto
{
    public string MaSinhVien { get; set; } = string.Empty;
    public string ImageProtected { get; set; } = string.Empty;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public SinhVien SinhVien { get; set; } = null!;
}
