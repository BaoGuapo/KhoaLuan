# Quy ước làm việc nhóm

## Phân công thư mục

- `backend`: ASP.NET Core Web API, Entity Framework Core, Azure SQL, Swagger.
- `web`: ASP.NET Core MVC; chỉ gọi backend API, không kết nối database trực tiếp.
- `mobile`: Flutter; chỉ gọi backend API, không kết nối database trực tiếp.
- `docs`: sơ đồ, API docs, quy tắc và hướng dẫn.

## Nhánh Git

- `main`: nhánh ổn định.
- `feature/backend-*`: phát triển API.
- `feature/web-*`: phát triển website.
- `feature/mobile-*`: phát triển Flutter.

Không push trực tiếp vào `main`. Tạo pull request để cả nhóm kiểm tra trước khi merge.

## Cấu hình backend

Sau khi source backend được sinh, tạo User Secrets:

```powershell
dotnet user-secrets init --project backend/QuanLySinhVien.Api
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "<Azure SQL connection string>"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" "<JWT secret dài và ngẫu nhiên>"
```

Mọi thay đổi cấu trúc dữ liệu phải được tạo bằng Entity Framework Core migration và commit migration cùng source code.
