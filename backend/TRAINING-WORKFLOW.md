# Chương trình đào tạo → áp dụng học kỳ → mở lớp → phân công

## Màn hình web

### Dữ liệu demo có sẵn (chỉ Development)

Backend tự seed sau khi xác nhận DB kết nối và không còn migration pending. Seed nằm trong `Data/DevelopmentTrainingSeed.cs`, chạy theo transaction và khóa riêng để tránh tạo trùng khi nhiều instance khởi động. Chạy lại không reset mật khẩu, hồ sơ, trạng thái lớp hoặc đăng ký đã thao tác. Production không seed. Dữ liệu thật không bị ghi đè.

| Loại | Mã demo |
|---|---|
| Ngành | `DEMO-CNTT` – Công nghệ thông tin (Demo) |
| Khóa | `DEMO-K2026` – 2026–2030 |
| Học kỳ | `DEMO-HK1-2026` – 07/09/2026–27/12/2026 |
| Môn bắt buộc HK1 | `DEMO-LTCB` – Lập trình cơ bản |
| Môn tự chọn HK1 | `DEMO-TINHOC` – Tin học ứng dụng |
| Phòng | `DEMO-P101` |
| Giảng viên đúng ngành | `DEMO-GV01` |
| Sinh viên đúng ngành/khóa | `DEMO-SV01` |

Hai tài khoản mới `DEMO-GV01` và `DEMO-SV01` dùng mật khẩu ban đầu **`Demo@12345`**, chỉ dành cho Development và bắt buộc đổi mật khẩu lần đầu. Mật khẩu đã đổi sẽ không bị reset khi seed lại. Không seed mẫu khuôn mặt/ảnh sinh trắc học.

Có sẵn chương trình **CNTT khóa 2026 – chương trình Demo**, đợt áp dụng HK1 và 8 lớp `DEMO-LTCB-01`…`DEMO-LTCB-08`:

- `-01`: đã phân công, thứ 2 tiết 1–3, đang mở đăng ký. Sinh viên demo chưa được đăng ký sẵn để có thể demo thao tác đăng ký.
- `-02`: đã phân công, thứ 3 tiết 1–3, chưa mở đăng ký. Dùng để demo nút mở đăng ký rồi thử đăng ký trùng môn.
- `-03`…`-08`: chờ phân công. Chọn `DEMO-GV01`, `DEMO-P101`, thứ 2 tiết 1–3 để demo chặn trùng lịch; đổi sang thứ 4 tiết 1–3 để lưu thành công.

Đăng nhập CBQL/Admin hiện có → vào `/dao-tao` → chọn đợt chương trình Demo. Không dùng lại tiền tố `DEMO-LTCB` để mở thêm vì 8 mã đã tồn tại; nếu cần trình diễn mở hàng loạt, dùng tiền tố mới như `DEMO-LTCB-DOT2`.

Để kiểm tra seed và tính chạy lại an toàn:

```powershell
dotnet run --project backend/QuanLySinhVien.Checks -- --demo-seed
```

Đăng nhập Admin hoặc CanBoQuanLy, chọn **Đào tạo** trên thanh điều hướng hoặc vào `/dao-tao`.

