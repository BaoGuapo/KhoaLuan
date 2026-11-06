BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [MonHoc] ADD [LaMonTuChon] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [MonHoc] ADD [MaNganh] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [MonHoc] ADD [SoTietLyThuyet] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [MonHoc] ADD [SoTietThucHanh] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [MonHoc] ADD [SoTinChi] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LopHocPhan]') AND [c].[name] = N'MaHocKy');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [LopHocPhan] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [LopHocPhan] ALTER COLUMN [MaHocKy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [LichHoc] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [LoaiPhong] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [MonHocMaMonHoc] nvarchar(30) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [NgayBatDau] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [NgayKetThuc] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD [SoBuoi] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    CREATE TABLE [GiaoTrinh] (
        [Id] int NOT NULL IDENTITY,
        [MaMonHoc] nvarchar(30) NOT NULL,
        [TenFile] nvarchar(max) NOT NULL,
        [DuongDanFile] nvarchar(max) NOT NULL,
        [LoaiFile] nvarchar(max) NOT NULL,
        [NgayTao] datetime2 NOT NULL,
        CONSTRAINT [PK_GiaoTrinh] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GiaoTrinh_MonHoc_MaMonHoc] FOREIGN KEY ([MaMonHoc]) REFERENCES [MonHoc] ([MaMonHoc]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    CREATE TABLE [NganhDaoTaos] (
        [MaNganh] nvarchar(450) NOT NULL,
        [TenNganh] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_NganhDaoTaos] PRIMARY KEY ([MaNganh])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    CREATE INDEX [IX_LopHocPhan_MonHocMaMonHoc] ON [LopHocPhan] ([MonHocMaMonHoc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    CREATE INDEX [IX_GiaoTrinh_MaMonHoc] ON [GiaoTrinh] ([MaMonHoc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    ALTER TABLE [LopHocPhan] ADD CONSTRAINT [FK_LopHocPhan_MonHoc_MonHocMaMonHoc] FOREIGN KEY ([MonHocMaMonHoc]) REFERENCES [MonHoc] ([MaMonHoc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106142844_GiaoTrinh'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106142844_GiaoTrinh', N'8.0.0');
END;
GO

COMMIT;
GO

