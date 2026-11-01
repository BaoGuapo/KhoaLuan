using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrainingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy types/columns already match Azure SQL. Only update model mappings; do not alter existing data.

            migrationBuilder.CreateTable(
                name: "KeHoachDaoTao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaNganh = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MaKhoa = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TenKhoaDaoTao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenChuongTrinh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeHoachDaoTao", x => x.Id);
                    table.ForeignKey("FK_KeHoachDaoTao_NganhHoc", x => x.MaNganh, "NganhHoc", "maNganh", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_KeHoachDaoTao_KhoaTuyenSinh", x => x.MaKhoa, "KhoaTuyenSinh", "maKhoa", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApDungHocKy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeHoachDaoTaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaHocKy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ThuTuHocKy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApDungHocKy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApDungHocKy_HocKyThucTe_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HocKyThucTe",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApDungHocKy_KeHoachDaoTao_KeHoachDaoTaoId",
                        column: x => x.KeHoachDaoTaoId,
                        principalTable: "KeHoachDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonKeHoachDaoTao",
                columns: table => new
                {
                    KeHoachDaoTaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaMonHoc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ThuTuHocKy = table.Column<int>(type: "int", nullable: false),
                    BatBuoc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonKeHoachDaoTao", x => new { x.KeHoachDaoTaoId, x.MaMonHoc });
                    table.ForeignKey(
                        name: "FK_MonKeHoachDaoTao_KeHoachDaoTao_KeHoachDaoTaoId",
                        column: x => x.KeHoachDaoTaoId,
                        principalTable: "KeHoachDaoTao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonKeHoachDaoTao_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LopHocPhanDuKien",
                columns: table => new
                {
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApDungHocKyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaMonHoc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SiSoToiDa = table.Column<int>(type: "int", nullable: false),
                    DaPhanCong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocPhanDuKien", x => x.MaLopHocPhan);
                    table.ForeignKey(
                        name: "FK_LopHocPhanDuKien_ApDungHocKy_ApDungHocKyId",
                        column: x => x.ApDungHocKyId,
                        principalTable: "ApDungHocKy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LopHocPhanDuKien_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApDungHocKy_KeHoachDaoTaoId_MaHocKy",
                table: "ApDungHocKy",
                columns: new[] { "KeHoachDaoTaoId", "MaHocKy" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApDungHocKy_MaHocKy",
                table: "ApDungHocKy",
                column: "MaHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachDaoTao_MaNganh_MaKhoa",
                table: "KeHoachDaoTao",
                columns: new[] { "MaNganh", "MaKhoa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhanDuKien_ApDungHocKyId",
                table: "LopHocPhanDuKien",
                column: "ApDungHocKyId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhanDuKien_MaMonHoc",
                table: "LopHocPhanDuKien",
                column: "MaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_MonKeHoachDaoTao_MaMonHoc",
                table: "MonKeHoachDaoTao",
                column: "MaMonHoc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LopHocPhanDuKien");

            migrationBuilder.DropTable(
                name: "MonKeHoachDaoTao");

            migrationBuilder.DropTable(
                name: "ApDungHocKy");

            migrationBuilder.DropTable(
                name: "KeHoachDaoTao");

        }
    }
}
