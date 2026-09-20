using Microsoft.EntityFrameworkCore;
using QuanLySinhVien.Api.Models.Entities;

namespace QuanLySinhVien.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SinhVien> SinhViens => Set<SinhVien>();
    public DbSet<GiangVien> GiangViens => Set<GiangVien>();
    public DbSet<MonHoc> MonHocs => Set<MonHoc>();
    public DbSet<HocKyThucTe> HocKys => Set<HocKyThucTe>();
    public DbSet<PhongHoc> PhongHocs => Set<PhongHoc>();
    public DbSet<LopHocPhan> LopHocPhans => Set<LopHocPhan>();
    public DbSet<DanhSachSinhVienLop> DanhSachSinhVienLops => Set<DanhSachSinhVienLop>();
    public DbSet<LichHoc> LichHocs => Set<LichHoc>();
    public DbSet<BuoiHoc> BuoiHocs => Set<BuoiHoc>();
    public DbSet<ChiTietDiemDanh> ChiTietDiemDanhs => Set<ChiTietDiemDanh>();
    public DbSet<HoSoKhuonMat> HoSoKhuonMats => Set<HoSoKhuonMat>();
    public DbSet<PhienDiemDanh> PhienDiemDanhs => Set<PhienDiemDanh>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new Role { Id = 1, Name = RoleNames.Admin, Description = "Quản trị hệ thống" },
                new Role { Id = 2, Name = RoleNames.CanBoQuanLy, Description = "Cán bộ quản lý" },
                new Role { Id = 3, Name = RoleNames.GiangVien, Description = "Giảng viên" },
                new Role { Id = 4, Name = RoleNames.SinhVien, Description = "Sinh viên" });
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<SinhVien>(e => { e.ToTable("SinhVien"); e.HasKey(x => x.MaSinhVien); e.Property(x => x.MaSinhVien).HasMaxLength(30); e.Property(x => x.HoTen).HasMaxLength(200); e.Property(x => x.Email).HasMaxLength(256); });
        modelBuilder.Entity<GiangVien>(e => { e.ToTable("GiangVien"); e.HasKey(x => x.MaGiangVien); e.Property(x => x.MaGiangVien).HasMaxLength(30); e.Property(x => x.HoTen).HasMaxLength(200); });
        modelBuilder.Entity<MonHoc>(e => { e.ToTable("MonHoc"); e.HasKey(x => x.MaMonHoc); e.Property(x => x.MaMonHoc).HasMaxLength(30); e.Property(x => x.TenMonHoc).HasMaxLength(200); });
        modelBuilder.Entity<HocKyThucTe>(e => { e.ToTable("HocKyThucTe"); e.HasKey(x => x.MaHocKy); e.Property(x => x.MaHocKy).HasMaxLength(30); });
        modelBuilder.Entity<PhongHoc>(e => { e.ToTable("PhongHoc"); e.HasKey(x => x.MaPhong); e.Property(x => x.MaPhong).HasMaxLength(30); });
        modelBuilder.Entity<LopHocPhan>(e =>
        {
            e.ToTable("LopHocPhan"); e.HasKey(x => x.MaLopHocPhan); e.Property(x => x.MaLopHocPhan).HasMaxLength(30);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.MaGiangVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DanhSachSinhVienLop>(e => { e.ToTable("DanhSachSinhVienLop"); e.HasKey(x => new { x.MaSinhVien, x.MaLopHocPhan }); e.HasOne(x => x.SinhVien).WithMany(x => x.DangKyLop).HasForeignKey(x => x.MaSinhVien); e.HasOne(x => x.LopHocPhan).WithMany(x => x.DanhSachSinhVien).HasForeignKey(x => x.MaLopHocPhan); });
        modelBuilder.Entity<LichHoc>(e => { e.ToTable("LichHoc"); e.HasKey(x => x.MaLichHoc); e.HasOne(x => x.LopHocPhan).WithMany(x => x.LichHocs).HasForeignKey(x => x.MaLopHocPhan); e.HasOne(x => x.PhongHoc).WithMany().HasForeignKey(x => x.MaPhong); });
        modelBuilder.Entity<BuoiHoc>(e => { e.ToTable("BuoiHoc"); e.HasKey(x => x.MaBuoiHoc); e.HasOne(x => x.LichHoc).WithMany(x => x.BuoiHocs).HasForeignKey(x => x.MaLichHoc); });
        modelBuilder.Entity<ChiTietDiemDanh>(e => { e.ToTable("ChiTietDiemDanh"); e.HasKey(x => new { x.MaBuoiHoc, x.MaSinhVien }); e.HasOne(x => x.BuoiHoc).WithMany(x => x.ChiTietDiemDanhs).HasForeignKey(x => x.MaBuoiHoc); e.HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.MaSinhVien); });
        modelBuilder.Entity<HoSoKhuonMat>(e => { e.ToTable("HoSoKhuonMat"); e.HasKey(x => x.MaHoSo); e.HasOne(x => x.SinhVien).WithOne(x => x.HoSoKhuonMat).HasForeignKey<HoSoKhuonMat>(x => x.MaSinhVien); });
        modelBuilder.Entity<PhienDiemDanh>(e => { e.ToTable("PhienDiemDanh"); e.HasKey(x => x.Id); e.HasIndex(x => x.QrTokenHash).IsUnique(); e.HasOne(x => x.BuoiHoc).WithMany().HasForeignKey(x => x.MaBuoiHoc); });
        modelBuilder.Entity<ThongBao>(e => { e.ToTable("ThongBao"); e.HasKey(x => x.Id); e.Property(x => x.TieuDe).HasMaxLength(250); });
    }
}
