using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations;

// The legacy matKhau columns already exist in the original academic database.
// This no-op migration records their mapping in the EF model snapshot without
// recreating or modifying those columns. Authentication uses Users.PasswordHash.
public partial class MapLegacyProfilePasswordColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
