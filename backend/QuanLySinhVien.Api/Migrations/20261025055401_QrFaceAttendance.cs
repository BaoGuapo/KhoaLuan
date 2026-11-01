using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class QrFaceAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "HoSoKhuonMat",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "HoSoKhuonMat",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaSinhVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhienDiemDanhId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceChallenges_PhienDiemDanh_PhienDiemDanhId",
                        column: x => x.PhienDiemDanhId,
                        principalTable: "PhienDiemDanh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceChallenges_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChallenges_MaSinhVien_CreatedAtUtc",
                table: "AttendanceChallenges",
                columns: new[] { "MaSinhVien", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChallenges_PhienDiemDanhId",
                table: "AttendanceChallenges",
                column: "PhienDiemDanhId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceChallenges");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "HoSoKhuonMat");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "HoSoKhuonMat");
        }
    }
}
