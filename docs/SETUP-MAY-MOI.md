# Dựng dự án trên máy mới

Hướng dẫn này đủ chi tiết để một AI agent hoặc một người chưa từng chạy dự án tự dựng được
môi trường chạy đầy đủ: backend API, website, và cơ sở dữ liệu kèm dữ liệu mẫu.

Mất khoảng 15–30 phút, phần lớn là thời gian tải gói NuGet.

---

## 0. Cần có sẵn trên máy

| Thứ | Ghi chú |
|---|---|
| **.NET SDK 8** | `dotnet --version` phải in ra `8.x`. Tải tại dotnet.microsoft.com |
| **SQL Server 17 trở lên** | Express là đủ. **Bản cũ hơn 17 sẽ không khôi phục được file sao lưu** |
| **sqlcmd** | Thường cài kèm SQL Server |
| **Git** | Để lấy mã nguồn |

Hai thứ phải xin người bàn giao:

1. File sao lưu cơ sở dữ liệu `HKH.bak` (khoảng 13 MB)
2. Mật khẩu chung của các tài khoản demo

Kiểm tra phiên bản SQL Server trước khi làm tiếp. Nếu dưới 17 thì dừng lại và cài bản mới hơn,
vì mọi bước sau sẽ thất bại:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -I -W -Q "SELECT SERVERPROPERTY('ProductVersion')"
```

---

## 1. Lấy mã nguồn

```powershell
git clone https://github.com/BaoGuapo/KhoaLuan.git
cd KhoaLuan
```

Ba mô hình nhận diện khuôn mặt (`backend/QuanLySinhVien.Api/Models/Face/*.onnx`) đã nằm sẵn
trong repo, không cần tải thêm.

---

## 2. Khôi phục cơ sở dữ liệu

Tìm thư mục dữ liệu của SQL Server trên máy này:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -I -W -Q "SELECT SERVERPROPERTY('InstanceDefaultDataPath')"
```

Khôi phục, thay `C:\Tai\HKH.bak` bằng đường dẫn file bạn nhận được và `<DATA_PATH>` bằng kết
quả vừa lấy ở trên:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -I -Q "RESTORE DATABASE QuanLySinhVien_Personal_HKH FROM DISK='C:\Tai\HKH.bak' WITH MOVE 'QuanLySinhVien_Personal_HKH' TO '<DATA_PATH>QuanLySinhVien_Personal_HKH.mdf', MOVE 'QuanLySinhVien_Personal_HKH_log' TO '<DATA_PATH>QuanLySinhVien_Personal_HKH_log.ldf', STATS=50"
```

Kiểm tra. Phải thấy **31 bảng** và **10 migration**:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -I -W -d QuanLySinhVien_Personal_HKH -Q "SET NOCOUNT ON; SELECT COUNT(*) AS so_bang FROM sys.tables; SELECT COUNT(*) AS so_migration FROM __EFMigrationsHistory"
```

Nếu `so_migration` là 0 thì file sao lưu lấy từ bản cũ chưa ghi nhận lịch sử migration. Chạy:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -I -d QuanLySinhVien_Personal_HKH -i backend\QuanLySinhVien.Api\Migrations\BaselineExistingDatabase.sql
```

---

## 3. Cấu hình

Dự án **không có `appsettings.json`**, mọi khóa nằm trong User Secrets nên không bị commit.
Mẫu đầy đủ xem ở `backend/QuanLySinhVien.Api/appsettings.example.json`.

### Chuỗi kết nối

Nếu instance của bạn không phải `.\SQLEXPRESS` thì sửa lại cho đúng:

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=QuanLySinhVien_Personal_HKH;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
```

### Khóa JWT

Sinh khóa mới, **không xin của người khác**. Khóa chỉ dùng để ký token đăng nhập; đổi khóa
chỉ khiến các phiên đăng nhập cũ hết hiệu lực, không ảnh hưởng dữ liệu.

```powershell
$bytes = New-Object byte[] 48; (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes); $key = [Convert]::ToBase64String($bytes); dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" $key
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Issuer" "QuanLySinhVien.Api"
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Audience" "QuanLySinhVien.Clients"
```

API sẽ không khởi động nếu thiếu chuỗi kết nối, hoặc nếu `Jwt:Key` ngắn hơn 32 ký tự.

Website không cần cấu hình gì thêm: `web/QuanLySinhVien.Web/appsettings.json` đã trỏ sẵn tới
`https://localhost:7081/`.

---

## 4. Chạy

Mở **hai cửa sổ terminal riêng**, mỗi cửa sổ một lệnh:

```powershell
dotnet run --project backend/QuanLySinhVien.Api
```

```powershell
dotnet run --project web/QuanLySinhVien.Web
```

| Thành phần | Địa chỉ |
|---|---|
| Website | https://localhost:7082 |
| API | https://localhost:7081 |
| Swagger | https://localhost:7081/swagger |

Lần đầu chạy ở môi trường Development, API tự tạo thêm bốn tài khoản dev. Đây là hành vi
bình thường, không phải lỗi.

---

## 5. Kiểm tra đã chạy đúng

Chạy bộ kiểm thử sẵn có. Tất cả phải xanh:

```powershell
dotnet run --project backend/QuanLySinhVien.Checks -- --class-periods
```

```powershell
dotnet run --project backend/QuanLySinhVien.Checks -- --attendance
```

```powershell
dotnet run --project backend/QuanLySinhVien.Checks -- --email
```

Kết quả mong đợi: 19, 70 và 8 check đều PASS.

Sau đó mở website, đăng nhập bằng một tài khoản demo và kiểm tra:

- Vào **Báo cáo** → thấy số liệu thống kê và ba biểu đồ
- Vào **Lớp phụ trách** → thấy danh sách lớp của giảng viên

---

## 6. Tài khoản đăng nhập

Mật khẩu chung của bộ tài khoản demo phải **hỏi người bàn giao**, không ghi trong tài liệu này.

| Vai trò | Tài khoản |
|---|---|
| Quản trị | `HKH-ADMIN` |
| Cán bộ quản lý | `HKH-CBQL` |
| Giảng viên | `HKH-GV01` … `HKH-GV03` |
| Sinh viên | `HKH-SV01` … `HKH-SV30` |

Ngoài ra có bốn tài khoản dev do hệ thống tự tạo, mật khẩu cố định trong mã nguồn
(`Data/DbInitializer.cs`), lần đăng nhập đầu sẽ bắt đổi mật khẩu:

`admin`, `CBQL001`, `GV001`, `2001230048`

---

## 7. Dữ liệu khuôn mặt sẽ KHÔNG dùng được

Đây là hạn chế đã biết, không phải lỗi cài đặt.

Vector khuôn mặt và ảnh đăng ký trong cơ sở dữ liệu được mã hóa bằng ASP.NET Data Protection,
và khóa giải mã lại được Windows DPAPI khóa chặt theo **tài khoản Windows của máy gốc**. Máy
khác không giải mã được, kể cả khi có file khóa.

Hệ quả trên máy mới:

| Chức năng | Tình trạng |
|---|---|
| Đăng nhập, sinh viên, lớp học phần, lịch học | Chạy bình thường |
| Điểm danh, báo cáo, xuất Excel | Chạy bình thường |
| Hồ sơ khuôn mặt **đã đăng ký từ trước** | Không đọc được |
| Đăng ký khuôn mặt **mới** | Chạy bình thường, máy tự sinh khóa riêng |

Cách xử lý: đăng ký lại khuôn mặt cho những sinh viên cần demo. Trang duyệt hồ sơ cũ có thể
báo thiếu ảnh — trả hồ sơ lại để sinh viên đăng ký lại là xong.

---

## 8. Cấu hình tùy chọn

Bỏ qua được. Thiếu thì hệ thống tự chuyển sang phương án dự phòng, không hỏng gì.

### Cloudinary — nơi lưu ảnh đăng ký khuôn mặt

Không khai báo thì ảnh lưu thẳng trong cơ sở dữ liệu như trước. Muốn dùng thì xin bộ khóa từ
người bàn giao rồi đặt `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`,
và `Cloudinary:Enabled` bằng `true`.

### Email — gửi thông báo và cảnh báo chuyên cần

Không khai báo thì hệ thống chỉ ghi nội dung thư vào nhật ký, kèm dòng
`Chưa cấu hình SMTP nên chỉ ghi nhật ký`. Rất tiện để demo mà không gửi thư thật.

Muốn gửi thật, dùng Gmail có bật xác thực 2 bước rồi tạo App Password tại
[myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords):

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Email:Enabled" "true"
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Email:Host" "smtp.gmail.com"
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Email:UserName" "<email-cua-ban>"
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Email:Password" "<app-password-16-ky-tu>"
```

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Email:FromAddress" "<email-cua-ban>"
```

Email của sinh viên trong dữ liệu mẫu đều là `@personal.local.test` nên gửi tới đó sẽ lỗi tên
miền. Muốn thử gửi thật thì đổi email của một sinh viên thành địa chỉ có thật.

---

## 9. Xử lý sự cố

Các lỗi dưới đây đều đã gặp thật khi dựng dự án.

### `RESTORE` báo sai phiên bản

SQL Server dưới bản 17 không đọc được file sao lưu này. Phải cài bản 17 trở lên, không có cách
vòng nào khác.

### `RESTORE` báo không mở được file `.bak`

Tài khoản chạy dịch vụ SQL Server không có quyền đọc thư mục chứa file. Cấp quyền rồi thử lại,
sửa tên instance nếu khác:

```powershell
icacls C:\Tai /grant '"NT SERVICE\MSSQL$SQLEXPRESS":(OI)(CI)R' /T
```

### `UPDATE failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'`

Thiếu tham số `-I` khi gọi `sqlcmd`. Mọi lệnh trong tài liệu này đều đã có sẵn `-I`.

### `BACKUP DATABASE WITH COMPRESSION is not supported on Express Edition`

Bản Express không nén được bản sao lưu. Bỏ `COMPRESSION` khỏi câu lệnh.

### API báo lỗi kết nối, `ClientConnectionId` toàn số 0

API chưa bao giờ kết nối được tới SQL Server. Kiểm tra theo thứ tự: dịch vụ SQL Server đang
chạy chưa, tên instance trong chuỗi kết nối có đúng không, và tài khoản Windows hiện tại có
quyền vào database không.

### `dotnet build` báo không ghi được file `.exe`

API hoặc website đang chạy và giữ file. Dừng tiến trình đó rồi build lại.

### Dùng LocalDB thay vì SQLEXPRESS

LocalDB chỉ chạy cho một tài khoản Windows trên một máy, tự tắt khi không dùng, và hai
instance không mở chung một file dữ liệu được. Dự án đã chuyển sang SQL Server Express vì
lý do đó. **Không nên quay lại LocalDB.**

### `promote-to-primary.ps1`

Script cũ trong `.local-personal`, ghi cứng chuỗi kết nối LocalDB. Chạy nó sẽ phá cấu hình
vừa dựng. Đừng dùng.
