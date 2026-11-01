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
    public DbSet<StudentIdentityPhoto> StudentIdentityPhotos => Set<StudentIdentityPhoto>();
    public DbSet<PhienDiemDanh> PhienDiemDanhs => Set<PhienDiemDanh>();
    public DbSet<AttendanceChallenge> AttendanceChallenges => Set<AttendanceChallenge>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<KeHoachDaoTao> KeHoachDaoTaos => Set<KeHoachDaoTao>();
    public DbSet<MonKeHoachDaoTao> MonKeHoachDaoTaos => Set<MonKeHoachDaoTao>();
    public DbSet<ApDungHocKy> ApDungHocKys => Set<ApDungHocKy>();
    public DbSet<LopHocPhanDuKien> LopHocPhanDuKiens => Set<LopHocPhanDuKien>();
    public DbSet<HoSoHocVu> HoSoHocVus => Set<HoSoHocVu>();
    public DbSet<KetQuaHocPhan> KetQuaHocPhans => Set<KetQuaHocPhan>();
    public DbSet<DieuKienHocPhan> DieuKienHocPhans => Set<DieuKienHocPhan>();
    public DbSet<DotDangKyHocPhan> DotDangKyHocPhans => Set<DotDangKyHocPhan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AttendanceChallenge>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MaSinhVien).HasMaxLength(30);
            e.Property(x => x.Purpose).HasMaxLength(20);
            e.Property(x => x.IssuedTo).HasMaxLength(100);
            e.Property(x => x.Direction).HasMaxLength(5);
            e.Property(x => x.FrameHashes).HasMaxLength(194);
            e.HasIndex(x => new { x.MaSinhVien, x.CreatedAtUtc });
            e.HasOne<SinhVien>().WithMany().HasForeignKey(x => x.MaSinhVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PhienDiemDanh>().WithMany().HasForeignKey(x => x.PhienDiemDanhId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HoSoKhuonMat>().Property(x => x.ApprovedBy).HasMaxLength(100);
        modelBuilder.Entity<HoSoKhuonMat>().Property(x => x.RejectedBy).HasMaxLength(100);
        modelBuilder.Entity<HoSoKhuonMat>().Property(x => x.RejectionReason).HasMaxLength(500);
        modelBuilder.Entity<HoSoKhuonMat>().Property(x => x.ReviewNote).HasMaxLength(500);
        modelBuilder.Entity<StudentIdentityPhoto>(e => {
            e.ToTable("StudentIdentityPhotos"); e.HasKey(x => x.MaSinhVien);
            e.Property(x => x.MaSinhVien).HasMaxLength(30); e.Property(x => x.UpdatedBy).HasMaxLength(100);
            e.HasOne(x => x.SinhVien).WithOne().HasForeignKey<StudentIdentityPhoto>(x => x.MaSinhVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HoSoHocVu>(e =>
        {
            e.ToTable("HoSoHocVu"); e.HasKey(x => x.MaSinhVien);
            e.Property(x => x.MaSinhVien).HasMaxLength(30);
            e.Property(x => x.GpaTichLuy).HasPrecision(3, 2);
            e.HasOne(x => x.SinhVien).WithOne().HasForeignKey<HoSoHocVu>(x => x.MaSinhVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<KetQuaHocPhan>(e =>
        {
            e.ToTable("KetQuaHocPhan"); e.HasKey(x => new { x.MaSinhVien, x.MaMonHoc, x.MaHocKy });
            e.Property(x => x.MaSinhVien).HasMaxLength(30); e.Property(x => x.MaMonHoc).HasMaxLength(30);
            e.Property(x => x.MaHocKy).HasMaxLength(30); e.Property(x => x.DiemChu).HasMaxLength(2);
            e.HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.MaSinhVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MonHoc>().WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HocKy).WithMany().HasForeignKey(x => x.MaHocKy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DieuKienHocPhan>(e =>
        {
            e.ToTable("DieuKienHocPhan"); e.HasKey(x => new { x.KeHoachDaoTaoId, x.MaMonHoc, x.MaMonDieuKien });
            e.Property(x => x.MaMonHoc).HasMaxLength(30); e.Property(x => x.MaMonDieuKien).HasMaxLength(30); e.Property(x => x.Loai).HasMaxLength(12);
            e.HasOne<KeHoachDaoTao>().WithMany().HasForeignKey(x => x.KeHoachDaoTaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MonHoc>().WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MonHoc>().WithMany().HasForeignKey(x => x.MaMonDieuKien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DotDangKyHocPhan>(e =>
        {
            e.ToTable("DotDangKyHocPhan"); e.HasKey(x => x.Id); e.Property(x => x.TenDot).HasMaxLength(200); e.Property(x => x.MaHocKy).HasMaxLength(30);
            e.HasOne(x => x.HocKy).WithMany().HasForeignKey(x => x.MaHocKy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<KeHoachDaoTao>().WithMany().HasForeignKey(x => x.KeHoachDaoTaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.MaHocKy, x.MoLucUtc, x.DongLucUtc });
        });
        modelBuilder.Entity<DanhSachSinhVienLop>().Property(x => x.LoaiDangKy).HasMaxLength(12).HasDefaultValue("moi");
        modelBuilder.Entity<KeHoachDaoTao>(e =>
        {
            e.ToTable("KeHoachDaoTao"); e.HasKey(x => x.Id);
            e.Property(x => x.MaNganh).HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.MaKhoa).HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.TenKhoaDaoTao).HasMaxLength(200);
            e.Property(x => x.TenChuongTrinh).HasMaxLength(200);
            e.HasIndex(x => new { x.MaNganh, x.MaKhoa }).IsUnique();
        });
        modelBuilder.Entity<MonKeHoachDaoTao>(e =>
        {
            e.ToTable("MonKeHoachDaoTao"); e.HasKey(x => new { x.KeHoachDaoTaoId, x.MaMonHoc });
            e.Property(x => x.MaMonHoc).HasMaxLength(50).IsUnicode(false);
            e.HasOne(x => x.KeHoach).WithMany(x => x.MonHocs).HasForeignKey(x => x.KeHoachDaoTaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ApDungHocKy>(e =>
        {
            e.ToTable("ApDungHocKy"); e.HasKey(x => x.Id);
            e.Property(x => x.MaHocKy).HasMaxLength(50).IsUnicode(false);
            e.HasIndex(x => new { x.KeHoachDaoTaoId, x.MaHocKy }).IsUnique();
            e.HasOne(x => x.KeHoach).WithMany().HasForeignKey(x => x.KeHoachDaoTaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HocKyThucTe>().WithMany().HasForeignKey(x => x.MaHocKy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<LopHocPhanDuKien>(e =>
        {
            e.ToTable("LopHocPhanDuKien"); e.HasKey(x => x.MaLopHocPhan);
            e.Property(x => x.MaLopHocPhan).HasMaxLength(30);
            e.Property(x => x.MaMonHoc).HasMaxLength(50).IsUnicode(false);
            e.HasOne(x => x.ApDung).WithMany(x => x.LopHocPhans).HasForeignKey(x => x.ApDungHocKyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
        });

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

        modelBuilder.Entity<SinhVien>(e =>
        {
            e.ToTable("SinhVien");
            e.HasKey(x => x.MaSinhVien);
            e.Property(x => x.MaSinhVien).HasMaxLength(30);
            e.Property(x => x.MatKhauLegacy).HasColumnName("matKhau").HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.HoTen).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.AnhDaiDien).HasMaxLength(500);
            e.Property(x => x.GioiTinh).HasMaxLength(20);
            e.Property(x => x.NoiSinh).HasMaxLength(200);
            e.Property(x => x.DanToc).HasMaxLength(100);
            e.Property(x => x.TonGiao).HasMaxLength(100);
            e.Property(x => x.QuocTich).HasMaxLength(100);
            e.Property(x => x.KhuVuc).HasMaxLength(100);
            e.Property(x => x.SoCCCD).HasMaxLength(30);
            e.Property(x => x.NoiCapCCCD).HasMaxLength(200);
            e.Property(x => x.DiaChiLienHe).HasMaxLength(500);
            e.Property(x => x.HoKhauThuongTru).HasMaxLength(500);
            e.Property(x => x.MaHoSo).HasMaxLength(50);
            e.Property(x => x.LopHoc).HasMaxLength(100);
            e.Property(x => x.CoSo).HasMaxLength(200);
            e.Property(x => x.BacDaoTao).HasMaxLength(100);
            e.Property(x => x.LoaiHinhDaoTao).HasMaxLength(100);
            e.Property(x => x.Khoa).HasMaxLength(200);
            e.Property(x => x.Nganh).HasMaxLength(200);
            e.Property(x => x.ChuyenNganh).HasMaxLength(200);
            e.Property(x => x.KhoaHoc).HasMaxLength(50);
            e.Property(x => x.DoiTuong).HasMaxLength(100);
            e.Property(x => x.DienChinhSach).HasMaxLength(100);
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            e.HasOne<User>().WithOne().HasForeignKey<SinhVien>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<GiangVien>(e =>
        {
            e.ToTable("GiangVien");
            e.HasKey(x => x.MaGiangVien);
            e.Property(x => x.MaGiangVien).HasMaxLength(30);
            e.Property(x => x.MatKhauLegacy).HasColumnName("matKhau").HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.HoTen).HasMaxLength(200);
            e.Property(x => x.SoDienThoai).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.AnhDaiDien).HasMaxLength(500);
            e.Property(x => x.GioiTinh).HasMaxLength(20);
            e.Property(x => x.NoiSinh).HasMaxLength(200);
            e.Property(x => x.DanToc).HasMaxLength(100);
            e.Property(x => x.TonGiao).HasMaxLength(100);
            e.Property(x => x.QuocTich).HasMaxLength(100);
            e.Property(x => x.SoCCCD).HasMaxLength(30);
            e.Property(x => x.NoiCapCCCD).HasMaxLength(200);
            e.Property(x => x.DiaChiLienHe).HasMaxLength(500);
            e.Property(x => x.HoKhauThuongTru).HasMaxLength(500);
            e.Property(x => x.CoSo).HasMaxLength(200);
            e.Property(x => x.Khoa).HasMaxLength(200);
            e.Property(x => x.ChuyenNganh).HasMaxLength(200);
            e.Property(x => x.TrinhDoHocVan).HasMaxLength(100);
            e.Property(x => x.ChucDanh).HasMaxLength(100);
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            e.HasOne<User>().WithOne().HasForeignKey<GiangVien>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<MonHoc>(e => { e.ToTable("MonHoc"); e.HasKey(x => x.MaMonHoc); e.Property(x => x.MaMonHoc).HasMaxLength(30); e.Property(x => x.TenMonHoc).HasMaxLength(200); });
        modelBuilder.Entity<HocKyThucTe>(e =>
        {
            e.ToTable("HocKyThucTe"); e.HasKey(x => x.MaHocKy); e.Property(x => x.MaHocKy).HasMaxLength(30);
            e.Property(x => x.LoaiHocKy).HasConversion<byte>().IsRequired(false);
            e.Property(x => x.NgayBatDau).HasColumnType("date"); e.Property(x => x.NgayKetThuc).HasColumnType("date");
        });
        modelBuilder.Entity<PhongHoc>(e => { e.ToTable("PhongHoc"); e.HasKey(x => x.MaPhong); e.Property(x => x.MaPhong).HasMaxLength(30); });
        modelBuilder.Entity<LopHocPhan>(e =>
        {
            e.ToTable("LopHocPhan"); e.HasKey(x => x.MaLopHocPhan); e.Property(x => x.MaLopHocPhan).HasMaxLength(30);
            e.Property(x => x.TrangThai).HasConversion<byte>().IsRequired(false);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MaMonHoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.MaGiangVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DanhSachSinhVienLop>(e => { e.ToTable("DanhSachSinhVienLop"); e.HasKey(x => new { x.MaSinhVien, x.MaLopHocPhan }); e.HasOne(x => x.SinhVien).WithMany(x => x.DangKyLop).HasForeignKey(x => x.MaSinhVien); e.HasOne(x => x.LopHocPhan).WithMany(x => x.DanhSachSinhVien).HasForeignKey(x => x.MaLopHocPhan);
            e.Property(x => x.TrangThaiCanhBao).HasConversion(x => LegacyAcademicValues.WarningCode(x!), x => LegacyAcademicValues.WarningName(x)); });
        modelBuilder.Entity<LichHoc>(e => { e.ToTable("LichHoc"); e.HasKey(x => x.MaLichHoc); e.HasOne(x => x.LopHocPhan).WithMany(x => x.LichHocs).HasForeignKey(x => x.MaLopHocPhan); e.HasOne(x => x.PhongHoc).WithMany().HasForeignKey(x => x.MaPhong); });
        modelBuilder.Entity<BuoiHoc>(e => { e.ToTable("BuoiHoc"); e.HasKey(x => x.MaBuoiHoc); e.HasOne(x => x.LichHoc).WithMany(x => x.BuoiHocs).HasForeignKey(x => x.MaLichHoc);
            e.Property(x => x.ThoiGianDiemDanh).HasColumnName("thoiGianMoDiemDanh").HasColumnType("datetime");
            e.Property(x => x.TrangThaiBuoiHoc).HasConversion(x => LegacyAcademicValues.SessionCode(x), x => LegacyAcademicValues.SessionName(x)).IsRequired(false); });
        modelBuilder.Entity<ChiTietDiemDanh>(e => { e.ToTable("ChiTietDiemDanh"); e.HasKey(x => new { x.MaBuoiHoc, x.MaSinhVien }); e.Property(x => x.KetQuaViTri).HasMaxLength(20); e.HasOne(x => x.BuoiHoc).WithMany(x => x.ChiTietDiemDanhs).HasForeignKey(x => x.MaBuoiHoc); e.HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.MaSinhVien); });
        modelBuilder.Entity<HoSoKhuonMat>(e => { e.ToTable("HoSoKhuonMat"); e.HasKey(x => x.MaHoSo); e.HasOne(x => x.SinhVien).WithOne(x => x.HoSoKhuonMat).HasForeignKey<HoSoKhuonMat>(x => x.MaSinhVien); });
        modelBuilder.Entity<PhienDiemDanh>(e => { e.ToTable("PhienDiemDanh"); e.HasKey(x => x.Id); e.HasIndex(x => x.QrTokenHash).IsUnique(); e.HasOne(x => x.BuoiHoc).WithMany().HasForeignKey(x => x.MaBuoiHoc); });
        modelBuilder.Entity<ThongBao>(e => { e.ToTable("ThongBao"); e.HasKey(x => x.Id); e.Property(x => x.TieuDe).HasMaxLength(250); });
    }
}
