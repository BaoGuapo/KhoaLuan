# Đăng ký học phần

Căn cứ đối chiếu: [Quy chế HUIT, QĐ 3344/QĐ-DCT ngày 05/09/2025](https://doc.huit.edu.vn/images/documents/n00ct/2025-qcdt-dai-hoc-05092025-final-20250909042553-e.pdf), Điều 6 và Điều 21. `vuot` là nhãn giao diện cho môn chưa học nằm sau tiến độ hiện tại trong chương trình; không tự đặt ngưỡng GPA riêng cho nhãn này.

## Luồng và dữ liệu

- `StudentRegistrationRules` được dùng cho cả danh sách và kiểm tra trong transaction ghi đăng ký. Danh sách gợi ý không thay thế kiểm tra lúc POST.
- Đợt mở phù hợp chương trình quyết định học kỳ đăng ký mặc định; khi chưa có đợt, giao diện dùng học kỳ chứa ngày hiện tại tại Việt Nam. Không chuyển sang kỳ cũ chỉ vì kỳ đó có dữ liệu.
- `HoSoHocVu`: tiến độ, GPA hệ 4 do học vụ xác nhận, cờ học kỳ đầu/cuối. GPA không được suy ra từ một phần bảng điểm.
- `KetQuaHocPhan`: một bản ghi sinh viên/môn/học kỳ; chỉ kết quả đã công bố của kỳ đã kết thúc mới dùng phân loại. Giữ lịch sử các kỳ; kết quả cao nhất trước đó hiển thị trên giao diện.
- `DieuKienHocPhan`: quan hệ `tienquyet`, `hoctruoc`, `songhanh` theo chương trình.
- `DotDangKyHocPhan`: giờ UTC, phạm vi chương trình (null là toàn trường), cờ cho đăng ký và cờ học kỳ hè. Khoảng thời gian là `[mở, đóng)`.
- `DanhSachSinhVienLop.LoaiDangKy`: lưu `moi`, `lai`, `caithien`, `vuot` tại thời điểm ghi nhận.
- Thiếu hồ sơ học vụ/GPA bắt buộc thì hiển thị lý do và từ chối đăng ký; không tạo điều kiện học tập giả. Khối lượng tối thiểu là nhắc nhở khi thêm từng môn, không chặn môn đầu tiên; chưa có bước chốt đợt đăng ký.

## API

Sinh viên:

```text
GET  /api/v1/dang-ky-hoc-phan/danh-sach?loaiDangKy=moi&hocKy=<mã>
POST /api/v1/dang-ky-hoc-phan/sinh-vien-tu-dang-ky/<lớp>?loaiDangKy=vuot
GET  /api/v1/dang-ky-hoc-phan/da-dang-ky
```

Admin/Cán bộ quản lý nhập cấu hình/kết quả qua Swagger (cần JWT của đúng vai trò):

```text
GET /api/v1/dao-tao/dang-ky/dot
PUT /api/v1/dao-tao/dang-ky/dot/<guid>
    {tenDot, maHocKy, keHoachDaoTaoId, moLuc, dongLuc, choPhepDangKy, laHocKyHe}
PUT /api/v1/dao-tao/dang-ky/hoc-vu/<sinh-vien>
    {thuTuHocKy, gpaTichLuy, laHocKyDau, laHocKyCuoi}
PUT /api/v1/dao-tao/dang-ky/ket-qua/<sinh-vien>/<môn>/<học-kỳ>
    {diemChu, daCongBo}
PUT /api/v1/dao-tao/dang-ky/dieu-kien/<chương-trình-guid>/<môn>/<môn-điều-kiện>
    {loai}
```

`moLuc`, `dongLuc` phải gửi kèm offset, ví dụ `2026-10-19T08:00:00+07:00`. Đợt đăng ký do học vụ cấu hình theo thông báo thực tế; không lấy ngày đầu học kỳ làm ngày mở đăng ký. Màn hình Đào tạo cho mở bất kỳ môn thuộc chương trình vào học kỳ thực tế để phục vụ các loại đăng ký.

## Cập nhật schema và chạy thử

Migration bổ sung: `20261018191740_CourseRegistrationRules`. Chỉ thêm bốn bảng và cột loại đăng ký, không tự chạy migration khi API khởi động. Trước khi dùng mã mới với một DB, cần nâng cấp schema và nhập cấu hình học vụ.

Database có lịch sử/schema khác phải đối chiếu kiểu và độ dài khóa ngoại trước khi áp dụng; không ghi giả lịch sử các migration cũ. Bản cá nhân dùng script nâng cấp riêng, được kiểm tra marker và chỉ chạy trên LocalDB cố định. Azure SQL chưa được thay đổi bởi cập nhật này.

```powershell
dotnet run --project backend/QuanLySinhVien.Checks
dotnet run --project backend/QuanLySinhVien.Checks -- --student-smoke
```

Các kiểm tra trên dùng dữ liệu trong bộ nhớ/API giả. Luồng thật được kiểm tra trên database cá nhân bằng `.local-personal/check-registration.ps1`; file này không đưa lên Git. Dữ liệu cá nhân chứa kết quả F/C và đợt thực hành giả lập để kiểm tra đủ bốn loại; đây không phải lịch đăng ký thật của HUIT.

Phạm vi hiện tại là hệ đại học chính quy; chưa triển khai xác nhận học phí, học phần tương đương/thay thế hoặc chốt toàn bộ khối lượng đăng ký. Không tuyên bố toàn bộ quy chế đã được tự động hóa.
