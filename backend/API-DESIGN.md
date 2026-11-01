# API design — Hệ thống quản lý sinh viên tích hợp nhận diện khuôn mặt

**Trạng thái:** bản thiết kế API dựa trên báo cáo đồ án, đặc tả UC01–UC34 và ERD hiện tại. Chỉ nhóm `auth` hiện đã có source khởi đầu; các endpoint nghiệp vụ bên dưới chưa được triển khai.

## Phạm vi được chốt từ báo cáo đồ án

API phải bao phủ bốn nhóm: quản lý đào tạo và hồ sơ; điểm danh QR kết hợp nhận diện/chống giả mạo; tra cứu học tập và chuyên cần; thông báo/cảnh báo. Luồng điểm danh chính thức là: giảng viên mở phiên → hệ thống phát hành QR động có hạn → sinh viên quét QR và gửi ảnh → hệ thống kiểm tra lớp, hạn QR, chống giả mạo và đối sánh khuôn mặt → chỉ khi tất cả đạt thì ghi nhận điểm danh. Mục tiêu nhận diện là dưới 3 giây; không được tạo kết quả trùng.

## Quy ước chung

- Base URL: `/api/v1` (các endpoint auth đang có tạm dùng `/api/auth` và sẽ được chuẩn hóa khi triển khai đầy đủ).
- Tất cả request/response dùng JSON UTF-8 và thời gian ở ISO-8601 UTC.
- Access token JWT truyền bằng `Authorization: Bearer <access-token>`.
- Danh sách dùng `page`, `pageSize`, `search`, `sort`; response trả `{ "items": [], "page": 1, "pageSize": 20, "total": 0 }`.
- Mã định danh trong URL dùng giá trị `ma...` từ database. Kiểu dữ liệu chính xác (string/int/Guid) phải được chốt trong migration trước khi code.
- API không trả `matKhau`, `passwordHash`, refresh token hash hay `vectorJSON` khuôn mặt.
- `CanBoQuanLy` trong code tương ứng tác nhân **Phòng Đào tạo/Giáo vụ khoa** trong báo cáo.
- `DanhSachSinhVienLop` là enrollment/đăng ký học phần. Mọi endpoint sinh viên chỉ được truy cập enrollment của chính mình.

## Quy tắc tài khoản

`User`, `Role`, `UserRole`, `RefreshToken` là lớp xác thực chung. `SinhVien` và `GiangVien` là profile nghiệp vụ liên kết với một `User`; không nên tiếp tục lưu mật khẩu riêng trong hai bảng profile. Bốn role: `Admin`, `CanBoQuanLy`, `GiangVien`, `SinhVien`.

## Xác thực

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Đăng nhập bằng username/email và password |
| POST | `/api/auth/refresh-token` | Public | Đổi refresh token lấy cặp token mới |
| POST | `/api/auth/change-password` | Đã đăng nhập | Đổi mật khẩu và vô hiệu hóa refresh token cũ |
| POST | `/api/auth/logout` | Đã đăng nhập | Thu hồi refresh token hiện tại |
| GET | `/api/v1/me` | Đã đăng nhập | Lấy thông tin tài khoản và role hiện tại |

`POST /api/auth/login` request:

```json
{ "userNameOrEmail": "admin", "password": "<password>" }
```

