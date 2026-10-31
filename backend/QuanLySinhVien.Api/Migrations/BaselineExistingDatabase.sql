-- Ghi nhận lịch sử migration cho một cơ sở dữ liệu đã có sẵn schema.
--
-- Dùng khi database được tạo bằng GenerateCreateScript (PersonalSeeder) chứ không qua
-- "dotnet ef database update", nên thiếu bảng __EFMigrationsHistory. Script này KHÔNG
-- đổi schema, chỉ khai báo rằng mười migration dưới đây coi như đã áp dụng, để từ đó
-- EF quản lý được database và các migration sau chạy bình thường.
--
-- Chỉ chạy khi schema đã khớp với model hiện tại. Chạy lại nhiều lần vẫn an toàn.
--
--   sqlcmd -S "<server>" -E -C -I -d <database> -i BaselineExistingDatabase.sql

SET NOCOUNT ON;

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Da tao bang __EFMigrationsHistory.';
END

DECLARE @applied TABLE (MigrationId nvarchar(150) PRIMARY KEY);
INSERT INTO @applied (MigrationId) VALUES
    (N'20260918000000_InitialIdentity'),
    (N'20260921104243_LinkAcademicProfilesToUsers'),
    (N'20260921104630_MapLegacyProfilePasswordColumns'),
    (N'20260921123628_RequirePasswordChangeOnFirstLogin'),
    (N'20260927152707_ExpandAcademicProfiles'),
    (N'20261011105226_TrainingWorkflow'),
    (N'20261018191740_CourseRegistrationRules'),
    (N'20261025055401_QrFaceAttendance'),
    (N'20261025060842_FaceEvidenceReplayReceipts'),
    (N'20261026165807_FacePhotoReview');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT a.MigrationId, N'8.0.0'
FROM @applied AS a
WHERE NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] AS h WHERE h.MigrationId = a.MigrationId);

DECLARE @total int = (SELECT COUNT(*) FROM [__EFMigrationsHistory]);
PRINT CONCAT(N'Tong so migration da ghi nhan: ', @total);
