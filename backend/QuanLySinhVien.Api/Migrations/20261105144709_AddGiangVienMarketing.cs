using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGiangVienMarketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoNhom",
                table: "MonHoc");

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

            migrationBuilder.InsertData(
                table: "GiangVien",
                columns: new[] { "MaGiangVien", "AnhDaiDien", "ChucDanh", "ChuyenNganh", "CoSo", "DanToc", "DiaChiLienHe", "Email", "GioiTinh", "HoKhauThuongTru", "HoTen", "Khoa", "matKhau", "NgayBatDauCongTac", "NgayCapCCCD", "NgaySinh", "NgayVaoDang", "NoiCapCCCD", "NoiSinh", "QuocTich", "SoCCCD", "SoDienThoai", "TonGiao", "TrangThaiTaiKhoan", "TrinhDoHocVan", "UserId" },
                values: new object[,]
                {
                    { "GV_MKT01", null, null, "Marketing", null, null, null, "phuongntm@huit.edu.vn", null, null, "Nguyễn Thị Mai Phương", null, "", null, null, null, null, null, null, null, null, "0901234567", null, true, "Tiến sĩ", null },
                    { "GV_MKT02", null, null, "Marketing", null, null, null, "namth@huit.edu.vn", null, null, "Trần Hoàng Nam", null, "", null, null, null, null, null, null, null, null, "0902345678", null, true, "Thạc sĩ", null },
                    { "GV_MKT03", null, null, "Marketing", null, null, null, "tulc@huit.edu.vn", null, null, "Lê Cẩm Tú", null, "", null, null, null, null, null, null, null, null, "0903456789", null, true, "Thạc sĩ", null },
                    { "GV_MKT04", null, null, "Marketing", null, null, null, "baopv@huit.edu.vn", null, null, "Phạm Vũ Bảo", null, "", null, null, null, null, null, null, null, null, "0904567890", null, true, "Phó Giáo sĩ / Tiến sĩ", null },
                    { "GV_MKT05", null, null, "Marketing", null, null, null, "trangdtt@huit.edu.vn", null, null, "Đỗ Thị Thu Trang", null, "", null, null, null, null, null, null, null, null, "0905678901", null, true, "Thạc sĩ", null },
                    { "GV_MKT06", null, null, "Marketing", null, null, null, "khoavm@huit.edu.vn", null, null, "Võ Minh Khoa", null, "", null, null, null, null, null, null, null, null, "0906789012", null, true, "Thạc sĩ", null },
                    { "GV_MKT07", null, null, "Marketing", null, null, null, "anhbh@huit.edu.vn", null, null, "Bùi Hoàng Anh", null, "", null, null, null, null, null, null, null, null, "0907890123", null, true, "Tiến sĩ", null },
                    { "GV_MKT08", null, null, "Marketing", null, null, null, "linhnp@huit.edu.vn", null, null, "Ngô Phương Linh", null, "", null, null, null, null, null, null, null, null, "0908901234", null, true, "Thạc sĩ", null },
                    { "GV_MKT09", null, null, "Marketing", null, null, null, "huydq@huit.edu.vn", null, null, "Đặng Quốc Huy", null, "", null, null, null, null, null, null, null, null, "0909012345", null, true, "Tiến sĩ", null },
                    { "GV_MKT10", null, null, "Marketing", null, null, null, "vannpk@huit.edu.vn", null, null, "Nguyễn Phan Khánh Vân", null, "", null, null, null, null, null, null, null, null, "0901122334", null, true, "Thạc sĩ", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT01");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT02");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT03");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT04");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT05");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT06");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT07");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT08");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT09");

            migrationBuilder.DeleteData(
                table: "GiangVien",
                keyColumn: "MaGiangVien",
                keyValue: "GV_MKT10");

            migrationBuilder.DropColumn(
                name: "LichHoc",
                table: "LopHocPhan");

            migrationBuilder.DropColumn(
                name: "LoaiPhong",
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

            migrationBuilder.AddColumn<int>(
                name: "SoNhom",
                table: "MonHoc",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
