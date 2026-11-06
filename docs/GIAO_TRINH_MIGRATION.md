# Gộp nhánh giáo trình vào cơ sở dữ liệu

Nhánh `feature/giao-trinh` ban đầu có 4 migration (`InitialCreate`, `ThemCotLoaiMonHoc`, `AddNganhDaoTaoTable`, `AddGiangVienMarketing`) và sửa migration cũ `LinkAcademicProfilesToUsers`. Khi gộp, các migration này được thay bằng một migration duy nhất `20261106142844_GiaoTrinh`, nối tiếp sau `ThemLichSuSuaDiemDanh`, vì:

- `InitialCreate` tạo lại 18 bảng đã có (`Users`, `Roles`, `SinhVien`, `MonHoc`...), làm `dotnet ef database update` báo lỗi bảng đã tồn tại.
- `LaMonTuChon` được thêm hai lần (`ThemCotLoaiMonHoc` và `AddNganhDaoTaoTable`).
- Bảng `GiaoTrinh` có trong model nhưng chưa migration nào tạo.
- Sửa migration đã chạy làm cơ sở dữ liệu dựng mới thiếu cột `UserId`.

Migration `GiaoTrinh` gồm: bảng `GiaoTrinh`, bảng `NganhDaoTaos`; cột `MonHoc.LaMonTuChon/MaNganh/SoTinChi/SoTietLyThuyet/SoTietThucHanh`; cột `LopHocPhan.LichHoc/LoaiPhong/NgayBatDau/NgayKetThuc/SoBuoi/MonHocMaMonHoc`; `LopHocPhan.MaHocKy` cho phép null.

## Cơ sở dữ liệu chưa chạy migration của nhánh giáo trình

Chạy bình thường:

```powershell
dotnet ef database update --project backend/QuanLySinhVien.Api
```

Hoặc chạy script idempotent [giao-trinh-migration.sql](giao-trinh-migration.sql).

## Cơ sở dữ liệu đã chạy migration cũ của nhánh giáo trình

Các bảng và cột đã có sẵn, chỉ cần sửa lịch sử migration. Sao lưu cơ sở dữ liệu trước, rồi chạy:

```sql
-- Xóa lịch sử 4 migration đã bỏ.
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    N'20261017092252_InitialCreate',
    N'20261018075436_ThemCotLoaiMonHoc',
    N'20261102015321_AddNganhDaoTaoTable',
    N'20261105144709_AddGiangVienMarketing');

-- Kiểm tra các đối tượng mà migration GiaoTrinh sẽ tạo; cả ba phải trả về 1.
SELECT CASE WHEN OBJECT_ID(N'[GiaoTrinh]') IS NULL THEN 0 ELSE 1 END AS CoBangGiaoTrinh,
       CASE WHEN OBJECT_ID(N'[NganhDaoTaos]') IS NULL THEN 0 ELSE 1 END AS CoBangNganhDaoTaos,
       CASE WHEN COL_LENGTH(N'LopHocPhan', N'MonHocMaMonHoc') IS NULL THEN 0 ELSE 1 END AS CoCotMonHocMaMonHoc;
```

- Nếu cả ba là 1: đánh dấu migration mới đã chạy để EF không tạo lại.

  ```sql
  INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
  VALUES (N'20261106142844_GiaoTrinh', N'8.0.0');
  ```

- Nếu có giá trị 0 (ví dụ chưa có bảng `GiaoTrinh`): đổi tên hoặc xóa đối tượng đang dở rồi chạy script [giao-trinh-migration.sql](giao-trinh-migration.sql).

Cuối cùng chạy `dotnet ef database update` để áp các migration còn thiếu của nhánh chính, ví dụ `ThemLichSuSuaDiemDanh`. Nếu cơ sở dữ liệu chỉ dùng để thử nghiệm, có thể dựng lại từ đầu theo [SETUP.md](SETUP.md).
