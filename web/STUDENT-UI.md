# Giao diện sinh viên

## Bố cục

Các trang của tài khoản SinhVien dùng một điều hướng chung: Trang chủ, Đăng ký học phần, Lịch học, Thông tin cá nhân, Hồ sơ khuôn mặt và Đổi mật khẩu. CSS `student-portal.css` chỉ áp dụng cho body `student-portal`; không đổi dashboard các vai trò khác hoặc trang đăng nhập công khai. Màu xanh–trắng, khung viền mảnh, chữ và trạng thái rõ, không dùng gradient/animation trang trí trong phần này.

- Trang chủ: thông tin hồ sơ thật, hai tác vụ chính đăng ký học phần/lịch học, các liên kết tài khoản.
- Đăng ký: lọc học kỳ + tìm mã/tên môn + chỉ lớp còn chỗ; chọn môn bên trái, xem các lớp bên phải; kết quả đăng ký nằm bên dưới. Trên mobile, lớp chuyển thành từng khối có nhãn và nút đăng ký toàn chiều rộng.
- Hết chỗ, đã đăng ký, đã chọn lớp khác cùng môn, trùng lịch: không hiện nút đăng ký; backend vẫn kiểm tra lại khi gửi yêu cầu.
- Có hộp xác nhận, chặn submit lặp trong lúc chờ và popup kết quả. Bộ lọc/học kỳ/môn được giữ sau thao tác.
- Lịch học: tuần trước/hiện tại/sau, đọc từ các lớp đã đăng ký, giới hạn theo ngày bắt đầu/kết thúc học kỳ. Đây là lịch định kỳ; chưa bao gồm thay đổi nghỉ/học bù theo từng buổi.
- Hồ sơ: nhóm trường liên hệ/cá nhân; thông tin đào tạo không bị biến thành trường tự sửa.
- Khuôn mặt: giữ nguyên camera, cách đăng ký và bảo vệ dữ liệu, đổi nội dung hướng dẫn cho người dùng dễ hiểu.

## Kết nối dữ liệu

Trang đăng ký và lịch cũ có dữ liệu hard-code và nút mô phỏng. Các trang mới dùng `StudentAcademicApiClient` phía server; không đưa JWT vào JavaScript/browser storage.

- GET `/api/v1/dang-ky-hoc-phan/danh-sach`: danh sách lớp đang mở có lịch học.
- GET `/api/v1/dang-ky-hoc-phan/da-dang-ky`: endpoint đọc mới, role SinhVien, chỉ lấy dữ liệu theo danh tính JWT hiện tại, không nhận mã sinh viên do client truyền.
- GET `/api/v1/hoc-ky`: học kỳ thực tế.
- POST `/api/v1/dang-ky-hoc-phan/sinh-vien-tu-dang-ky/{maLopHocPhan}`: đăng ký dùng nghiệp vụ hiện có.

Lỗi API hiển thị thông báo và nút thử lại, không thay bằng dữ liệu minh họa. Không bổ sung nghiệp vụ hủy/chuyển lớp hoặc giả lập học lại/cải thiện khi backend chưa hỗ trợ. Không cần migration hoặc sửa dữ liệu Azure SQL cho thay đổi giao diện này.

## Kiểm tra

```powershell
dotnet build backend/QuanLySinhVien.Api --no-restore
dotnet build web/QuanLySinhVien.Web --no-restore
dotnet run --project backend/QuanLySinhVien.Checks -- --student-smoke
dotnet run --project backend/QuanLySinhVien.Checks -- --web-smoke
node --test web/QuanLySinhVien.Web/tests/face-capture.test.cjs
```

Student smoke kiểm tra render, trạng thái lớp, lọc/không có kết quả, POST đăng ký, kết quả sau đăng ký, antiforgery, điều hướng, hồ sơ và lỗi API trên host Kestrel với fixture trong memory. Không thay đổi xác thực của ứng dụng thật và không ghi DB. `--db-read` kiểm tra thêm hai truy vấn đọc với hồ sơ demo nếu đã có.

Để kiểm tra trình duyệt bằng dữ liệu giả:

```powershell
dotnet run --project backend/QuanLySinhVien.Checks -- --student-preview
```

Host riêng: `http://127.0.0.1:7094/SinhVien/DangKyHocPhan`. Khung 390px: `/mobile-preview`. Host chỉ dành cho kiểm tra local, dừng bằng Ctrl+C; không dùng để deploy.
