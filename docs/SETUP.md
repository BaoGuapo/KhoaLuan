# Thiết lập môi trường cục bộ

## Yêu cầu

- .NET SDK (khuyến nghị bản LTS hiện hành)
- Flutter SDK (stable channel) và thiết bị giả lập hoặc thiết bị thật
- Git
- Azure SQL hoặc SQL Server dùng cho môi trường phát triển

Kiểm tra SDK sau khi cài đặt:

```powershell
dotnet --version
flutter doctor
```

## Sinh source ban đầu

Từ thư mục gốc repository, chạy:

```powershell
.\\scripts\\initialize.ps1
```

Script không ghi đè dự án đã tồn tại. Nó tạo solution, Web API, MVC và ứng dụng Flutter theo đúng cấu trúc đã quy ước.

## Cấu hình bí mật backend

Sau khi chạy script, sao chép `backend/QuanLySinhVien.Api/appsettings.example.json` thành `appsettings.Development.json`, hoặc ưu tiên dùng User Secrets. Không commit các giá trị thật.

```powershell
dotnet user-secrets init --project backend/QuanLySinhVien.Api
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "<connection string>"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" "<random secret>"
```
