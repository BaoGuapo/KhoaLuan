BEGIN TRANSACTION;
GO

ALTER TABLE [HoSoKhuonMat] ADD [ApprovedAtUtc] datetime2 NULL;
GO

ALTER TABLE [HoSoKhuonMat] ADD [ApprovedBy] nvarchar(100) NULL;
GO

CREATE TABLE [AttendanceChallenges] (
    [Id] uniqueidentifier NOT NULL,
    [MaSinhVien] nvarchar(30) NOT NULL,
    [PhienDiemDanhId] uniqueidentifier NULL,
    [Purpose] nvarchar(20) NOT NULL,
    [IssuedTo] nvarchar(100) NOT NULL,
    [Direction] nvarchar(5) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [UsedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_AttendanceChallenges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceChallenges_PhienDiemDanh_PhienDiemDanhId] FOREIGN KEY ([PhienDiemDanhId]) REFERENCES [PhienDiemDanh] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceChallenges_SinhVien_MaSinhVien] FOREIGN KEY ([MaSinhVien]) REFERENCES [SinhVien] ([MaSinhVien]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_AttendanceChallenges_MaSinhVien_CreatedAtUtc] ON [AttendanceChallenges] ([MaSinhVien], [CreatedAtUtc]);
GO

CREATE INDEX [IX_AttendanceChallenges_PhienDiemDanhId] ON [AttendanceChallenges] ([PhienDiemDanhId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261025055401_QrFaceAttendance', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AttendanceChallenges] ADD [FrameHashes] nvarchar(194) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261025060842_FaceEvidenceReplayReceipts', N'8.0.0');
GO

COMMIT;
GO

