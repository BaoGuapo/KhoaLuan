# Bộ dữ liệu demo

Lệnh dưới đây **xoá toàn bộ dữ liệu** của database phát triển (giữ lại lịch sử migration và bảng vai trò) rồi nạp bộ dữ liệu demo. Chỉ chạy trên database của riêng mình, không chạy trên Azure SQL (lệnh tự từ chối). Phải gõ lại đúng tên database sau `--confirm`.

```powershell
dotnet ef database update --project backend/QuanLySinhVien.Api
dotnet run --project backend/QuanLySinhVien.Checks -- --reset-demo-data --confirm <TenDatabase>
dotnet user-secrets set "DevelopmentSeed:TrainingDemo" "false" --project backend/QuanLySinhVien.Api
```

Lệnh cuối tắt bộ mẫu `DEMO-*` mà backend tự chèn khi khởi động ở Development, để không trộn với bộ dữ liệu này. Nên sao lưu database trước khi chạy.

## Nội dung

| Hạng mục | Dữ liệu |
|---|---|
| Ngành | CNTT – Công nghệ thông tin, QTKD – Quản trị kinh doanh (khóa K2024) |
| Học kỳ | HK1-2526, HK2-2526 (đã kết thúc, có điểm đã công bố); HK1-2627 (đang học, điểm danh đã chốt tới hôm trước ngày nạp, chưa có điểm); HK2-2627 (sắp tới: đợt đăng ký 04–17/01/2027, 5 lớp chờ phân công) |
| Môn học | 19 môn, có kế hoạch đào tạo theo học kỳ và điều kiện tiên quyết/học trước |
| Lớp học phần | 25 lớp có lịch, phòng, buổi học hằng tuần, không trùng giờ giảng viên, phòng hay lớp sinh hoạt |
| Điểm danh | Các buổi đã qua đều đã chốt; khoảng 96% có mặt, một số sinh viên bị cảnh báo hoặc cấm thi |
| Thông báo | 3 thông báo chung, 1 thông báo theo lớp |

Hồ sơ khuôn mặt và ảnh thẻ không tạo giả được. Lệnh reset chuyển các hồ sơ có sẵn theo danh sách `FaceTransfers` trong `backend/QuanLySinhVien.Checks/DemoDataSeed.cs`, và dừng lại nếu database còn hồ sơ khuôn mặt ngoài danh sách đó.

## Tài khoản

Mọi tài khoản dùng mật khẩu `Demo@12345`, không bắt đổi ở lần đăng nhập đầu.

| Vai trò | Tài khoản |
|---|---|
| Admin | `admin` |
| Cán bộ quản lý | `CBQL001`, `CBQL002` |
| Giảng viên CNTT | `GV001` (dạy 2 lớp Lập trình di động HK1-2627), `GV_CNTT01` – `GV_CNTT05` |
| Giảng viên QTKD | `GV_QTKD01` – `GV_QTKD05`, `GV_MKT01` – `GV_MKT10` |
| Sinh viên CNTT | `2001240001` – `2001240040` (lớp 16DHTH01, 16DHTH02), `2001230048` |
| Sinh viên QTKD | `2013240001` – `2013240020` (lớp 16DHQT01) |
