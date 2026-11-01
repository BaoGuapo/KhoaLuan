BEGIN TRANSACTION;
GO

CREATE TABLE [KeHoachDaoTao] (
    [Id] uniqueidentifier NOT NULL,
    [MaNganh] varchar(50) NOT NULL,
    [MaKhoa] varchar(50) NOT NULL,
    [TenKhoaDaoTao] nvarchar(200) NOT NULL,
    [TenChuongTrinh] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_KeHoachDaoTao] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KeHoachDaoTao_NganhHoc] FOREIGN KEY ([MaNganh]) REFERENCES [NganhHoc] ([maNganh]) ON DELETE NO ACTION,
    CONSTRAINT [FK_KeHoachDaoTao_KhoaTuyenSinh] FOREIGN KEY ([MaKhoa]) REFERENCES [KhoaTuyenSinh] ([maKhoa]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ApDungHocKy] (
    [Id] uniqueidentifier NOT NULL,
    [KeHoachDaoTaoId] uniqueidentifier NOT NULL,
    [MaHocKy] varchar(50) NOT NULL,
    [ThuTuHocKy] int NOT NULL,
    CONSTRAINT [PK_ApDungHocKy] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApDungHocKy_HocKyThucTe_MaHocKy] FOREIGN KEY ([MaHocKy]) REFERENCES [HocKyThucTe] ([MaHocKy]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApDungHocKy_KeHoachDaoTao_KeHoachDaoTaoId] FOREIGN KEY ([KeHoachDaoTaoId]) REFERENCES [KeHoachDaoTao] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [MonKeHoachDaoTao] (
    [KeHoachDaoTaoId] uniqueidentifier NOT NULL,
    [MaMonHoc] varchar(50) NOT NULL,
    [ThuTuHocKy] int NOT NULL,
    [BatBuoc] bit NOT NULL,
    CONSTRAINT [PK_MonKeHoachDaoTao] PRIMARY KEY ([KeHoachDaoTaoId], [MaMonHoc]),
    CONSTRAINT [FK_MonKeHoachDaoTao_KeHoachDaoTao_KeHoachDaoTaoId] FOREIGN KEY ([KeHoachDaoTaoId]) REFERENCES [KeHoachDaoTao] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MonKeHoachDaoTao_MonHoc_MaMonHoc] FOREIGN KEY ([MaMonHoc]) REFERENCES [MonHoc] ([MaMonHoc]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LopHocPhanDuKien] (
    [MaLopHocPhan] nvarchar(30) NOT NULL,
    [ApDungHocKyId] uniqueidentifier NOT NULL,
    [MaMonHoc] varchar(50) NOT NULL,
    [SiSoToiDa] int NOT NULL,
    [DaPhanCong] bit NOT NULL,
    CONSTRAINT [PK_LopHocPhanDuKien] PRIMARY KEY ([MaLopHocPhan]),
    CONSTRAINT [FK_LopHocPhanDuKien_ApDungHocKy_ApDungHocKyId] FOREIGN KEY ([ApDungHocKyId]) REFERENCES [ApDungHocKy] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LopHocPhanDuKien_MonHoc_MaMonHoc] FOREIGN KEY ([MaMonHoc]) REFERENCES [MonHoc] ([MaMonHoc]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_ApDungHocKy_KeHoachDaoTaoId_MaHocKy] ON [ApDungHocKy] ([KeHoachDaoTaoId], [MaHocKy]);
GO

CREATE INDEX [IX_ApDungHocKy_MaHocKy] ON [ApDungHocKy] ([MaHocKy]);
GO

CREATE UNIQUE INDEX [IX_KeHoachDaoTao_MaNganh_MaKhoa] ON [KeHoachDaoTao] ([MaNganh], [MaKhoa]);
GO

CREATE INDEX [IX_LopHocPhanDuKien_ApDungHocKyId] ON [LopHocPhanDuKien] ([ApDungHocKyId]);
GO

CREATE INDEX [IX_LopHocPhanDuKien_MaMonHoc] ON [LopHocPhanDuKien] ([MaMonHoc]);
GO

CREATE INDEX [IX_MonKeHoachDaoTao_MaMonHoc] ON [MonKeHoachDaoTao] ([MaMonHoc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011105226_TrainingWorkflow', N'8.0.0');
GO

COMMIT;
GO
