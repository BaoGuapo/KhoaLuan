using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySinhVien.Api.Migrations;

public partial class LinkAcademicProfilesToUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "SinhVien",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "GiangVien",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SinhVien_UserId",
            table: "SinhVien",
            column: "UserId",
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_GiangVien_UserId",
            table: "GiangVien",
            column: "UserId",
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_SinhVien_Users_UserId",
            table: "SinhVien",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_GiangVien_Users_UserId",
            table: "GiangVien",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_SinhVien_Users_UserId", table: "SinhVien");
        migrationBuilder.DropForeignKey(name: "FK_GiangVien_Users_UserId", table: "GiangVien");
        migrationBuilder.DropIndex(name: "IX_SinhVien_UserId", table: "SinhVien");
        migrationBuilder.DropIndex(name: "IX_GiangVien_UserId", table: "GiangVien");
        migrationBuilder.DropColumn(name: "UserId", table: "SinhVien");
        migrationBuilder.DropColumn(name: "UserId", table: "GiangVien");
    }
}
