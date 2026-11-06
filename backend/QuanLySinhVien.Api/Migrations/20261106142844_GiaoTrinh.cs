using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class GiaoTrinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LaMonTuChon",
                table: "MonHoc",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaNganh",
                table: "MonHoc",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoTietLyThuyet",
                table: "MonHoc",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoTietThucHanh",
                table: "MonHoc",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoTinChi",
                table: "MonHoc",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "MaHocKy",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "LichHoc",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiPhong",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonHocMaMonHoc",
                table: "LopHocPhan",
                type: "nvarchar(30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NgayBatDau",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NgayKetThuc",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoBuoi",
                table: "LopHocPhan",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GiaoTrinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaMonHoc = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    TenFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuongDanFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoTrinh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiaoTrinh_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NganhDaoTaos",
                columns: table => new
                {
                    MaNganh = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenNganh = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NganhDaoTaos", x => x.MaNganh);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MonHocMaMonHoc",
                table: "LopHocPhan",
                column: "MonHocMaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoTrinh_MaMonHoc",
                table: "GiaoTrinh",
                column: "MaMonHoc");

            migrationBuilder.AddForeignKey(
                name: "FK_LopHocPhan_MonHoc_MonHocMaMonHoc",
                table: "LopHocPhan",
                column: "MonHocMaMonHoc",
                principalTable: "MonHoc",
                principalColumn: "MaMonHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LopHocPhan_MonHoc_MonHocMaMonHoc",
                table: "LopHocPhan");

            migrationBuilder.DropTable(
                name: "GiaoTrinh");

            migrationBuilder.DropTable(
                name: "NganhDaoTaos");

            migrationBuilder.DropIndex(
                name: "IX_LopHocPhan_MonHocMaMonHoc",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "LaMonTuChon",
                table: "MonHoc");

            migrationBuilder.DropColumn(
                name: "MaNganh",
                table: "MonHoc");

            migrationBuilder.DropColumn(
                name: "SoTietLyThuyet",
                table: "MonHoc");

            migrationBuilder.DropColumn(
                name: "SoTietThucHanh",
                table: "MonHoc");

            migrationBuilder.DropColumn(
                name: "SoTinChi",
                table: "MonHoc");

            migrationBuilder.DropColumn(
                name: "LichHoc",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "LoaiPhong",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "MonHocMaMonHoc",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "NgayBatDau",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "NgayKetThuc",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "SoBuoi",
                table: "LopHocPhan");

            migrationBuilder.AlterColumn<string>(
                name: "MaHocKy",
                table: "LopHocPhan",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
