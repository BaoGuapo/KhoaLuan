using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNganhDaoTaoTable : Migration
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
                name: "SoNhom",
                table: "MonHoc",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<string>(
                name: "MonHocMaMonHoc",
                table: "LopHocPhan",
                type: "nvarchar(30)",
                nullable: true);

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
                name: "SoNhom",
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
                name: "MonHocMaMonHoc",
                table: "LopHocPhan");
        }
    }
}
