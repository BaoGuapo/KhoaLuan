# Backend — QuanLySinhVien.Api

Điều kiện đăng ký học phần và cấu hình đợt/kết quả học tập: [hướng dẫn cập nhật](../docs/DANG_KY_HOC_PHAN.md). Dữ liệu demo đào tạo cần hồ sơ học vụ và đợt đăng ký phù hợp trước khi sinh viên tự đăng ký.

ASP.NET Core Web API cho hệ thống quản lý sinh viên. API là thành phần duy nhất truy cập SQL Server/Azure SQL; web và mobile chỉ gọi API qua HTTPS.

## Yêu cầu

- .NET SDK 8.0 hoặc tương thích (`dotnet --version`)
- Một Azure SQL/SQL Server đã cho phép IP của máy phát triển qua firewall

## Cấu hình bí mật

Không sửa `appsettings.example.json` để đưa thông tin thật vào Git. Từ thư mục gốc repository, cấu hình User Secrets:

```powershell
dotnet user-secrets init --project backend/QuanLySinhVien.Api
dotnet user-secrets set --project backend/QuanLySinhVien.Api "ConnectionStrings:DefaultConnection" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Issuer" "QuanLySinhVien.Api"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Audience" "QuanLySinhVien.Clients"
dotnet user-secrets set --project backend/QuanLySinhVien.Api "Jwt:Key" "<at-least-32-character-random-secret>"
```

`appsettings.Development.json` và User Secrets không được commit. Có thể đặt các cấu hình tương tự bằng environment variables khi deploy.

## Migration

Migration đầu tiên đã có trong `Migrations/`. Chỉ chạy lệnh dưới đây khi connection string hợp lệ và firewall Azure SQL đã cho phép kết nối:

```powershell
dotnet ef database update --project backend/QuanLySinhVien.Api
```

Tạo migration mới sau khi thay đổi entity:

```powershell
dotnet ef migrations add <TenMigration> --project backend/QuanLySinhVien.Api
```

## Chạy API

```powershell
dotnet restore backend/QuanLySinhVien.Api
dotnet run --project backend/QuanLySinhVien.Api
```

Khi chạy Development, Swagger ở `https://localhost:<port>/swagger`. Nhấn **Authorize** và dán access token JWT (không cần tự ghi `Bearer`).

Backend Development đồng thời nghe HTTP tại `http://localhost:5081` để Android
Emulator truy cập qua `http://10.0.2.2:5081`. HTTPS redirection chỉ bị tắt trong
Development; môi trường production vẫn bắt buộc HTTPS. Không dùng HTTP khi triển
khai ra Internet.

## Tài khoản development và API auth

Sau khi migration đã được áp dụng, lần khởi động Development đầu tiên seed bốn role và bốn tài khoản kiểm thử:

| Role | Mã tài khoản | Mật khẩu mẫu |
|---|---|---|
| `Admin` | `admin` | `Admin@123` |
| `CanBoQuanLy` | `CBQL001` | `CanBo@123` |
| `GiangVien` | `GV001` | `GiangVien@123` |
| `SinhVien` | `2001230048` | `SinhVien@123` |

Không dùng các tài khoản/mật khẩu này ngoài Development; hãy đổi mật khẩu khi cần kiểm thử luồng `POST /api/auth/change-password`.

