using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class FacePhotoReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationPhotosProtected",
                table: "HoSoKhuonMat",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "HoSoKhuonMat",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedBy",
                table: "HoSoKhuonMat",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "HoSoKhuonMat",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "HoSoKhuonMat",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewVersion",
                table: "HoSoKhuonMat",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentIdentityPhotos",
                columns: table => new
                {
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ImageProtected = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentIdentityPhotos", x => x.MaSinhVien);
                    table.ForeignKey(
                        name: "FK_StudentIdentityPhotos_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentIdentityPhotos");

            migrationBuilder.DropColumn(
                name: "RegistrationPhotosProtected",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "RejectedAtUtc",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "ReviewVersion",
                table: "HoSoKhuonMat");
        }
    }
}
