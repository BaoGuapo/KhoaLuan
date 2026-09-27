using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAcademicProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "SinhVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BacDaoTao",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChuyenNganh",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoSo",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanToc",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChiLienHe",
                table: "SinhVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DienChinhSach",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoiTuong",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GioiTinh",
                table: "SinhVien",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoKhauThuongTru",
                table: "SinhVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Khoa",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KhoaHoc",
                table: "SinhVien",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KhuVuc",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiHinhDaoTao",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LopHoc",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaHoSo",
                table: "SinhVien",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nganh",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapCCCD",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayVaoDang",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayVaoDoan",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayVaoTruong",
                table: "SinhVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiCapCCCD",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiSinh",
                table: "SinhVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuocTich",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoCCCD",
                table: "SinhVien",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TonGiao",
                table: "SinhVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "GiangVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChucDanh",
                table: "GiangVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChuyenNganh",
                table: "GiangVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoSo",
                table: "GiangVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanToc",
                table: "GiangVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChiLienHe",
                table: "GiangVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "GiangVien",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GioiTinh",
                table: "GiangVien",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoKhauThuongTru",
                table: "GiangVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Khoa",
                table: "GiangVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayBatDauCongTac",
                table: "GiangVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapCCCD",
                table: "GiangVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "GiangVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayVaoDang",
                table: "GiangVien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiCapCCCD",
                table: "GiangVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiSinh",
                table: "GiangVien",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuocTich",
                table: "GiangVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoCCCD",
                table: "GiangVien",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "GiangVien",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TonGiao",
                table: "GiangVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrinhDoHocVan",
                table: "GiangVien",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "BacDaoTao",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "ChuyenNganh",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "CoSo",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "DanToc",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "DiaChiLienHe",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "DienChinhSach",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "DoiTuong",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "GioiTinh",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "HoKhauThuongTru",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "Khoa",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "KhoaHoc",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "KhuVuc",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "LoaiHinhDaoTao",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "LopHoc",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "MaHoSo",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "Nganh",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgayCapCCCD",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgayVaoDang",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgayVaoDoan",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NgayVaoTruong",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NoiCapCCCD",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "NoiSinh",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "QuocTich",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "SoCCCD",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "TonGiao",
                table: "SinhVien");

            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "ChucDanh",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "ChuyenNganh",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "CoSo",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "DanToc",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "DiaChiLienHe",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "GioiTinh",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "HoKhauThuongTru",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "Khoa",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgayBatDauCongTac",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgayCapCCCD",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NgayVaoDang",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NoiCapCCCD",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "NoiSinh",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "QuocTich",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "SoCCCD",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "TonGiao",
                table: "GiangVien");

            migrationBuilder.DropColumn(
                name: "TrinhDoHocVan",
                table: "GiangVien");
        }
    }
}
