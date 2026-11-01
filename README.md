# Hệ thống Quản lý Sinh viên tích hợp Nhận diện Khuôn mặt

Repository chung của đồ án, gồm ASP.NET Core Web API, website ASP.NET Core MVC và ứng dụng Flutter.

## Cấu trúc repository

```text
backend/  ASP.NET Core Web API, Entity Framework Core và Azure SQL
web/      ASP.NET Core MVC, gọi backend API qua HTTPS
mobile/   Ứng dụng Flutter
docs/     Tài liệu, hướng dẫn và quy ước nhóm
scripts/  Script hỗ trợ môi trường phát triển
```

Chỉ `backend` được kết nối trực tiếp đến Azure SQL. `web` và `mobile` phải sử dụng API.

## 1. Yêu cầu môi trường

- Git.
- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).
- Flutter SDK chỉ cần khi phát triển phần `mobile`.
- Một tài khoản Azure SQL được cấp quyền trên database của nhóm.
- IP hiện tại đã được thêm tại Azure Portal: **SQL server → Networking → Firewall rules**.

Kiểm tra .NET sau khi cài đặt:

```powershell
dotnet --version
```

Kết quả phải là phiên bản `8.x` hoặc phiên bản tương thích với `net8.0`.

## 2. Lấy source và khôi phục package

```powershell
git clone https://github.com/BaoGuapo/KhoaLuan.git
cd KhoaLuan
dotnet restore backend/QuanLySinhVien.Api
dotnet restore web/QuanLySinhVien.Web
```

Không cần chạy `scripts/initialize.ps1` sau khi clone vì các project đã được tạo sẵn.

## 3. Cấu hình bí mật backend

Project backend đã có `UserSecretsId`, vì vậy mỗi thành viên chỉ cần đặt các giá trị bí mật trên máy của mình. Không ghi giá trị thật vào `appsettings.json`, `appsettings.example.json` hoặc Git.

### Connection string Azure SQL

Thay các phần `<...>` bằng thông tin được người quản trị nhóm cung cấp:

```powershell
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<database-user>;Password=<database-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

Nên dùng tài khoản database riêng có quyền hạn chế cho thành viên, không chia sẻ tài khoản quản trị Azure SQL.

### Cấu hình JWT

Tạo JWT key ngẫu nhiên trên máy hiện tại:

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Issuer" "QuanLySinhVien.Api"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Audience" "QuanLySinhVien.Clients"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" $jwtKey
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:AccessTokenMinutes" "30"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:RefreshTokenDays" "7"
```

User Secrets nằm ngoài repository và không được Git theo dõi. Không gửi file `secrets.json` dạng không mã hóa qua Git, email hoặc ứng dụng nhắn tin.

## 4. Chứng chỉ HTTPS cục bộ

Chạy một lần trên mỗi máy:

```powershell
dotnet dev-certs https --trust
```

Chọn **Yes** nếu Windows yêu cầu xác nhận tin cậy chứng chỉ.

## 5. Entity Framework Core và database

Cài công cụ migration một lần:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

Áp dụng migration sau khi connection string hợp lệ và Azure Firewall đã cho phép IP:

```powershell
dotnet ef database update --project backend/QuanLySinhVien.Api --startup-project backend/QuanLySinhVien.Api
```

Lệnh này có thể chạy lại an toàn; EF Core chỉ áp dụng migration còn thiếu. Nếu nhóm cùng dùng một Azure SQL database đã được migration, lệnh sẽ báo database đã ở phiên bản mới nhất.

## 6. Chạy hệ thống

Mở hai cửa sổ PowerShell tại thư mục gốc repository.

Cửa sổ thứ nhất — backend API:

```powershell
dotnet run --project backend/QuanLySinhVien.Api
```

Cửa sổ thứ hai — website:

```powershell
dotnet run --project web/QuanLySinhVien.Web
```

Các địa chỉ mặc định:

- Website: [https://localhost:7082](https://localhost:7082)
- Trang đăng nhập: [https://localhost:7082/tai-khoan/dang-nhap](https://localhost:7082/tai-khoan/dang-nhap)
- Swagger API: [https://localhost:7081/swagger](https://localhost:7081/swagger)
- Backend API: `https://localhost:7081`

Luôn chạy backend trước website. Dừng một server bằng `Ctrl+C` trong cửa sổ đang chạy server đó.

## 7. Tài khoản kiểm thử Development

Sau khi migration hoàn tất, lần chạy backend đầu tiên trong môi trường Development sẽ tạo bốn role và các tài khoản sau:

| Role | Mã đăng nhập | Mật khẩu mẫu |
|---|---|---|
| `Admin` | `admin` | `Admin@123` |
| `CanBoQuanLy` | `CBQL001` | `CanBo@123` |
| `GiangVien` | `GV001` | `GiangVien@123` |
| `SinhVien` | `2001230048` | `SinhVien@123` |

Các mật khẩu này chỉ dành cho Development, không sử dụng khi triển khai thật.

## 8. Kiểm tra trước khi commit hoặc push

```powershell
dotnet build backend/QuanLySinhVien.Api
dotnet build web/QuanLySinhVien.Web
git status
```

Không commit các nội dung sau:

- Connection string hoặc mật khẩu Azure SQL.
- JWT key thật.
- `appsettings.Development.json`, `.env`, `secrets.json`.
- Thư mục `bin/`, `obj/`, `.vs/` hoặc file build Flutter.

Các loại file trên đã được khai báo trong `.gitignore`, nhưng mỗi thành viên vẫn phải xem lại `git status` trước khi push.

## 9. Chia sẻ cấu hình cho thành viên khác

Không đưa bí mật vào repository, kể cả repository private. Cách khuyến nghị:

1. Cấp một tài khoản Azure SQL riêng có quyền phù hợp cho thành viên.
2. Thêm IP của thành viên vào Azure SQL Firewall.
3. Gửi connection string qua trình quản lý mật khẩu hoặc file nén AES-256.
4. Gửi mật khẩu mở file bằng một kênh khác.
5. Để thành viên tự tạo JWT key bằng lệnh ở mục 3.

## 10. Xử lý lỗi thường gặp

### `dotnet` is not recognized

Cài .NET SDK 8, đóng PowerShell cũ và mở cửa sổ PowerShell mới.

### `ConnectionStrings:DefaultConnection must be configured`

Connection string chưa được lưu trong User Secrets. Chạy lại lệnh ở mục 3.

### `Cannot open server` hoặc lỗi Azure SQL Firewall

Kiểm tra server/database/user trong connection string và thêm IP hiện tại vào Azure SQL Firewall.

### `Invalid object name 'Users'`

Database chưa được migration. Chạy lệnh `dotnet ef database update` ở mục 5.

### Website báo `Không thể kết nối đến backend API`

Kiểm tra theo thứ tự:

1. Backend đang chạy tại `https://localhost:7081`.
2. Swagger mở được trên trình duyệt.
3. Backend không báo lỗi Azure SQL hoặc thiếu bảng.
4. Đã chạy `dotnet dev-certs https --trust`.

### `address already in use`

Cổng đang được một tiến trình khác sử dụng. Dừng cửa sổ server cũ bằng `Ctrl+C` trước khi chạy lại.

Xem thêm hướng dẫn chuyên biệt tại [backend/README.md](backend/README.md) và quy ước nhóm tại [docs/QUY_UOC_NHOM.md](docs/QUY_UOC_NHOM.md).
