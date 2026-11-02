BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102141756_ThemLichSuSuaDiemDanh'
)
BEGIN
    CREATE TABLE [LichSuSuaDiemDanh] (
        [Id] uniqueidentifier NOT NULL,
        [MaBuoiHoc] nvarchar(450) NOT NULL,
        [MaSinhVien] nvarchar(30) NOT NULL,
        [TrangThaiCu] nvarchar(20) NULL,
        [TrangThaiMoi] nvarchar(20) NOT NULL,
        [LyDo] nvarchar(500) NOT NULL,
        [NguoiSua] nvarchar(50) NOT NULL,
        [SuaLucUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_LichSuSuaDiemDanh] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LichSuSuaDiemDanh_BuoiHoc_MaBuoiHoc] FOREIGN KEY ([MaBuoiHoc]) REFERENCES [BuoiHoc] ([MaBuoiHoc]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LichSuSuaDiemDanh_SinhVien_MaSinhVien] FOREIGN KEY ([MaSinhVien]) REFERENCES [SinhVien] ([MaSinhVien]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102141756_ThemLichSuSuaDiemDanh'
)
BEGIN
    CREATE INDEX [IX_LichSuSuaDiemDanh_MaBuoiHoc_SuaLucUtc] ON [LichSuSuaDiemDanh] ([MaBuoiHoc], [SuaLucUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102141756_ThemLichSuSuaDiemDanh'
)
BEGIN
    CREATE INDEX [IX_LichSuSuaDiemDanh_MaSinhVien] ON [LichSuSuaDiemDanh] ([MaSinhVien]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102141756_ThemLichSuSuaDiemDanh'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102141756_ThemLichSuSuaDiemDanh', N'8.0.0');
END;
GO

COMMIT;
GO

