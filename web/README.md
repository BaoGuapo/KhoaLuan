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
- Tài khoản mới được chuyển bắt buộc tới trang đổi mật khẩu ở lần đăng nhập đầu tiên;
  các trang chức năng bị chặn cho đến khi đổi mật khẩu tạm thành công.
- Phân quyền bốn role: `Admin`, `CanBoQuanLy`, `GiangVien`, `SinhVien`.
- Điều hướng tới dashboard tương ứng và chặn truy cập chéo bằng authorization policy.
- Quản lý tài khoản: tìm kiếm/lọc, khóa/mở khóa, cấp lại mật khẩu tạm và phân quyền tài khoản nội bộ.
- Quản lý hồ sơ sinh viên/giảng viên theo quy trình tạo hồ sơ trước, cấp tài khoản sau.
- Xem và cập nhật thông tin cá nhân cho mọi vai trò.
- Lọc danh sách giảng viên theo khoa/chuyên ngành và mở trang phân công giảng dạy.
- Phân công môn thuộc ngành của giảng viên, xếp lịch theo học kỳ/phòng và kiểm tra trùng lịch.
- Giảng viên xem lớp phụ trách, điểm danh theo buổi, chốt điểm danh và theo dõi tổng hợp chuyên cần.

Trang quản lý người dùng dành cho Admin/CBQL:

```text
https://localhost:7082/quan-ly-nguoi-dung
```

Admin có thể quản lý toàn bộ tài khoản. CBQL chỉ quản lý sinh viên và giảng viên.
Trang thông tin cá nhân của người dùng ở `/thong-tin-ca-nhan`.

Các trang nghiệp vụ giảng viên:

```text
https://localhost:7082/quan-ly-nguoi-dung/giang-vien
https://localhost:7082/quan-ly-nguoi-dung/giang-vien/{maGiangVien}/phan-cong
https://localhost:7082/GiangVien/LichGiangDay
```

## Nhận diện khuôn mặt Tuần 8

Sau khi nhập đúng mã sinh viên và mật khẩu, tài khoản đã đăng ký khuôn mặt được
chuyển sang `/tai-khoan/xac-thuc-khuon-mat`. Website chưa cấp cookie đăng nhập
cho đến khi ba mẫu quét khớp hồ sơ; phiên chờ hết hạn sau 5 phút. Tài khoản
chưa đăng ký mặt tiếp tục đăng nhập như trước. Bước này chỉ áp dụng trên web;
Flutter và API đăng nhập bằng mật khẩu chưa bị chặn, nên không xem đây là MFA
chống giả mạo hoặc hàng rào bảo mật cho toàn hệ thống.
Trang xác thực tự tải mô hình, xin quyền camera và quét khi mở; người dùng chỉ
cần bấm **Thử quét lại** nếu camera/model lỗi hoặc lần đối chiếu trước thất bại.
Sau khi khớp, website chuyển vào trang đích và hiện thông báo thành công dạng popup.

- Sinh viên mở `/khuon-mat`, bật camera rồi đăng ký hoặc cập nhật mẫu.
- Giảng viên mở lớp phụ trách, chọn buổi chưa chốt, nhấn **Điểm danh bằng khuôn mặt**.
- Camera lấy ba mẫu; Tiny Face Detector xác định một khuôn mặt, mốc 68 điểm căn chỉnh
  ảnh 112×112; MobileNetV2 đã huấn luyện cho face recognition trích xuất embedding
  512 chiều. Website chỉ gửi embedding đến backend, không lưu ảnh camera.
- Backend chỉ so khớp với sinh viên đã đăng ký lớp và ghi kết quả theo buổi học.

Các script, trọng số và mô hình ONNX được phục vụ cục bộ trong `wwwroot`; nguồn và
checksum ở `docs/FACE_MODELS.md`. Camera trình duyệt cần HTTPS hoặc localhost.
Đây là quy trình demo có giảng viên giám sát: chưa có kiểm tra người thật/chống ảnh
chụp (liveness) và chưa được hiệu chỉnh ngưỡng trên tập dữ liệu của trường.
Các nghiệp vụ thông báo và báo cáo mở rộng thuộc các tuần triển khai tiếp theo.

## Tài khoản kiểm thử Development

Các tài khoản này chỉ được backend seed khi chạy môi trường `Development` và database đã áp dụng migration:

| Role | Mã tài khoản | Mật khẩu mẫu |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| CanBoQuanLy | `CBQL001` | `CanBo@123` |
| GiangVien | `GV001` | `GiangVien@123` |
| SinhVien | `2001230048` | `SinhVien@123` |

Không sử dụng các mật khẩu này ngoài môi trường phát triển.

## Chương trình đào tạo, mở lớp và phân công

Admin/CanBoQuanLy chọn **Đào tạo** hoặc vào `/dao-tao`. Luồng: nhập chương trình (form hoặc Excel xem trước) → áp dụng học kỳ → mở nhiều lớp cùng môn → phân công giảng viên/lịch học → mở đăng ký. Hướng dẫn đầy đủ và migration tại [backend/TRAINING-WORKFLOW.md](../backend/TRAINING-WORKFLOW.md).

Development có sẵn ngành `DEMO-CNTT`, khóa `DEMO-K2026`, chương trình HK1 và 8 lớp cùng môn `DEMO-LTCB-01`…`-08`. Lớp 01 mở đăng ký, lớp 02 đã phân công nhưng đóng đăng ký, lớp 03–08 chờ phân công. Tài khoản `DEMO-GV01` và `DEMO-SV01` dùng mật khẩu ban đầu `Demo@12345`, bắt buộc đổi lần đầu; seed không reset mật khẩu đã đổi. Xem kịch bản chi tiết trong hướng dẫn trên.
