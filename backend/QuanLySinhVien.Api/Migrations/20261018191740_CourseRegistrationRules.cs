using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class CourseRegistrationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoaiDangKy",
                table: "DanhSachSinhVienLop",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "moi");

            migrationBuilder.CreateTable(
                name: "DieuKienHocPhan",
                columns: table => new
                {
                    KeHoachDaoTaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaMonHoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaMonDieuKien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuKienHocPhan", x => new { x.KeHoachDaoTaoId, x.MaMonHoc, x.MaMonDieuKien });
                    table.ForeignKey(
                        name: "FK_DieuKienHocPhan_KeHoachDaoTao_KeHoachDaoTaoId",
                        column: x => x.KeHoachDaoTaoId,
                        principalTable: "KeHoachDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DieuKienHocPhan_MonHoc_MaMonDieuKien",
                        column: x => x.MaMonDieuKien,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DieuKienHocPhan_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DotDangKyHocPhan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenDot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaHocKy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KeHoachDaoTaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MoLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DongLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChoPhepDangKy = table.Column<bool>(type: "bit", nullable: false),
                    LaHocKyHe = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotDangKyHocPhan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotDangKyHocPhan_HocKyThucTe_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HocKyThucTe",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DotDangKyHocPhan_KeHoachDaoTao_KeHoachDaoTaoId",
                        column: x => x.KeHoachDaoTaoId,
                        principalTable: "KeHoachDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoSoHocVu",
                columns: table => new
                {
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ThuTuHocKy = table.Column<int>(type: "int", nullable: false),
                    GpaTichLuy = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    LaHocKyDau = table.Column<bool>(type: "bit", nullable: false),
                    LaHocKyCuoi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoHocVu", x => x.MaSinhVien);
                    table.ForeignKey(
                        name: "FK_HoSoHocVu_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaHocPhan",
                columns: table => new
                {
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaMonHoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaHocKy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiemChu = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DaCongBo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaHocPhan", x => new { x.MaSinhVien, x.MaMonHoc, x.MaHocKy });
                    table.ForeignKey(
                        name: "FK_KetQuaHocPhan_HocKyThucTe_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HocKyThucTe",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KetQuaHocPhan_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KetQuaHocPhan_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DieuKienHocPhan_MaMonDieuKien",
                table: "DieuKienHocPhan",
                column: "MaMonDieuKien");

            migrationBuilder.CreateIndex(
                name: "IX_DieuKienHocPhan_MaMonHoc",
                table: "DieuKienHocPhan",
                column: "MaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_DotDangKyHocPhan_KeHoachDaoTaoId",
                table: "DotDangKyHocPhan",
                column: "KeHoachDaoTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDangKyHocPhan_MaHocKy_MoLucUtc_DongLucUtc",
                table: "DotDangKyHocPhan",
                columns: new[] { "MaHocKy", "MoLucUtc", "DongLucUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaHocPhan_MaHocKy",
                table: "KetQuaHocPhan",
                column: "MaHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaHocPhan_MaMonHoc",
                table: "KetQuaHocPhan",
                column: "MaMonHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DieuKienHocPhan");

            migrationBuilder.DropTable(
                name: "DotDangKyHocPhan");

            migrationBuilder.DropTable(
                name: "HoSoHocVu");

            migrationBuilder.DropTable(
                name: "KetQuaHocPhan");

            migrationBuilder.DropColumn(
                name: "LoaiDangKy",
                table: "DanhSachSinhVienLop");
        }
    }
}
