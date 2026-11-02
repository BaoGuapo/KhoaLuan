using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class ThemLichSuSuaDiemDanh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LichSuSuaDiemDanh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaBuoiHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    TrangThaiCu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrangThaiMoi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NguoiSua = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SuaLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuSuaDiemDanh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuSuaDiemDanh_BuoiHoc_MaBuoiHoc",
                        column: x => x.MaBuoiHoc,
                        principalTable: "BuoiHoc",
                        principalColumn: "MaBuoiHoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuSuaDiemDanh_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuSuaDiemDanh_MaBuoiHoc_SuaLucUtc",
                table: "LichSuSuaDiemDanh",
                columns: new[] { "MaBuoiHoc", "SuaLucUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuSuaDiemDanh_MaSinhVien",
                table: "LichSuSuaDiemDanh",
                column: "MaSinhVien");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuSuaDiemDanh");
        }
    }
}
