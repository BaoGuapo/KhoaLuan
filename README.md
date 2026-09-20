# Hệ thống Quản lý Sinh viên tích hợp Nhận diện Khuôn mặt

Repository chung cho ba nền tảng của đồ án.

## Cấu trúc

```text
backend/  ASP.NET Core Web API, xử lý nghiệp vụ và truy cập Azure SQL
web/      ASP.NET Core MVC, giao diện quản trị trên trình duyệt
mobile/   Flutter, giao diện di động
docs/     Tài liệu API, sơ đồ, hướng dẫn cài đặt và quy ước nhóm
scripts/  Các script khởi tạo môi trường cục bộ
```

Chỉ `backend` được phép kết nối trực tiếp tới Azure SQL. `web` và `mobile` gọi API qua HTTPS.

## Khởi tạo dự án

Xem [hướng dẫn cài đặt](docs/SETUP.md), sau đó chạy:

```powershell
.\\scripts\\initialize.ps1
```

Không commit connection string Azure SQL, JWT secret, mật khẩu, `.env` hay `appsettings.Development.json`.
