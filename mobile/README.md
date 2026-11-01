<<<<<<< HEAD
# Mobile

`quan_ly_sinh_vien_app` sẽ là ứng dụng Flutter. Ứng dụng chỉ gọi backend qua HTTPS; không truy cập Azure SQL trực tiếp.
=======
# Mobile Flutter

Ứng dụng Flutter nằm tại `mobile/quan_ly_sinh_vien_app`. Mobile chỉ gọi backend
ASP.NET Core Web API, không kết nối trực tiếp Azure SQL và không chứa connection
string, JWT secret hoặc mật khẩu người dùng.

## Phạm vi đã triển khai

- Đăng nhập bằng mã sinh viên, mã giảng viên hoặc mã tài khoản Admin/CBQL.
- Gọi `POST /api/auth/login` và đọc JWT, refresh token, role.
- Lưu phiên bằng `flutter_secure_storage`; không lưu mật khẩu.
- Tự làm mới access token qua `POST /api/auth/refresh-token` khi mở lại ứng dụng.
- Bắt buộc đổi mật khẩu tạm ở lần đăng nhập đầu tiên.
- Gọi `POST /api/auth/change-password`, xóa phiên cũ và yêu cầu đăng nhập lại.
- Đăng xuất qua `POST /api/auth/logout`; vẫn xóa phiên cục bộ khi mất mạng.
- Điều hướng màn hình theo bốn role: `Admin`, `CanBoQuanLy`, `GiangVien`, `SinhVien`.

## 1. Cài môi trường

Cài Flutter SDK stable và Android Studio, sau đó mở terminal mới:

```powershell
flutter doctor
```

Máy hiện tại chưa có Flutter SDK khi source được tạo, vì vậy cần sinh các thư mục
platform chuẩn một lần. Từ thư mục gốc repository chạy:

```powershell
.\mobile\quan_ly_sinh_vien_app\tool\bootstrap_platforms.ps1
```

Script dùng `flutter create` để tạo Android (và iOS khi chạy trên macOS), giữ nguyên
toàn bộ source trong `lib/`, đặt Android `minSdk = 23`, chỉ bật HTTP cleartext cho
bản Android debug, chạy `flutter pub get` và format mã nguồn.

## 2. Chạy backend cho Android Emulator

Backend phải chạy trước:

```powershell
dotnet run --project backend/QuanLySinhVien.Api
```

Trong Development, backend có HTTP tại `http://localhost:5081`. Android Emulator
dùng địa chỉ đặc biệt `10.0.2.2` để truy cập localhost của máy, nên có thể chạy app:

```powershell
cd mobile/quan_ly_sinh_vien_app
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5081/
```

Đây cũng là URL mặc định nếu không truyền `API_BASE_URL`.

## 3. Chạy trên thiết bị Android thật

Điện thoại và máy tính phải cùng mạng Wi-Fi. Lấy IPv4 LAN của máy tính bằng
`ipconfig`, cho phép cổng `5081` qua Windows Firewall, rồi chạy backend lắng nghe
trên mạng LAN:

```powershell
dotnet run --project backend/QuanLySinhVien.Api --urls "http://0.0.0.0:5081"
flutter run --dart-define=API_BASE_URL=http://<IP-LAN-CUA-MAY>:5081/
```

Chỉ dùng HTTP cho Development trong mạng nội bộ. Khi triển khai thật, truyền URL
HTTPS public của backend:

```powershell
flutter run --dart-define=API_BASE_URL=https://api.example.com/
```

## 4. Kiểm tra

```powershell
cd mobile/quan_ly_sinh_vien_app
flutter pub get
flutter analyze
flutter test
```

Các tài khoản Development được ghi trong README gốc. Tài khoản đã tồn tại trước
migration có `MustChangePassword = false`; để kiểm thử đổi mật khẩu lần đầu, hãy
cấp một tài khoản sinh viên hoặc giảng viên mới qua Swagger.
>>>>>>> origin/main