1. Danh mục: tạo ngành, khóa tuyển sinh, học kỳ thực tế nếu chưa có. Môn học dùng danh mục hiện có tại `/CanBoQuanLy/QuanLyMonHoc`; phòng học dùng danh mục phòng hiện có.
2. Nhập chương trình: chọn ngành + khóa tuyển sinh, nhập khoa chuyên môn và tên chương trình. Thêm môn, thứ tự học kỳ (1–12), bắt buộc/tự chọn. Một môn chỉ xuất hiện một lần; mỗi ngành/khóa có một chương trình trong phiên bản này.
3. Excel: tải mẫu `.xlsx`, thay dòng ví dụ bằng mã môn có sẵn. Ba cột `MaMonHoc`, `ThuTuHocKy`, `BatBuoc` (1=bắt buộc, 0=tự chọn). Chọn ngành/khóa/khoa/tên chương trình trên form, tải file và bấm **Xem trước Excel**. Kiểm tra lỗi, sửa trên form hoặc tải file đã sửa, rồi bấm **Lưu chương trình**. Xem trước không ghi database. Không hỗ trợ macro/công thức; tối đa 2 MB và 200 môn.
4. Áp dụng học kỳ: chọn chương trình, thứ tự học kỳ trong chương trình và học kỳ thực tế. Ví dụ: học kỳ dự kiến 1 → HK1-2026. Không áp dụng một chương trình hai lần cho cùng học kỳ thực tế.
5. Mở lớp: chọn đợt áp dụng và môn, nhập tiền tố, số lớp 1–50 và sĩ số 1–500. Ví dụ tiền tố `LTCB-K26-HK1`, số lớp `8` → `LTCB-K26-HK1-01`…`-08`, tất cả cùng môn. Các lớp ban đầu chờ phân công; không cho sinh viên đăng ký. Có thể mở thêm đợt với tiền tố khác, không cố định 8 lớp.
6. Phân công từng lớp: chọn giảng viên đúng ngành, phòng, thứ và tiết. Chuyên ngành giảng viên phải khớp mã hoặc tên ngành. Kiểm tra trùng phòng/giảng viên trong học kỳ; tạo lịch học và các buổi học theo khoảng ngày học kỳ. Lớp vẫn đóng đăng ký.
7. Bấm **Mở cho sinh viên đăng ký** khi lớp đã phân công. Sinh viên chọn một lớp của môn trong học kỳ, không đăng ký trùng môn, trùng lịch hoặc vượt sĩ số.

Hồ sơ sinh viên phải có `Nganh` là mã/tên ngành và `KhoaHoc` là mã khóa tuyển sinh hoặc năm bắt đầu. Với lớp mở bằng luồng mới, danh sách và API đăng ký đều kiểm tra ngành/khóa. Lớp legacy chưa gắn đợt áp dụng vẫn được giữ, không tự gán ngành/khóa khi chưa có thông tin chắc chắn.

Đóng học phí và điểm danh thuộc các bước tiếp theo của quy trình 6 bước, không thêm thanh toán thật trong thay đổi này.

## API (JWT Admin/CanBoQuanLy)

| Method | Đường dẫn | Công dụng |
|---|---|---|
| GET | `/api/v1/dao-tao/danh-muc` | Ngành, khóa, môn, học kỳ, giảng viên, phòng |
| POST | `/api/v1/dao-tao/nganh-hoc` | `{maNganh,tenNganh}` |
| POST | `/api/v1/dao-tao/khoa-tuyen-sinh` | `{maKhoa,namBatDau,namKetThuc}` |
| POST | `/api/v1/dao-tao/hoc-ky` | `{maHocKy,tenHocKy,ngayBatDau,ngayKetThuc}` |
| GET/POST | `/api/v1/dao-tao/chuong-trinh` | Danh sách/tạo chương trình |
| GET | `/api/v1/dao-tao/chuong-trinh/mau-excel` | Tải mẫu XLSX |
| POST | `/api/v1/dao-tao/chuong-trinh/xem-truoc-excel` | Multipart field `file`, trả môn và lỗi, không lưu |
| GET/POST | `/api/v1/dao-tao/ap-dung-hoc-ky` | Danh sách/áp dụng chương trình |
| POST | `/api/v1/dao-tao/ap-dung-hoc-ky/{id}/mo-lop` | Mở nhiều lớp cùng môn |
| POST | `/api/v1/dao-tao/lop-hoc-phan/{code}/phan-cong` | Gán giảng viên + tạo lịch học |
| POST | `/api/v1/dao-tao/lop-hoc-phan/{code}/mo-dang-ky` | Mở đăng ký sau phân công |

Ví dụ tạo chương trình (thay bằng mã đã tồn tại):

```json
{
  "maNganh": "CNTT",
  "maKhoa": "K2026",
  "tenKhoaDaoTao": "Khoa Công nghệ thông tin",
  "tenChuongTrinh": "CNTT khóa 2026",
  "monHocs": [{"maMonHoc": "LTCB", "thuTuHocKy": 1, "batBuoc": true}]
}
```

