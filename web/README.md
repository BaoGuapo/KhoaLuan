# QuanLySinhVien.Web

ASP.NET Core MVC cho nội dung tuần 6: đăng nhập, đăng xuất, đổi mật khẩu và phân quyền người dùng. Web không truy cập database; mọi thao tác xác thực đều gọi backend API.

## Chạy ứng dụng

Chạy backend trước tại `https://localhost:7081`, sau đó từ thư mục gốc repository chạy:

```powershell
dotnet dev-certs https --trust
dotnet run --project web/QuanLySinhVien.Web
```

Mở `https://localhost:7082`. URL backend nằm tại `QuanLySinhVien.Web/appsettings.json` và không chứa secret.

JWT access token và refresh token được giữ trong session phía server; cookie đăng nhập chỉ chứa tên người dùng và role. Trang `/Home/Admin` minh họa phân quyền `[Authorize(Roles = "Admin")]`.

## Phạm vi tuần 6 theo đề cương

- Đăng nhập bằng mã sinh viên, mã giảng viên hoặc mã tài khoản quản trị/cán bộ.
- Đăng xuất và thu hồi refresh token.
- Đổi mật khẩu; sau khi đổi phải đăng nhập lại.
- Phân quyền bốn role: `Admin`, `CanBoQuanLy`, `GiangVien`, `SinhVien`.
- Điều hướng tới dashboard tương ứng và chặn truy cập chéo bằng authorization policy.

Các nghiệp vụ quản lý hồ sơ, lớp học, lịch học, khuôn mặt, điểm danh, báo cáo và thông báo thuộc các tuần triển khai chức năng website tiếp theo, không nằm trong phạm vi tuần 6.

## Tài khoản kiểm thử Development

Các tài khoản này chỉ được backend seed khi chạy môi trường `Development` và database đã áp dụng migration:

| Role | Mã tài khoản | Mật khẩu mẫu |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| CanBoQuanLy | `CBQL001` | `CanBo@123` |
| GiangVien | `GV001` | `GiangVien@123` |
| SinhVien | `2001230048` | `SinhVien@123` |

Không sử dụng các mật khẩu này ngoài môi trường phát triển.
