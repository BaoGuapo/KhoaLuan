# Backend — QuanLySinhVien.Api

ASP.NET Core Web API cho hệ thống quản lý sinh viên. API là thành phần duy nhất truy cập SQL Server/Azure SQL; web và mobile chỉ gọi API qua HTTPS.

## Yêu cầu

- .NET SDK 8.0 hoặc tương thích (`dotnet --version`)
- Một Azure SQL/SQL Server đã cho phép IP của máy phát triển qua firewall

## Cấu hình bí mật

Không sửa `appsettings.example.json` để đưa thông tin thật vào Git. Từ thư mục gốc repository, cấu hình User Secrets:

```powershell
dotnet user-secrets init --project backend/QuanLySinhVien.Api
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Issuer" "QuanLySinhVien.Api"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Audience" "QuanLySinhVien.Clients"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" "<at-least-32-character-random-secret>"
```

`appsettings.Development.json` và User Secrets không được commit. Có thể đặt các cấu hình tương tự bằng environment variables khi deploy.

## Migration

Migration đầu tiên đã có trong `Migrations/`. Chỉ chạy lệnh dưới đây khi connection string hợp lệ và firewall Azure SQL đã cho phép kết nối:

```powershell
dotnet ef database update --project backend/QuanLySinhVien.Api
```

Tạo migration mới sau khi thay đổi entity:

```powershell
dotnet ef migrations add <TenMigration> --project backend/QuanLySinhVien.Api
```

## Chạy API

```powershell
dotnet restore backend/QuanLySinhVien.Api
dotnet run --project backend/QuanLySinhVien.Api
```

Khi chạy Development, Swagger ở `https://localhost:<port>/swagger`. Nhấn **Authorize** và dán access token JWT (không cần tự ghi `Bearer`).

## Tài khoản development và API auth

Sau khi migration đã được áp dụng, lần khởi động Development đầu tiên seed bốn role và bốn tài khoản kiểm thử:

| Role | Mã tài khoản | Mật khẩu mẫu |
|---|---|---|
| `Admin` | `admin` | `Admin@123` |
| `CanBoQuanLy` | `CBQL001` | `CanBo@123` |
| `GiangVien` | `GV001` | `GiangVien@123` |
| `SinhVien` | `2001230048` | `SinhVien@123` |

Không dùng các tài khoản/mật khẩu này ngoài Development; hãy đổi mật khẩu khi cần kiểm thử luồng `POST /api/auth/change-password`.

- `POST /api/auth/login`
- `POST /api/auth/change-password` (JWT bắt buộc)
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout` (JWT bắt buộc)

`GET /api/admin/ping` là endpoint kiểm thử phân quyền và chỉ role `Admin` được gọi.
