namespace QuanLySinhVien.Api.Models.Entities;

public sealed class AttendanceChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MaSinhVien { get; set; } = string.Empty;
    public Guid? PhienDiemDanhId { get; set; }
    public string Purpose { get; set; } = "Attendance";
    public string IssuedTo { get; set; } = string.Empty;
    public string Direction { get; set; } = "left";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public string? FrameHashes { get; set; }
}
