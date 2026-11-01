using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations
{
    /// <inheritdoc />
    public partial class FaceEvidenceReplayReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrameHashes",
                table: "AttendanceChallenges",
                type: "nvarchar(194)",
                maxLength: 194,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrameHashes",
                table: "AttendanceChallenges");
        }
    }
}
