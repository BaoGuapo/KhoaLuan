using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class ThemDiemTongKet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiemTongKet",
                table: "KetQuaHocPhan",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiNhap",
                table: "KetQuaHocPhan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NhapLucUtc",
                table: "KetQuaHocPhan",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiemTongKet",
                table: "KetQuaHocPhan");

            migrationBuilder.DropColumn(
                name: "NguoiNhap",
                table: "KetQuaHocPhan");

            migrationBuilder.DropColumn(
                name: "NhapLucUtc",
                table: "KetQuaHocPhan");
        }
    }
}
