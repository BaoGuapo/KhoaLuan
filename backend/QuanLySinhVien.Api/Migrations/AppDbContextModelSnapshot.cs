using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using QuanLySinhVien.Api.Data;

#nullable disable

namespace QuanLySinhVien.Api.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0").HasAnnotation("Relational:MaxIdentifierLength", 128).HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.RefreshToken", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<DateTime>("ExpiresAtUtc").HasColumnType("datetime2");
            b.Property<string>("ReplacedByTokenHash").HasColumnType("nvarchar(max)");
            b.Property<DateTime?>("RevokedAtUtc").HasColumnType("datetime2");
            b.Property<string>("TokenHash").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            b.HasKey("Id"); b.HasIndex("TokenHash").IsUnique(); b.HasIndex("UserId"); b.ToTable("RefreshTokens");
        });
        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.Role", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").HasAnnotation("SqlServer:Identity", "1, 1");
            b.Property<string>("Description").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("Name").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.HasKey("Id"); b.HasIndex("Name").IsUnique(); b.ToTable("Roles");
            b.HasData(
                new { Id = 1, Name = "Admin", Description = "Quản trị hệ thống" },
                new { Id = 2, Name = "CanBoQuanLy", Description = "Cán bộ quản lý" },
                new { Id = 3, Name = "GiangVien", Description = "Giảng viên" },
                new { Id = 4, Name = "SinhVien", Description = "Sinh viên" });
        });
        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.User", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<string>("Email").IsRequired().HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("FullName").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<bool>("IsActive").HasColumnType("bit");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("UserName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.HasKey("Id"); b.HasIndex("Email").IsUnique(); b.HasIndex("UserName").IsUnique(); b.ToTable("Users");
        });
        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.UserRole", b =>
        {
            b.Property<Guid>("UserId").HasColumnType("uniqueidentifier"); b.Property<int>("RoleId").HasColumnType("int");
            b.HasKey("UserId", "RoleId"); b.HasIndex("RoleId"); b.ToTable("UserRoles");
        });
        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.RefreshToken", b => b.HasOne("QuanLySinhVien.Api.Models.Entities.User", "User").WithMany("RefreshTokens").HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("QuanLySinhVien.Api.Models.Entities.UserRole", b =>
        {
            b.HasOne("QuanLySinhVien.Api.Models.Entities.Role", "Role").WithMany("UserRoles").HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.HasOne("QuanLySinhVien.Api.Models.Entities.User", "User").WithMany("UserRoles").HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
    }
}
