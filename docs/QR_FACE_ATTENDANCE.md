# Điểm danh bằng QR và xác minh khuôn mặt

Luồng dùng chung cho ASP.NET MVC và Flutter. Sinh viên phải đăng nhập, thuộc lớp của buổi học và có hồ sơ khuôn mặt được duyệt. Backend tự phát hiện mặt, kiểm tra giả mạo và trích xuất MobileNetV2; không tin vector, tọa độ mặt hay kết luận người thật từ client.

## Chạy và dùng thử

1. Khôi phục package .NET và chạy `./scripts/setup-face-models.ps1`. Ba mô hình đã nằm trong `backend/QuanLySinhVien.Api/Models/Face` và được copy khi publish.
2. Xem `docs/qr-face-attendance-migration.sql`, rồi áp dụng migration với database đã được cấu hình: `dotnet ef database update --project backend/QuanLySinhVien.Api`. Quá trình phát triển tính năng chỉ tạo migration/script; không tự cập nhật Azure SQL dùng chung.
3. Chạy backend và web theo README. Trên web mở `/diem-danh`; mục này có trên thanh điều hướng. Trang đăng ký mặt là `/diem-danh/ho-so`.
4. Mobile: `flutter pub get`, sau đó `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5081/` với Android Emulator. Thiết bị thật cần địa chỉ API truy cập được qua mạng; production dùng HTTPS. Windows cần Developer Mode hoặc quyền tạo symbolic link cho plugin Flutter. Máy phát triển hiện chưa có Android SDK; chưa có kiểm chứng APK hoặc camera thiết bị thật.
5. Sinh viên đồng ý lưu ảnh đăng ký/mẫu nhận diện mã hóa, chụp nhìn thẳng → quay theo hướng ngẫu nhiên → nhìn thẳng. Sau kiểm tra người thật, động tác và nhất quán khuôn mặt, ba ảnh được giữ làm căn cứ duyệt; hồ sơ ở trạng thái chờ duyệt.
6. CBQL/Admin mở `/quan-ly/ho-so-khuon-mat`, chọn **Đối chiếu ảnh** để xem ảnh thẻ từ hồ sơ gốc và ba ảnh chụp khi đăng ký, có thể phóng to. Ảnh thẻ được CBQL/Admin cập nhật riêng và ghi người/thời gian cập nhật; không lấy ảnh đại diện tự chọn làm căn cứ. Người duyệt xác nhận đã so khớp hai nguồn ảnh rồi duyệt, hoặc trả hồ sơ kèm lý do để sinh viên đăng ký lại. Sinh viên không cần có mặt, không chụp lại tại máy CBQL/Admin. Duyệt một lần dùng cho mọi lớp; giảng viên không có quyền xem ảnh đối chiếu, duyệt hoặc hủy hồ sơ.
7. Giảng viên chọn buổi học đã có từ lịch giảng dạy và mở phiên 1–30 phút. QR được tạo lại mỗi 15 giây; từng QR có hiệu lực tối đa 30 giây. Mở phiên mới sẽ đóng các phiên cũ của buổi đó. Buổi học đã chốt không mở lại được.
8. Sinh viên quét QR. Backend kiểm tra tài khoản, danh sách lớp, thời hạn, hồ sơ được duyệt và kết quả hiện có. Hợp lệ mới cấp thử thách 90 giây, ràng buộc tài khoản và phiên.
9. Sinh viên chụp ba ảnh JPEG theo hướng dẫn. Backend chạy SCRFD, MiniFASNetV2, kiểm tra hướng quay và đối chiếu 1:1 từng ảnh với tài khoản. Khi mọi điều kiện đạt, lưu Có mặt. Phiên hết hạn hoặc bị đóng trong lúc xử lý sẽ không được ghi nhận.
10. Giảng viên xem danh sách kết quả tự cập nhật, đóng phiên khi kết thúc. Chốt buổi học ở màn hình quản lý lớp sẽ đóng các phiên QR trong cùng transaction và ghi Vắng cho người chưa điểm danh. Sinh viên xem được lịch sử riêng trên cả web/mobile. Đóng phiên QR khác với chốt kết quả buổi học.

## API

Các đường dẫn dưới đây có tiền tố `/api/v1/qr-diem-danh`. Toàn bộ cần JWT; quyền được kiểm tra tại backend.

