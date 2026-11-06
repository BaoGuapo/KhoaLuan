using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiangVien",
                columns: table => new
                {
                    MaGiangVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrangThaiTaiKhoan = table.Column<bool>(type: "bit", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoiSinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DanToc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TonGiao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuocTich = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapCCCD = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoiCapCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChiLienHe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoKhauThuongTru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayBatDauCongTac = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoSo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Khoa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChuyenNganh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrinhDoHocVan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChucDanh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayVaoDang = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiangVien", x => x.MaGiangVien);
                });

            migrationBuilder.CreateTable(
                name: "HocKyThucTe",
                columns: table => new
                {
                    MaHocKy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenHocKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiHocKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocKyThucTe", x => x.MaHocKy);
                });

            migrationBuilder.CreateTable(
                name: "NganhDaoTao",
                columns: table => new
                {
                    MaNganh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenNganh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NganhDaoTao", x => x.MaNganh);
                });

            migrationBuilder.CreateTable(
                name: "PhongHoc",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenPhong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiaChiCoSo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoaiPhong = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongHoc", x => x.MaPhong);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SinhVien",
                columns: table => new
                {
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrangThaiTaiKhoan = table.Column<bool>(type: "bit", nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoiSinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DanToc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TonGiao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuocTich = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KhuVuc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapCCCD = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoiCapCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChiLienHe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoKhauThuongTru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaHoSo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayVaoTruong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LopHoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoSo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BacDaoTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoaiHinhDaoTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Khoa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nganh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChuyenNganh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KhoaHoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoiTuong = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DienChinhSach = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayVaoDoan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayVaoDang = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhVien", x => x.MaSinhVien);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HieuLucTuUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaoLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonHoc",
                columns: table => new
                {
                    MaMonHoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenMonHoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoTinChiLyThuyet = table.Column<int>(type: "int", nullable: false),
                    SoTinChiThucHanh = table.Column<int>(type: "int", nullable: false),
                    SoTC = table.Column<int>(type: "int", nullable: false),
                    TietLT = table.Column<int>(type: "int", nullable: false),
                    TietTH = table.Column<int>(type: "int", nullable: false),
                    MaNganh = table.Column<string>(type: "nvarchar(30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHoc", x => x.MaMonHoc);
                    table.ForeignKey(
                        name: "FK_MonHoc_NganhDaoTao_MaNganh",
                        column: x => x.MaNganh,
                        principalTable: "NganhDaoTao",
                        principalColumn: "MaNganh",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HoSoKhuonMat",
                columns: table => new
                {
                    MaHoSo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    VectorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoKhuonMat", x => x.MaHoSo);
                    table.ForeignKey(
                        name: "FK_HoSoKhuonMat_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LopHocPhan",
                columns: table => new
                {
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaMonHoc = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    MaHocKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaGiangVien = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    SiSoToiDa = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonHocMaMonHoc = table.Column<string>(type: "nvarchar(30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocPhan", x => x.MaLopHocPhan);
                    table.ForeignKey(
                        name: "FK_LopHocPhan_GiangVien_MaGiangVien",
                        column: x => x.MaGiangVien,
                        principalTable: "GiangVien",
                        principalColumn: "MaGiangVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LopHocPhan_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LopHocPhan_MonHoc_MonHocMaMonHoc",
                        column: x => x.MonHocMaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc");
                });

            migrationBuilder.CreateTable(
                name: "DanhSachSinhVienLop",
                columns: table => new
                {
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    ThoiGianDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongSoBuoiVang = table.Column<int>(type: "int", nullable: false),
                    TrangThaiCanhBao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhSachSinhVienLop", x => new { x.MaSinhVien, x.MaLopHocPhan });
                    table.ForeignKey(
                        name: "FK_DanhSachSinhVienLop_LopHocPhan_MaLopHocPhan",
                        column: x => x.MaLopHocPhan,
                        principalTable: "LopHocPhan",
                        principalColumn: "MaLopHocPhan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhSachSinhVienLop_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichHoc",
                columns: table => new
                {
                    MaLichHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    MaPhong = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    ThuTrongTuan = table.Column<int>(type: "int", nullable: false),
                    TietBatDau = table.Column<int>(type: "int", nullable: false),
                    TietKetThuc = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHoc", x => x.MaLichHoc);
                    table.ForeignKey(
                        name: "FK_LichHoc_LopHocPhan_MaLopHocPhan",
                        column: x => x.MaLopHocPhan,
                        principalTable: "LopHocPhan",
                        principalColumn: "MaLopHocPhan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichHoc_PhongHoc_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "PhongHoc",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuoiHoc",
                columns: table => new
                {
                    MaBuoiHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaLichHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayHocCuThe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianDiemDanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThaiBuoiHoc = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuoiHoc", x => x.MaBuoiHoc);
                    table.ForeignKey(
                        name: "FK_BuoiHoc_LichHoc_MaLichHoc",
                        column: x => x.MaLichHoc,
                        principalTable: "LichHoc",
                        principalColumn: "MaLichHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDiemDanh",
                columns: table => new
                {
                    MaBuoiHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    ThoiGianCheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDiemDanh", x => new { x.MaBuoiHoc, x.MaSinhVien });
                    table.ForeignKey(
                        name: "FK_ChiTietDiemDanh_BuoiHoc_MaBuoiHoc",
                        column: x => x.MaBuoiHoc,
                        principalTable: "BuoiHoc",
                        principalColumn: "MaBuoiHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDiemDanh_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhienDiemDanh",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaBuoiHoc = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QrTokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MoLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HetHanLucUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DongLucUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhienDiemDanh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhienDiemDanh_BuoiHoc_MaBuoiHoc",
                        column: x => x.MaBuoiHoc,
                        principalTable: "BuoiHoc",
                        principalColumn: "MaBuoiHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Quản trị hệ thống", "Admin" },
                    { 2, "Cán bộ quản lý", "CanBoQuanLy" },
                    { 3, "Giảng viên", "GiangVien" },
                    { 4, "Sinh viên", "SinhVien" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuoiHoc_MaLichHoc",
                table: "BuoiHoc",
                column: "MaLichHoc");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDiemDanh_MaSinhVien",
                table: "ChiTietDiemDanh",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_DanhSachSinhVienLop_MaLopHocPhan",
                table: "DanhSachSinhVienLop",
                column: "MaLopHocPhan");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoKhuonMat_MaSinhVien",
                table: "HoSoKhuonMat",
                column: "MaSinhVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichHoc_MaLopHocPhan",
                table: "LichHoc",
                column: "MaLopHocPhan");

            migrationBuilder.CreateIndex(
                name: "IX_LichHoc_MaPhong",
                table: "LichHoc",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MaGiangVien",
                table: "LopHocPhan",
                column: "MaGiangVien");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MaMonHoc",
                table: "LopHocPhan",
                column: "MaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MonHocMaMonHoc",
                table: "LopHocPhan",
                column: "MonHocMaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_MonHoc_MaNganh",
                table: "MonHoc",
                column: "MaNganh");

            migrationBuilder.CreateIndex(
                name: "IX_PhienDiemDanh_MaBuoiHoc",
                table: "PhienDiemDanh",
                column: "MaBuoiHoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhienDiemDanh_QrTokenHash",
                table: "PhienDiemDanh",
                column: "QrTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDiemDanh");

            migrationBuilder.DropTable(
                name: "DanhSachSinhVienLop");

            migrationBuilder.DropTable(
                name: "HocKyThucTe");

            migrationBuilder.DropTable(
                name: "HoSoKhuonMat");

            migrationBuilder.DropTable(
                name: "PhienDiemDanh");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "BuoiHoc");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LichHoc");

            migrationBuilder.DropTable(
                name: "LopHocPhan");

            migrationBuilder.DropTable(
                name: "PhongHoc");

            migrationBuilder.DropTable(
                name: "GiangVien");

            migrationBuilder.DropTable(
                name: "MonHoc");

            migrationBuilder.DropTable(
                name: "NganhDaoTao");
        }
    }
}