Áp dụng: `{ "keHoachDaoTaoId": "<GUID trả về khi tạo chương trình>", "maHocKy": "HK1-2026", "thuTuHocKy": 1 }`.

Mở lớp: `{ "maMonHoc": "LTCB", "tienToMaLop": "LTCB-K26-HK1", "soLop": 8, "siSoToiDa": 40 }`.

Phân công: `{ "maGiangVien": "GV001", "maPhong": "P101", "thuTrongTuan": 2, "tietBatDau": 1, "tietKetThuc": 3 }`. Thứ 2–7, Chủ nhật=8, tiết 1–15. Khoảng ngày học kỳ tối đa 366 ngày.

API sinh viên dùng endpoint hiện có `/api/v1/dang-ky-hoc-phan/sinh-vien-tu-dang-ky/{maLopHocPhan}`; cả đăng ký qua học vụ và tự đăng ký dùng chung kiểm tra với transaction Serializable để bảo vệ sĩ số khi có nhiều request.

## Database và tương thích

- Giữ nguyên `NganhHoc`, `KhoaTuyenSinh`, `ChuongTrinhDaoTao`, các hồ sơ và lớp học phần đã có. `MaKhoa` của CTĐT là **khóa tuyển sinh**, không phải khoa chuyên môn.
- Bổ sung `KeHoachDaoTao`, `MonKeHoachDaoTao`, `ApDungHocKy`, `LopHocPhanDuKien`. Cần bảng dự kiến vì `LopHocPhan.maGiangVien` trong DB gốc bắt buộc có giá trị, không dùng giảng viên giả để mở lớp.
- Lưu chương trình đồng bộ các dòng môn vào `ChuongTrinhDaoTao` trong cùng transaction; không xóa/ghi đè dòng legacy. Các chương trình legacy chưa có tiêu đề/khoa/loại môn cần nhập qua form để áp dụng luồng mới.
- Mapping legacy đã đối chiếu SQL: `LoaiHocKy`, `TrangThai` lớp là tinyint; buổi học `0=ChuaDienRa`, `1=DangDienRa`, `2=DaChot`; cảnh báo `0=BinhThuong`, `1=CanhBao`, `2=CamThi`. API vẫn trả tên trạng thái tương thích UI. `ThoiGianDiemDanh` map tới cột `thoiGianMoDiemDanh`; ngày học kỳ có thể null và phải được điền trước phân công.
- Migration `20261011105226_TrainingWorkflow` chỉ thêm 4 bảng/khóa ngoại/chỉ mục. Các thay đổi mapping không ALTER, rename hay xóa cột legacy và không thêm lại `MustChangePassword` đã có. Script để kiểm tra: `QuanLySinhVien.Api/Migrations/TrainingWorkflow.review.sql`.
- Migration này dành cho DB legacy theo schema dự án, không phải script dựng một Azure DB trống từ đầu.

```powershell
dotnet ef database update TrainingWorkflow --project backend/QuanLySinhVien.Api
dotnet run --project backend/QuanLySinhVien.Api
dotnet run --project web/QuanLySinhVien.Web
```

Kết nối vẫn qua `ConnectionStrings:DefaultConnection` trong User Secrets; không lưu secret vào Git.

## Kiểm tra

```powershell
dotnet run --project backend/QuanLySinhVien.Checks
dotnet run --project backend/QuanLySinhVien.Checks -- --db-read
dotnet run --project backend/QuanLySinhVien.Checks -- --web-smoke
# Tạo fixture mã T-<ngẫu nhiên>, kiểm tra luồng rồi dọn đúng dữ liệu test trong finally:
dotnet run --project backend/QuanLySinhVien.Checks -- --db-test
```

Kiểm tra DB dùng cấu hình User Secrets của API trên Windows, không in secret hay hồ sơ cá nhân. Kiểm tra offline không cần kết nối SQL.