| Method và đường dẫn | Vai trò và tác dụng |
|---|---|
| GET `buoi-hoc` | Giảng viên phụ trách/QL: buổi học trong 7 ngày trước đến 14 ngày tới |
| POST `buoi-hoc/{code}/mo` | Giảng viên phụ trách/QL: `{ "minutes": 10 }` |
| GET `phien/{id}/qr` | Giảng viên phụ trách/QL: QR và thời hạn; không cache |
| POST `phien/{id}/dong` | Giảng viên phụ trách/QL: đóng phiên |
| GET `phien/{id}/ket-qua` | Giảng viên phụ trách/QL: kết quả buổi học |
| POST `quet` | Sinh viên: `{ "qrToken": "eduface:attendance:..." }`; trả thử thách |
| POST `xac-nhan` | Sinh viên: `{ "challengeId": "GUID", "frames": ["base64JPEG", "base64JPEG", "base64JPEG"] }` |
| GET `ho-so` | Sinh viên: trạng thái hồ sơ của mình |
| POST `ho-so/thu-thach` | Sinh viên: cấp thử thách đăng ký |
| POST `ho-so` | Sinh viên: ba ảnh/thử thách, `consent: true` và `storeReviewPhotosConsent: true` |
| GET `ho-so/cho-duyet` | CBQL/Admin: hồ sơ sinh viên dùng chung, gồm hồ sơ đã duyệt |
| GET `ho-so/{code}/doi-chieu` | CBQL/Admin: ảnh thẻ, ảnh đăng ký, metadata và phiên bản từng nguồn ảnh |
| POST `ho-so/{code}/anh-the` | CBQL/Admin: ảnh thẻ JPEG/PNG từ hồ sơ gốc, tối đa 1 MB |
| POST `ho-so/{code}/duyet` | CBQL/Admin: `profileVersion`, `identityPhotoVersion`, `confirmCompared: true`, ghi chú tùy chọn |
| POST `ho-so/{code}/tu-choi` | CBQL/Admin: các phiên bản đang xem và lý do `note` bắt buộc; sinh viên được đăng ký lại |
| POST `ho-so/{code}/huy` | CBQL/Admin: hủy hồ sơ đã duyệt để sinh viên đăng ký lại |
| POST `ho-so/{code}/thu-thach-duyet` | API camera duyệt cũ trả 410; dùng đối chiếu ảnh |
| GET `ket-qua-cua-toi` | Sinh viên: 100 kết quả gần nhất của chính mình |
| GET `buoi-hoc-cua-toi` | Sinh viên: buổi học hôm nay theo UTC+7 |

QR dùng chung cho nhiều sinh viên. Mã thử thách cá nhân dùng một lần, được tiêu thụ nguyên tử trước khi chạy AI; thất bại phải bắt đầu lại. Backend lưu ba hash SHA256 của byte ảnh trong thử thách để chặn ảnh trùng trong cùng lượt và ảnh đã dùng ở lượt khác của cùng sinh viên, kể cả khi đổi challenge ID. Đây là phát hiện phát lại đúng byte ảnh; chuyển mã, chỉnh sửa ảnh hoặc video thích nghi vẫn cần PAD và kiểm thử riêng. Không nhận mã sinh viên từ payload điểm danh. Chống ghi trùng bằng khóa `(MaBuoiHoc, MaSinhVien)` và transaction Serializable, kèm kiểm tra lại quyền, trạng thái, thời hạn, hồ sơ ngay trước khi lưu. Các endpoint cũ nhận vector để tự đăng ký hoặc điểm danh trả HTTP 410; cập nhật client theo API mới.

Ảnh camera tối đa 1 MB/ảnh, JPEG 160–2048 pixel. Ảnh lấy mẫu hướng dẫn và ảnh xác minh điểm danh không được lưu; riêng ba ảnh đăng ký đã đạt kiểm tra được lưu mã hóa bằng ASP.NET Data Protection để CBQL/Admin đối chiếu, sau khi sinh viên đồng ý rõ ràng. Client cũ chưa gửi `storeReviewPhotosConsent` bị từ chối trước khi tiêu thụ thử thách. Ảnh thẻ được chuẩn hóa JPEG và mã hóa riêng trong `StudentIdentityPhotos`, có người/thời gian cập nhật; không dùng URL avatar làm ảnh thẻ. Chỉ endpoint quản lý có quyền mới giải mã ảnh; trả `Cache-Control: no-store, private`, không đưa ảnh vào thư mục static hoặc log. Quyết định duyệt gắn cả phiên bản đăng ký và phiên bản ảnh thẻ để chặn duyệt nội dung vừa bị thay đổi. App xóa ảnh chụp tạm sau khi đọc. Cần giữ key ring Data Protection qua triển khai và dùng chung giữa các instance. Không bật ghi request body ảnh ở reverse proxy/APM. Hồ sơ cũ chưa lưu ảnh không thể khôi phục ảnh từ embedding; CBQL trả hồ sơ kèm lý do để bổ sung một lần.

