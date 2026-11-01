using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <summary>
    /// Reconciles expanded academic profile columns with the legacy Azure SQL
    /// database. Some environments received these columns manually before EF
    /// migration tracking, so every ALTER is intentionally conditional.
    /// </summary>
    public partial class ExpandAcademicProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'[SinhVien]', N'AnhDaiDien') IS NULL ALTER TABLE [SinhVien] ADD [AnhDaiDien] nvarchar(500) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'BacDaoTao') IS NULL ALTER TABLE [SinhVien] ADD [BacDaoTao] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'ChuyenNganh') IS NULL ALTER TABLE [SinhVien] ADD [ChuyenNganh] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'CoSo') IS NULL ALTER TABLE [SinhVien] ADD [CoSo] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'DanToc') IS NULL ALTER TABLE [SinhVien] ADD [DanToc] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'DiaChiLienHe') IS NULL ALTER TABLE [SinhVien] ADD [DiaChiLienHe] nvarchar(500) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'DienChinhSach') IS NULL ALTER TABLE [SinhVien] ADD [DienChinhSach] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'DoiTuong') IS NULL ALTER TABLE [SinhVien] ADD [DoiTuong] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'GioiTinh') IS NULL ALTER TABLE [SinhVien] ADD [GioiTinh] nvarchar(20) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'HoKhauThuongTru') IS NULL ALTER TABLE [SinhVien] ADD [HoKhauThuongTru] nvarchar(500) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'Khoa') IS NULL ALTER TABLE [SinhVien] ADD [Khoa] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'KhoaHoc') IS NULL ALTER TABLE [SinhVien] ADD [KhoaHoc] nvarchar(50) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'KhuVuc') IS NULL ALTER TABLE [SinhVien] ADD [KhuVuc] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'LoaiHinhDaoTao') IS NULL ALTER TABLE [SinhVien] ADD [LoaiHinhDaoTao] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'LopHoc') IS NULL ALTER TABLE [SinhVien] ADD [LopHoc] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'MaHoSo') IS NULL ALTER TABLE [SinhVien] ADD [MaHoSo] nvarchar(50) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'Nganh') IS NULL ALTER TABLE [SinhVien] ADD [Nganh] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NgayCapCCCD') IS NULL ALTER TABLE [SinhVien] ADD [NgayCapCCCD] datetime2 NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NgaySinh') IS NULL ALTER TABLE [SinhVien] ADD [NgaySinh] datetime2 NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NgayVaoDang') IS NULL ALTER TABLE [SinhVien] ADD [NgayVaoDang] datetime2 NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NgayVaoDoan') IS NULL ALTER TABLE [SinhVien] ADD [NgayVaoDoan] datetime2 NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NgayVaoTruong') IS NULL ALTER TABLE [SinhVien] ADD [NgayVaoTruong] datetime2 NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NoiCapCCCD') IS NULL ALTER TABLE [SinhVien] ADD [NoiCapCCCD] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'NoiSinh') IS NULL ALTER TABLE [SinhVien] ADD [NoiSinh] nvarchar(200) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'QuocTich') IS NULL ALTER TABLE [SinhVien] ADD [QuocTich] nvarchar(100) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'SoCCCD') IS NULL ALTER TABLE [SinhVien] ADD [SoCCCD] nvarchar(30) NULL;
                IF COL_LENGTH(N'[SinhVien]', N'TonGiao') IS NULL ALTER TABLE [SinhVien] ADD [TonGiao] nvarchar(100) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'AnhDaiDien') IS NULL ALTER TABLE [GiangVien] ADD [AnhDaiDien] nvarchar(500) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'ChucDanh') IS NULL ALTER TABLE [GiangVien] ADD [ChucDanh] nvarchar(100) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'ChuyenNganh') IS NULL ALTER TABLE [GiangVien] ADD [ChuyenNganh] nvarchar(200) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'CoSo') IS NULL ALTER TABLE [GiangVien] ADD [CoSo] nvarchar(200) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'DanToc') IS NULL ALTER TABLE [GiangVien] ADD [DanToc] nvarchar(100) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'DiaChiLienHe') IS NULL ALTER TABLE [GiangVien] ADD [DiaChiLienHe] nvarchar(500) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'Email') IS NULL ALTER TABLE [GiangVien] ADD [Email] nvarchar(256) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'GioiTinh') IS NULL ALTER TABLE [GiangVien] ADD [GioiTinh] nvarchar(20) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'HoKhauThuongTru') IS NULL ALTER TABLE [GiangVien] ADD [HoKhauThuongTru] nvarchar(500) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'Khoa') IS NULL ALTER TABLE [GiangVien] ADD [Khoa] nvarchar(200) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NgayBatDauCongTac') IS NULL ALTER TABLE [GiangVien] ADD [NgayBatDauCongTac] datetime2 NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NgayCapCCCD') IS NULL ALTER TABLE [GiangVien] ADD [NgayCapCCCD] datetime2 NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NgaySinh') IS NULL ALTER TABLE [GiangVien] ADD [NgaySinh] datetime2 NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NgayVaoDang') IS NULL ALTER TABLE [GiangVien] ADD [NgayVaoDang] datetime2 NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NoiCapCCCD') IS NULL ALTER TABLE [GiangVien] ADD [NoiCapCCCD] nvarchar(200) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'NoiSinh') IS NULL ALTER TABLE [GiangVien] ADD [NoiSinh] nvarchar(200) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'QuocTich') IS NULL ALTER TABLE [GiangVien] ADD [QuocTich] nvarchar(100) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'SoCCCD') IS NULL ALTER TABLE [GiangVien] ADD [SoCCCD] nvarchar(30) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'SoDienThoai') IS NULL ALTER TABLE [GiangVien] ADD [SoDienThoai] nvarchar(20) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'TonGiao') IS NULL ALTER TABLE [GiangVien] ADD [TonGiao] nvarchar(100) NULL;
                IF COL_LENGTH(N'[GiangVien]', N'TrinhDoHocVan') IS NULL ALTER TABLE [GiangVien] ADD [TrinhDoHocVan] nvarchar(100) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive: these columns may predate EF migration
            // tracking and must not be removed by a rollback.
        }
    }
}