- `POST /api/auth/login`
- `POST /api/auth/change-password` (JWT bắt buộc)
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout` (JWT bắt buộc)

`GET /api/admin/ping` là endpoint kiểm thử phân quyền và chỉ role `Admin` được gọi.

## Quy trình cấp tài khoản sinh viên/giảng viên

Hồ sơ nghiệp vụ phải tồn tại trước khi cấp tài khoản đăng nhập. Không tạo trực tiếp
`User` có role `SinhVien` hoặc `GiangVien` khi chưa có bản ghi tương ứng.

Các endpoint dành cho `Admin` hoặc `CanBoQuanLy`:

- `POST /api/v1/sinh-vien`: tạo hồ sơ sinh viên, chưa tạo tài khoản.
- `POST /api/v1/giang-vien`: tạo hồ sơ giảng viên, chưa tạo tài khoản.
- `POST /api/v1/sinh-vien/{maSinhVien}/tai-khoan`: cấp tài khoản từ hồ sơ sinh viên đã có.
- `POST /api/v1/giang-vien/{maGiangVien}/tai-khoan`: cấp tài khoản từ hồ sơ giảng viên đã có.

Khi cấp tài khoản, backend thực hiện trong transaction: kiểm tra hồ sơ, tạo `User`,
hash mật khẩu, gán `UserRole`, rồi cập nhật `SinhVien.UserId` hoặc `GiangVien.UserId`.
Nếu hồ sơ không tồn tại API trả `404`; nếu hồ sơ đã có tài khoản API trả `409`.

Tài khoản mới được tạo với `MustChangePassword = true`. Sau lần đăng nhập đầu tiên,
web tự chuyển người dùng tới trang đổi mật khẩu và không cho mở chức năng khác. API
cũng chỉ cho phép gọi `change-password` hoặc `logout` cho đến khi đổi mật khẩu thành
công. Khi đổi mật khẩu, cờ này được tắt, các refresh token cũ bị thu hồi và người dùng
phải đăng nhập lại bằng mật khẩu mới. Migration dùng giá trị mặc định `false` cho các
tài khoản đã tồn tại để không khóa đột ngột người dùng hiện hành.

Hai cột `matKhau` cũ trong bảng nghiệp vụ chỉ được ánh xạ để tương thích schema và
không được sử dụng hay trả về API. Mật khẩu đăng nhập chỉ được lưu tại
`Users.PasswordHash` bằng ASP.NET Core `PasswordHasher`.

## API quản lý người dùng

Admin và Cán bộ quản lý:

- `GET /api/v1/nguoi-dung`: danh sách, tìm kiếm, lọc role/trạng thái và phân trang.
- `GET /api/v1/nguoi-dung/{id}`: chi tiết tài khoản và hồ sơ liên kết.
- `PUT /api/v1/nguoi-dung/{id}/trang-thai`: khóa hoặc mở khóa tài khoản.
- `POST /api/v1/nguoi-dung/{id}/dat-lai-mat-khau`: cấp mật khẩu tạm và bắt buộc đổi ở lần đăng nhập sau.
- CRUD thông tin hồ sơ qua `/api/v1/sinh-vien` và `/api/v1/giang-vien`.

Chỉ Admin:

- `POST /api/v1/nguoi-dung`: tạo tài khoản Admin/CBQL.
- `PUT /api/v1/nguoi-dung/{id}/vai-tro`: đổi vai trò tài khoản nội bộ.

Mọi tài khoản đã đăng nhập:

- `GET /api/v1/nguoi-dung/me`: xem thông tin cá nhân.
- `PUT /api/v1/nguoi-dung/me`: cập nhật họ tên, email và số điện thoại phù hợp hồ sơ.

CBQL chỉ xem và quản trị tài khoản `SinhVien`, `GiangVien`; không thể xem hay thay
đổi tài khoản Admin/CBQL. Khi khóa tài khoản, cấp lại mật khẩu hoặc đổi role, refresh
token cũ được thu hồi. Middleware kiểm tra trạng thái và role hiện tại từ database để
từ chối access token cũ ngay khi quyền hoặc trạng thái đã thay đổi.

## API quản lý giảng viên và giảng dạy

Admin và Cán bộ quản lý:

- `GET /api/v1/giang-vien?search=&khoa=&chuyenNganh=`: tìm kiếm và lọc hồ sơ giảng viên theo khoa/chuyên ngành.
- `GET /api/v1/giang-day/giang-vien/{maGiangVien}/phan-cong`: lấy hồ sơ phân công, các môn thuộc chương trình đào tạo của chuyên ngành, học kỳ, phòng và lịch đã xếp.
- `POST /api/v1/giang-day/giang-vien/{maGiangVien}/phan-cong`: tạo lớp học phần, xếp lịch dạy và sinh tự động các buổi học theo tuần.

Giảng viên:

- `GET /api/v1/giang-day/cua-toi`: danh sách lớp và lịch được phân công.
- `GET /api/v1/giang-day/lop/{maLopHocPhan}`: danh sách sinh viên, các buổi học và thống kê chuyên cần.
- `PUT /api/v1/giang-day/buoi-hoc/{maBuoiHoc}/sinh-vien/{maSinhVien}/diem-danh`: cập nhật trạng thái `CoMat`, `DiTre`, `CoPhep` hoặc `Vang`.
- `POST /api/v1/giang-day/buoi-hoc/{maBuoiHoc}/chot-diem-danh`: chốt buổi, đánh vắng sinh viên chưa được ghi nhận và cập nhật cảnh báo chuyên cần.

Admin/CBQL cũng có thể mở lớp để hỗ trợ kiểm tra. Giảng viên chỉ truy cập được lớp
được phân công cho chính mình. Trước khi phân công, hồ sơ giảng viên phải có
`Khoa` và `ChuyenNganh`; giá trị chuyên ngành cần khớp mã hoặc tên trong bảng
`NganhHoc` để hệ thống lọc đúng môn từ `ChuongTrinhDaoTao`.

## API nhận diện khuôn mặt (Tuần 8)

Website hỗ trợ xác thực mặt bổ sung sau mã và mật khẩu cho sinh viên đã đăng ký
hồ sơ khuôn mặt. `POST /api/auth/verify-face` yêu cầu JWT của bước mật khẩu và
3–5 embedding; API trả `204` nếu khớp hoặc `401` nếu không khớp. Endpoint này
giới hạn 5 lượt/phút/tài khoản. Website chỉ tạo cookie đăng nhập sau khi xác thực
mặt thành công và cho phép hoàn tất trong 5 phút. Đây là kiểm tra bổ sung trên
website, **không** phải MFA có chống giả mạo: endpoint đăng nhập API hiện vẫn cấp
JWT cho ứng dụng Flutter sau mật khẩu, và vector từ trình duyệt có thể bị gửi lại.

Sinh viên đăng nhập bằng đúng mã sinh viên của hồ sơ:

- `GET /api/v1/me/ho-so-khuon-mat`: trạng thái đăng ký, không trả vector.
- `POST /api/v1/me/ho-so-khuon-mat/dang-ky`: đăng ký bằng 3–5 embedding MobileNetV2 512 chiều.
- `PUT /api/v1/me/ho-so-khuon-mat`: thay mẫu hiện có; lỗi xác thực mẫu không xóa mẫu cũ.

Giảng viên của lớp (Admin/CBQL cũng có quyền hỗ trợ):

- `POST /api/v1/giang-day/buoi-hoc/{maBuoiHoc}/nhan-dien-khuon-mat`: so khớp chỉ với sinh viên đã đăng ký lớp, ghi `CoMat` vào `ChiTietDiemDanh`, trả kết quả tối thiểu. Buổi đã chốt bị từ chối; điểm danh đã ghi được giữ nguyên.

Admin/CBQL có thể xóa mẫu qua `DELETE /api/v1/sinh-vien/{maSinhVien}/ho-so-khuon-mat`.
API giới hạn 30 yêu cầu/phút/tài khoản. Vector được chuẩn hóa, lấy trung bình nhiều mẫu,
so khớp cosine và mã hóa bằng ASP.NET Core Data Protection trước khi lưu vào cột
`HoSoKhuonMat.vectorJSON` hiện có; không cần migration mới. Khi chạy nhiều instance,
cần dùng kho khóa Data Protection chung và bền vững để đọc được vector sau khi khởi động lại.
Theo lựa chọn hiện tại của nhóm, backend không lưu ảnh khuôn mặt gốc hoặc ảnh đã căn chỉnh.
Ngưỡng mẫu nằm trong mục `FaceRecognition` của `appsettings.example.json` và cần hiệu chỉnh
trên ảnh thử nghiệm của lớp trước khi dùng thực tế.

## Luồng đào tạo và mở nhiều lớp cùng môn

API và web đã bổ sung nhập chương trình bằng form/Excel, áp dụng học kỳ, mở nhiều lớp học phần cùng môn, phân công giảng viên/lịch học và mở đăng ký. Xem [hướng dẫn và API](TRAINING-WORKFLOW.md). Màn hình web: `/dao-tao` (Admin/CanBoQuanLy).