## Mô hình và hiệu chỉnh

| Tệp | Nguồn | SHA256 |
|---|---|---|
| `det_2.5g.onnx` | [SCRFD từ face-reidentification v0.0.1](https://github.com/yakhyo/face-reidentification/releases/tag/v0.0.1) | `041f73f47371333d1d17a6fee6c8ab4e6aecabefe398ff32cca4e2d5eaee0af9` |
| `mobilenetv2_mcp.onnx` | [Face recognition v0.0.1](https://github.com/yakhyo/face-recognition/releases/tag/v0.0.1) | `38b148284dd48cc898d5d4453104252fbdcbacc105fe3f0b80e78954d9d20d89` |
| `MiniFASNetV2.onnx` | [Face anti-spoofing weights](https://github.com/yakhyo/face-anti-spoofing/releases/tag/weights) | `b32929adc2d9c34b9486f8c4c7bc97c1b69bc0ea9befefc380e4faae4e463907` |

SCRFD dùng RGB 640×640, chuẩn hóa `(pixel−127.5)/128`, padding đen, ba stride 8/16/32 và NMS. MobileNetV2 dùng căn chỉnh 5 mốc, RGB 112×112 và chuẩn hóa như website cũ, xuất 512 chiều rồi L2 normalize. MiniFASNetV2 dùng vùng mặt mở rộng hệ số 2.7, BGR 80×80, giá trị 0–255 và softmax; lớp 1 là người thật. Các dự án nguồn có giấy phép/tài liệu riêng; giấy phép mã nguồn và điều kiện sử dụng checkpoint/dữ liệu huấn luyện cần được xem riêng khi phân phối sản phẩm.

Cấu hình backend bằng section `FaceEvidence`: `ModelDirectory` mặc định `Models/Face`, `MinimumLiveScore` mặc định `0.80`, `MinimumTurn` mặc định `0.12` (độ dịch chuyển mũi chiếu trên trục hai mắt, không phải độ góc). `FaceRecognition:MatchThreshold` mặc định `0.35`; `SampleConsistencyThreshold` mặc định `0.70`. Đây là ngưỡng khởi đầu, không phải xác suất chính xác hay ngưỡng đã kiểm chứng với sinh viên của trường.

Cần kiểm thử người thật, ảnh in, ảnh trên nhiều màn hình và video trên nhiều điện thoại/điều kiện sáng. Tách người và lần thu giữa tập hiệu chỉnh và tập kiểm tra; báo cáo tỷ lệ nhận nhầm/từ chối nhầm, APCER/BPCER và thời gian xử lý. Kiểm thử tự động hiện xác nhận pipeline chạy, hợp đồng tensor và các trường hợp từ chối; không chứng minh độ chính xác thực địa. Thử thách ba ảnh giảm phát lại đơn giản, không phải bảo đảm chống camera ảo hoặc video/deepfake thích nghi.

QR có thể được chuyển tiếp cho người ở ngoài lớp. Nhận diện mặt và PAD không chứng minh vị trí trong lớp. Phiên bản này chưa thêm Bluetooth/GPS/Wi-Fi hoặc device attestation; giảng viên cần giám sát và đối soát khi chốt.

## Kiểm tra tự động

Trang quản lý riêng cho CBQL/Admin là **`/quan-ly/ho-so-khuon-mat`**, có mục **Duyệt hồ sơ khuôn mặt** trên thanh điều hướng và trang chủ của hai vai trò. Trang tự tải danh sách, lọc trạng thái và tìm mã sinh viên/họ tên; thao tác xác minh/duyệt dùng API proxy riêng dưới cùng URL, bảo vệ CSRF và giữ bearer ở server. `/diem-danh` dành cho phiên QR và kết quả, không chứa phần duyệt hồ sơ. Giảng viên và sinh viên bị từ chối truy cập trang quản lý riêng.

Hồ sơ khuôn mặt gắn một-một với **mã sinh viên**, không gắn mã lớp, môn, giảng viên hoặc phiên QR. Sinh viên đăng ký một lần và được duyệt một lần để sử dụng tại tất cả lớp đã tham gia. Trạng thái chờ duyệt không cho đăng ký đè; trạng thái đã duyệt không yêu cầu giảng viên môn khác duyệt lại. Mỗi buổi học vẫn có lượt xác minh trực tiếp riêng để đối chiếu với hồ sơ đã có, không tạo hồ sơ mới. Có thể đăng ký lại khi CBQL/Admin trả hồ sơ kèm lý do hoặc hủy hồ sơ đã duyệt; thao tác hủy ảnh hưởng tất cả lớp. Bộ kiểm thử có tình huống một hồ sơ/duyệt được giữ nguyên khi cùng sinh viên điểm danh thành công ở hai lớp với giảng viên khác nhau.

Luồng Flutter và web kiểm tra động tác trực tiếp trong lúc camera chạy. Mỗi lần quan sát gửi một JPEG tới `POST /api/v1/qr-diem-danh/huong-dan` cùng challengeId; API kiểm tra nonce còn hạn, chưa dùng và IssuedTo đúng tài khoản, rồi chạy SCRFD và PAD. Phản hồi gồm chất lượng, pose và ngưỡng quay cấu hình; không lưu ảnh, không tiêu thụ nonce, không tạo hồ sơ hoặc điểm danh. Chính sách riêng giới hạn 120 yêu cầu/phút/tài khoản. Flutter lấy mẫu tuần tự, nghỉ ít nhất 650 ms sau mỗi phản hồi để tránh xếp chồng yêu cầu.

Ứng dụng chỉ nhận ảnh khi đúng động tác và ổn định qua ba lần quan sát liên tiếp. Quay sai hướng, chưa quay đủ, chưa nhìn thẳng lại, không thấy mặt, mặt lệch hoặc quá nhỏ đều có phản hồi để sửa ngay; không dùng đếm ngược để quyết định chụp. Bộ ảnh giữ chính các JPEG đã được kiểm tra; không chụp thêm một ảnh khác sau khi nhận phản hồi thành công. Client dùng cùng quy ước pose/hướng với backend và biên an toàn chặt hơn. Camera giữ nguyên qua mọi bước; có xem lại ảnh và chỉ gửi bộ ảnh khi bấm **Gửi xác minh**. Hủy/rời trang bỏ phản hồi đến muộn. API cuối vẫn kiểm tra đầy đủ PAD, động tác, cùng người, nonce và chống phát lại; kết quả hướng dẫn không cấp quyền đăng ký hoặc điểm danh.

Flutter dùng Dart 3.10+/Flutter 3.38+ và `camera_web` từ 0.3.5+6 để có các bản sửa khởi tạo Safari, capability camera và chụp ảnh. Ảnh chụp được chuẩn hóa JPEG, bake orientation, giới hạn cạnh dài 960 pixel trước khi gửi. Ảnh selfie của `camera_web` được bỏ lật gương để hướng quay đầu và embedding thống nhất với camera native và website. Khi Safari hỏi quyền camera, trạng thái inactive tạm thời không tự hủy bước khởi tạo; rời trang thực sự vẫn hủy lượt chụp.

```powershell
dotnet build backend/QuanLySinhVien.Checks -c Release
dotnet run --project backend/QuanLySinhVien.Checks -- --attendance
dotnet run --project backend/QuanLySinhVien.Checks -- --attendance-web
dotnet run --project backend/QuanLySinhVien.Checks -- --attendance-api
node --test web/QuanLySinhVien.Web/tests/face-capture.test.cjs
cd mobile/quan_ly_sinh_vien_app
flutter analyze --no-fatal-infos
flutter test
```

Kiểm thử backend dùng SQLite trong bộ nhớ, không thay đổi database dùng chung; kiểm thử Razor dùng API giả trên loopback. Bài kiểm tra ONNX chạy mô hình thật, kiểm tra ảnh trống và dữ liệu sai định dạng. Có thể đặt ảnh mẫu công khai tại `.artifacts/face-test.jpg` để kiểm tra thêm ảnh mặt tĩnh phải bị từ chối. Kiểm thử camera và hiệu chỉnh ngưỡng trên thiết bị thật vẫn cần thực hiện.