Response:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-token>",
  "accessTokenExpiresAtUtc": "2026-09-18T03:30:00Z",
  "userName": "admin",
  "roles": ["Admin"]
}
```

## Ma trận use case và nhóm API

| Use case báo cáo | Nhóm endpoint |
|---|---|
| UC01–UC02 | `/api/auth/*`, `/api/v1/me`, `/api/v1/sinh-vien`, `/api/v1/giang-vien` |
| UC03–UC12 | Danh mục, lớp học phần, đăng ký học phần, `LichHoc` và `BuoiHoc` |
| UC13–UC24 | Hồ sơ khuôn mặt, phiên điểm danh, QR, check-in, kết quả điểm danh |
| UC25–UC31 | Lịch sử, tỷ lệ chuyên cần, cảnh báo, thống kê và xuất báo cáo |
| UC32–UC34 | Thông báo, lịch thi và kết quả học tập |

## Danh mục đào tạo

| Method | Endpoint | Role | Dữ liệu |
|---|---|---|---|
| GET, POST | `/api/v1/nganh-hoc` | GET: mọi role; POST: Admin, CanBoQuanLy | `NganhHoc` |
| GET, PUT, DELETE | `/api/v1/nganh-hoc/{maNganh}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `NganhHoc` |
| GET, POST | `/api/v1/khoa-tuyen-sinh` | GET: mọi role; POST: Admin, CanBoQuanLy | `KhoaTuyenSinh` |
| GET, PUT, DELETE | `/api/v1/khoa-tuyen-sinh/{maKhoa}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `KhoaTuyenSinh` |
| GET, POST | `/api/v1/mon-hoc` | GET: mọi role; POST: Admin, CanBoQuanLy | `MonHoc` |
| GET, PUT, DELETE | `/api/v1/mon-hoc/{maMonHoc}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `MonHoc` |
| GET, POST | `/api/v1/chuong-trinh-dao-tao` | GET: mọi role; POST: Admin, CanBoQuanLy | `ChuongTrinhDaoTao` |
| GET, PUT, DELETE | `/api/v1/chuong-trinh-dao-tao/{maChuongTrinh}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `ChuongTrinhDaoTao` |
| GET, POST | `/api/v1/hoc-ky` | GET: mọi role; POST: Admin, CanBoQuanLy | `HocKyThucTe` |
| GET, PUT, DELETE | `/api/v1/hoc-ky/{maHocKy}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `HocKyThucTe` |

Ví dụ `POST /api/v1/mon-hoc`:

```json
{ "maMonHoc": "IT001", "tenMonHoc": "Nhập môn lập trình", "soTinChiLyThuyet": 2, "soTinChiThucHanh": 1 }
```

Không xóa danh mục đang được lớp học phần/chương trình sử dụng; trả `409 Conflict` thay vì xóa mềm một cách ngầm định.

## Con người và phòng học

| Method | Endpoint | Role | Dữ liệu |
|---|---|---|---|
| GET, POST | `/api/v1/sinh-vien` | GET: Admin, CanBoQuanLy; POST: Admin, CanBoQuanLy | `SinhVien` |
| GET, PUT | `/api/v1/sinh-vien/{maSinhVien}` | Admin, CanBoQuanLy; SinhVien chỉ đọc/sửa profile của mình | `SinhVien` |
| GET, POST | `/api/v1/giang-vien` | GET: Admin, CanBoQuanLy; POST: Admin | `GiangVien` |
| GET, PUT | `/api/v1/giang-vien/{maGiangVien}` | Admin, CanBoQuanLy; GiangVien chỉ đọc/sửa profile của mình | `GiangVien` |
| GET, POST | `/api/v1/phong-hoc` | GET: mọi role; POST: Admin, CanBoQuanLy | `PhongHoc` |
| GET, PUT, DELETE | `/api/v1/phong-hoc/{maPhong}` | GET: mọi role; sửa/xóa: Admin, CanBoQuanLy | `PhongHoc` |

Tạo sinh viên/giảng viên phải tạo `User` tương ứng, gán đúng role và hash mật khẩu tại service; controller không nhận hoặc trả password hash.

## Lớp học phần, đăng ký và lịch học

| Method | Endpoint | Role | Dữ liệu |
|---|---|---|---|
| GET, POST | `/api/v1/lop-hoc-phan` | GET: mọi role có phạm vi phù hợp; POST: Admin, CanBoQuanLy | `LopHocPhan` |
| GET, PUT | `/api/v1/lop-hoc-phan/{maLopHocPhan}` | Admin, CanBoQuanLy; GiangVien được xem lớp mình dạy | `LopHocPhan` |
| POST | `/api/v1/lop-hoc-phan/{maLopHocPhan}/sinh-vien` | Admin, CanBoQuanLy | Thêm sinh viên (`DanhSachSinhVienLop`) |
| DELETE | `/api/v1/lop-hoc-phan/{maLopHocPhan}/sinh-vien/{maSinhVien}` | Admin, CanBoQuanLy | Hủy đăng ký |
| GET | `/api/v1/lop-hoc-phan/{maLopHocPhan}/sinh-vien` | Admin, CanBoQuanLy, GiangVien phụ trách | Danh sách lớp |
| GET, POST | `/api/v1/lop-hoc-phan/{maLopHocPhan}/lich-hoc` | GET: thành viên lớp; POST: Admin, CanBoQuanLy | `LichHoc` |
| PUT, DELETE | `/api/v1/lich-hoc/{maLichHoc}` | Admin, CanBoQuanLy | `LichHoc` |
| GET | `/api/v1/sinh-vien/{maSinhVien}/lich-hoc` | SinhVien của chính mình; CanBoQuanLy | Tra cứu `LichHoc` theo học kỳ |

Khi tạo/sửa `LichHoc`, service phải kiểm tra trùng phòng, trùng giảng viên và thời gian bắt đầu/kết thúc hợp lệ. Không dựa vào việc client tự kiểm tra. `LichHoc` là tên thực thể theo DB; không tạo bảng hay API tên `ThoiKhoaBieu`.

## Buổi học và điểm danh

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| GET, POST | `/api/v1/lich-hoc/{maLichHoc}/buoi-hoc` | GET: thành viên lớp; POST: CanBoQuanLy | Liệt kê/tạo `BuoiHoc` từ lịch học |
| GET, PUT | `/api/v1/buoi-hoc/{maBuoiHoc}` | Thành viên lớp đọc; GiangVien phụ trách/CanBoQuanLy sửa | Chi tiết buổi học |
| POST | `/api/v1/buoi-hoc/{maBuoiHoc}/phien-diem-danh` | GiangVien phụ trách, CanBoQuanLy | Mở `AttendanceSession`, đặt hạn phiên và sinh QR động |
| GET | `/api/v1/phien-diem-danh/{maPhien}/qr` | GiangVien phụ trách, CanBoQuanLy | Lấy payload/ảnh QR động cho phiên còn mở |
| POST | `/api/v1/phien-diem-danh/{maPhien}/check-in` | SinhVien thuộc lớp | Gửi QR token và ảnh camera để điểm danh |
| POST | `/api/v1/phien-diem-danh/{maPhien}/dong` | GiangVien phụ trách, CanBoQuanLy | Đóng phiên, chặn check-in mới |
| GET | `/api/v1/buoi-hoc/{maBuoiHoc}/diem-danh` | GiangVien phụ trách, CanBoQuanLy | Danh sách `ChiTietDiemDanh` |
| PUT | `/api/v1/buoi-hoc/{maBuoiHoc}/diem-danh/{maSinhVien}` | GiangVien phụ trách, CanBoQuanLy | Sửa trạng thái một sinh viên |
| POST | `/api/v1/buoi-hoc/{maBuoiHoc}/diem-danh/batch` | GiangVien phụ trách, CanBoQuanLy | Ghi điểm danh nhiều sinh viên |
| GET | `/api/v1/me/diem-danh` | SinhVien | Lịch sử và tổng vắng của chính mình |

`POST /api/v1/buoi-hoc/{maBuoiHoc}/diem-danh/batch`:

```json
{
  "records": [
    { "maSinhVien": "SV001", "trangThai": "CoMat", "thoiGianCheckIn": "2026-09-18T01:05:00Z" },
    { "maSinhVien": "SV002", "trangThai": "Vang" }
  ]
}
```

Giá trị `trangThai` nên chuẩn hóa thành enum: `CoMat`, `Muon`, `Vang`, `CoPhep`.

`POST /api/v1/phien-diem-danh/{maPhien}/check-in` dùng `multipart/form-data`:

```text
qrToken=<token-trong-QR>
image=<ảnh-camera>
capturedAtUtc=2026-09-18T01:05:00Z
```

Response thành công chỉ trả metadata:

```json
{ "attendanceId": "...", "status": "CoMat", "checkedInAtUtc": "2026-09-18T01:05:00Z" }
```

Response `422` phải phân biệt được các lý do an toàn cho client: `QrExpired`, `NotEnrolled`, `AlreadyCheckedIn`, `FaceNotMatched`, `LivenessFailed`, `ImageQualityTooLow`. Không trả vector khuôn mặt, độ tương đồng chi tiết hoặc ảnh đã lưu.

## Hồ sơ khuôn mặt

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| GET | `/api/v1/me/ho-so-khuon-mat` | SinhVien | Xem metadata và trạng thái hồ sơ của mình, không trả vector |
| POST | `/api/v1/me/ho-so-khuon-mat/dang-ky` | SinhVien | Thu ảnh camera, kiểm tra chất lượng rồi tạo FaceProfile (UC13) |
| PUT | `/api/v1/me/ho-so-khuon-mat` | SinhVien | Thay thế profile chỉ khi ảnh mới hợp lệ, nếu lỗi giữ dữ liệu cũ (UC14) |
| GET | `/api/v1/sinh-vien/{maSinhVien}/ho-so-khuon-mat` | Admin, CanBoQuanLy | Xem trạng thái hồ sơ, không trả vector |
| DELETE | `/api/v1/sinh-vien/{maSinhVien}/ho-so-khuon-mat` | Admin, CanBoQuanLy | Xóa mẫu theo yêu cầu |

Module nhận diện phải giới hạn rate, audit log và chỉ trả kết quả tối thiểu. `vectorJSON`/face embedding phải được mã hóa khi lưu; không cho mobile/web tải vector.

## Báo cáo

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| GET | `/api/v1/bao-cao/diem-danh/lop-hoc-phan/{maLopHocPhan}` | GiangVien phụ trách, CanBoQuanLy, Admin | Tỷ lệ có mặt/vắng theo lớp |
| GET | `/api/v1/bao-cao/diem-danh/sinh-vien/{maSinhVien}` | Admin, CanBoQuanLy; SinhVien của chính mình | Lịch sử và số buổi vắng |
| GET | `/api/v1/bao-cao/tong-quan` | Admin, CanBoQuanLy | Thống kê học kỳ, lớp, phòng và điểm danh |
| GET | `/api/v1/bao-cao/diem-danh/xuat` | GiangVien phụ trách, CanBoQuanLy, Admin | Xuất CSV/XLSX theo lớp/học kỳ (UC31) |

## Chuyên cần, cảnh báo và thông báo

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| GET | `/api/v1/me/chuyen-can` | SinhVien | Tỷ lệ có mặt/vắng theo học kỳ và lớp (UC27) |
| GET | `/api/v1/lop-hoc-phan/{maLopHocPhan}/chuyen-can` | GiangVien phụ trách, CanBoQuanLy | Tổng hợp chuyên cần của lớp (UC26) |
| POST | `/api/v1/lop-hoc-phan/{maLopHocPhan}/kiem-tra-canh-bao` | GiangVien phụ trách, CanBoQuanLy | Đánh giá ngưỡng vắng và tạo cảnh báo (UC28–29) |
| GET, POST | `/api/v1/thong-bao` | GET: người nhận; POST: GiangVien phụ trách, CanBoQuanLy | Xem/gửi thông báo học vụ (UC32–33) |
| GET | `/api/v1/thong-bao/{maThongBao}` | Người nhận hoặc người tạo | Chi tiết thông báo |
| POST | `/api/v1/thong-bao/{maThongBao}/da-doc` | Người nhận | Đánh dấu đã đọc |

`POST /api/v1/thong-bao`:

```json
{
  "tieuDe": "Đổi phòng học",
  "noiDung": "Buổi học ngày 2026-09-20 chuyển sang A.302.",
  "maLopHocPhan": "...",
  "hieuLucTuUtc": "2026-09-18T00:00:00Z"
}
```

Ngưỡng cảnh báo phải là cấu hình theo học kỳ/lớp; ví dụ 20% chỉ là giá trị khởi tạo theo báo cáo, không hard-code trong controller.

## Lịch thi và kết quả học tập

| Method | Endpoint | Role | Mục đích |
|---|---|---|---|
| GET | `/api/v1/me/lich-thi` | SinhVien | Tra cứu lịch thi theo học kỳ (UC34) |
| GET | `/api/v1/me/ket-qua-hoc-tap` | SinhVien | Tra cứu điểm thành phần, tổng kết và trạng thái công bố (UC34) |
| GET, POST | `/api/v1/lich-thi` | GET: CanBoQuanLy; POST: CanBoQuanLy | Quản lý lịch thi |
| GET, POST | `/api/v1/ket-qua-hoc-tap` | GET: CanBoQuanLy; POST: CanBoQuanLy | Nhập/công bố kết quả học tập |

## Mã lỗi

| Status | Khi nào dùng |
|---|---|
| 400 | Payload không hợp lệ hoặc vi phạm quy tắc nghiệp vụ |
| 401 | Không có/không hợp lệ/đã hết hạn access token |
| 403 | Có token nhưng không đúng role hoặc không thuộc lớp học phần |
| 404 | Không tồn tại resource hoặc không có quyền xem resource đó |
| 409 | Trùng mã, đăng ký trùng hoặc xóa resource đang được tham chiếu |
| 422 | Ảnh khuôn mặt không đạt điều kiện nhận diện |

## Các điểm cần chốt từ ERD trước khi code migration

1. ERD chưa có `AttendanceSession` (phiên/hạn QR/trạng thái đóng), `Notification`, `NotificationRecipient`, `ExamSchedule`, `AcademicResult`. Đây là các bảng bắt buộc để đáp ứng UC15–17, UC29, UC32–34.
2. `ChuongTrinhDaoTao` hiện chứa `maMonHoc`; nếu một chương trình có nhiều môn học, cần bảng nối `ChuongTrinhMonHoc` thay vì một khóa ngoại đơn.
3. UC08 yêu cầu kiểm tra môn tiên quyết, sĩ số và trùng lịch. Cần thêm `CoursePrerequisite` và `RegistrationWindow`, hoặc xác nhận đây là dữ liệu tích hợp từ hệ thống khác.
4. Cần xác nhận `maKhoa` trong `KhoaTuyenSinh` có ý nghĩa khóa tuyển sinh hay khoa/bộ môn để tránh tên khóa gây nhầm lẫn.
5. Cần bổ sung rõ khóa ngoại giữa `SinhVien` với `NganhHoc`, `KhoaTuyenSinh` và lớp hành chính (nếu có).
6. Cần chốt quan hệ `BuoiHoc`–`LichHoc` và `ChiTietDiemDanh`–`SinhVien`; đây là cơ sở để ràng buộc sinh viên chỉ điểm danh trong lớp đã đăng ký.
