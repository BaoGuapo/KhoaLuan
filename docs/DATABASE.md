# Cơ sở dữ liệu

Mã nguồn **không tự dựng được database từ đầu**. Các migration EF Core chỉ tạo phần bảng thêm sau
(tài khoản, kế hoạch đào tạo, điểm danh QR, giáo trình, điểm tổng kết...). Các bảng nghiệp vụ gốc
như `SinhVien`, `GiangVien`, `MonHoc`, `LopHocPhan`, `LichHoc`, `BuoiHoc`, `ChiTietDiemDanh` có sẵn
từ database ban đầu của nhóm, không nằm trong migration. Vì vậy database được nộp kèm dưới dạng
file sao lưu.

## File nộp kèm

| File | Dùng khi |
|---|---|
| `QuanLySinhVien.bacpac` | **Khuyến nghị.** Nạp được trên SQL Server 2019, 2022, 2025 và Azure SQL |
| `QuanLySinhVien.bak` | Chỉ khôi phục được trên **SQL Server 2025 (bản 17)** trở lên |

Hai file có cùng nội dung: 37 bảng (bản `.bacpac` không kèm bảng sơ đồ `sysdiagrams` của SSMS),
đã áp dụng đủ 14 migration, kèm bộ dữ liệu demo.

## Khôi phục

Thay `.\SQLEXPRESS` bằng tên instance SQL Server trên máy nếu khác.

### Cách 1: file `.bacpac`

Cài công cụ `sqlpackage` một lần:

```powershell
dotnet tool install -g microsoft.sqlpackage
```

Nạp vào database mới tên `QuanLySinhVien`:

```powershell
sqlpackage /Action:Import /SourceFile:"QuanLySinhVien.bacpac" /TargetServerName:".\SQLEXPRESS" /TargetDatabaseName:QuanLySinhVien /TargetTrustServerCertificate:True
```

Cũng có thể dùng SSMS: chuột phải **Databases** → **Import Data-tier Application...** → chọn file.

### Cách 2: file `.bak` (SQL Server 2025)

Trong SSMS: chuột phải **Databases** → **Restore Database...** → **Device** → chọn file
`QuanLySinhVien.bak`. Hoặc dùng lệnh, thay `<DATA>` bằng thư mục dữ liệu của SQL Server:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -Q "RESTORE DATABASE QuanLySinhVien FROM DISK='C:\duong-dan\QuanLySinhVien.bak' WITH MOVE 'QuanLySinhVien' TO '<DATA>\QuanLySinhVien.mdf', MOVE 'QuanLySinhVien_log' TO '<DATA>\QuanLySinhVien_log.ldf'"
```

### Kiểm tra

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -d QuanLySinhVien -Q "SELECT (SELECT COUNT(*) FROM __EFMigrationsHistory) AS migration, (SELECT COUNT(*) FROM SinhVien) AS sinh_vien, (SELECT COUNT(*) FROM LopHocPhan) AS lop"
```

Kết quả đúng: `14` migration, `61` sinh viên, `25` lớp.

## Cấu hình backend trỏ tới database

Chạy tại thư mục gốc mã nguồn:

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=QuanLySinhVien;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
```

Tắt bộ dữ liệu mẫu `DEMO-*` mà backend tự chèn khi khởi động, để không trộn với dữ liệu demo đã có:

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "DevelopmentSeed:TrainingDemo" "false"
```

Cấu hình khóa JWT theo mục 3 của `README.md`, sau đó chạy backend và website như mục 6.

## Tài khoản demo

Mọi tài khoản dùng mật khẩu `Demo@12345`.

| Vai trò | Tài khoản |
|---|---|
| Admin | `admin` |
| Cán bộ quản lý | `CBQL001`, `CBQL002` |
| Giảng viên | `GV001` (dạy Lập trình di động HK1-2627), `GV_CNTT01`–`GV_CNTT05`, `GV_QTKD01`–`GV_QTKD05`, `GV_MKT01`–`GV_MKT10` |
| Sinh viên | `2001230048`, `2001240001`–`2001240040` (CNTT), `2013240001`–`2013240020` (QTKD) |

Chi tiết nội dung dữ liệu (học kỳ, lớp, điểm danh, điểm) xem `docs/DU_LIEU_DEMO.md`.

## Hồ sơ khuôn mặt

Bản nộp **không chứa hồ sơ khuôn mặt hay ảnh thẻ** của ai. Đây là dữ liệu sinh trắc học của người
thật, và ảnh được mã hóa bằng khóa riêng của máy tạo ra nên không đọc được trên máy khác. Để thử
chức năng điểm danh bằng khuôn mặt:

1. Đăng nhập một tài khoản sinh viên, vào **Hồ sơ khuôn mặt** và đăng ký bằng camera.
2. Đăng nhập `CBQL001` (hoặc `admin`), vào **Hồ sơ khuôn mặt** để duyệt hồ sơ.
3. Đăng nhập giảng viên phụ trách lớp, mở phiên điểm danh QR; sinh viên quét QR và xác minh khuôn mặt.
