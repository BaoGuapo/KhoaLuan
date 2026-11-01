using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class ThemViTriDiemDanh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DoChinhXacMet",
                table: "ChiTietDiemDanh",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KetQuaViTri",
                table: "ChiTietDiemDanh",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "KhoangCachMet",
                table: "ChiTietDiemDanh",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "KinhDo",
                table: "ChiTietDiemDanh",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ViDo",
                table: "ChiTietDiemDanh",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoChinhXacMet",
                table: "ChiTietDiemDanh");

            migrationBuilder.DropColumn(
                name: "KetQuaViTri",
                table: "ChiTietDiemDanh");

            migrationBuilder.DropColumn(
                name: "KhoangCachMet",
                table: "ChiTietDiemDanh");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "ChiTietDiemDanh");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "ChiTietDiemDanh");
        }
    }
}
