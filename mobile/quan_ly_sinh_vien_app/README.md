# Ứng dụng Mobile Quản Lý Sinh Viên (EduFace)

Ứng dụng Flutter cross-platform (Android, iOS, Web, Windows, macOS, Linux) cho Hệ thống Quản Lý Sinh Viên tích hợp nhận diện khuôn mặt. Ứng dụng giao tiếp với `backend/QuanLySinhVien.Api` thông qua REST API; không truy cập SQL Server trực tiếp.

## 📱 Tính năng chính

### 🔐 Xác thực & Phân quyền
- **Đăng nhập** bằng mã tài khoản (mã sinh viên/giảng viên) và mật khẩu
- **Đổi mật khẩu** bắt buộc lần đầu đăng nhập (hỗ trợ `MustChangePassword`)
- **Làm mới token** tự động (access token + refresh token)
- **Đăng xuất** an toàn (thu hồi refresh token trên backend)
- **Phân quyền theo vai trò**: Admin, Cán bộ quản lý, Giảng viên, Sinh viên

### 👨‍🎓 Chức năng Sinh viên
| Màn hình | Chức năng |
|----------|-----------|
| **Trang chủ** | Thông tin cá nhân, trạng thái học tập, thông báo gần đây |
| **Lịch học** | Xem lịch học theo tuần, chi tiết lớp học phần, phòng học, giảng viên |
| **Đăng ký học phần** | Tìm kiếm môn, lọc theo trạng thái (còn chỗ/trùng lịch), đăng ký/hủy lớp |
| **Điểm & Kết quả** | Xem điểm thành phần, điểm tổng kết, GPA tích lũy |
| **Học phí** | Xem học phí học kỳ, chi tiết từng khoản, trạng thái thanh toán |
| **Thông báo** | Danh sách thông báo, xem chi tiết, đánh dấu đã đọc |
| **Đăng ký khuôn mặt** | Chụp 3-5 ảnh mẫu, gửi embedding lên backend (FaceID) |
| **Điểm danh khuôn mặt** | Quét khuôn mặt để điểm danh vào lớp (tích hợp liveness check) |
| **Hồ sơ cá nhân** | Cập nhật thông tin, đổi mật khẩu, xem trạng thái FaceID |

### 👨‍🏫 Chức năng Giảng viên
- Xem danh sách lớp được phân công
- Xem danh sách sinh viên trong lớp
- **Điểm danh khuôn mặt** cho sinh viên trong lớp
- Chốt điểm danh buổi học
- Xem lịch giảng dạy cá nhân

### 👨‍💼 Chức năng Cán bộ quản lý / Admin
| Tab | Chức năng |
|-----|-----------|
| **Dashboard** | Tổng quan hệ thống, thống kê nhanh |
| **Quản lý lớp** | Xem, tạo, chỉnh sửa lớp học phần |
| **Chương trình đào tạo** | Quản lý CTĐT, áp dụng học kỳ, mở nhiều lớp cùng môn |
| **Quản lý sinh viên** | Danh sách, hồ sơ, trạng thái FaceID, cấp tài khoản |
| **Quản lý học phí** | Theo dõi thu học phí, xuất báo cáo |
| **Chuyên cần** | Theo dõi điểm danh, cảnh báo vắng mặt |

## 🏗️ Kiến trúc & Công nghệ

```
lib/
├── main.dart                          # Entry point, DI container
├── app.dart                           # App root, routing, theme
├── core/
│   ├── api/api_constants.dart         # API endpoint constants
│   ├── auth/                          # Auth service, token storage, user model
│   ├── attendance/                    # Attendance models & service
│   ├── face/                          # Face detection, embedding, enrollment, recognition
│   ├── notifications/                 # FCM + in-app notifications
│   ├── student/                       # Student portal & registration services
│   ├── theme/                         # App colors, Material3 theme
│   └── widgets/                       # Reusable UI components
└── screens/
    ├── auth/                          # Login, change password, forgot password
    ├── admin/                         # Admin/CBQL tabs & scaffolds
    └── student/                       # Student tabs & screens
```

**Dependencies chính:**
- `flutter_secure_storage` - Lưu trữ token an toàn
- `http` - HTTP client cho API calls
- `camera` / `image_picker` - Chụp ảnh khuôn mặt
- `google_mlkit_face_detection` - Phát hiện khuôn mặt on-device
- `firebase_messaging` - Push notification (FCM)

## 🚀 Hướng dẫn cài đặt & chạy

### Yêu cầu hệ thống
- Flutter SDK ≥ 3.19 (Dart ≥ 3.3)
- Android Studio / VS Code với Flutter extension
- Backend API đang chạy (xem [backend README](../backend/README.md))

### 1. Cấu hình backend API
Tạo file `.env` hoặc truyền qua `--dart-define`:

```bash
# Development (Android Emulator)
--dart-define=API_BASE_URL=http://10.0.2.2:5081/

# Development (Thiết bị thật - máy cùng LAN)
--dart-define=API_BASE_URL=http://<IP-MÁY>:5081/

# Production
--dart-define=API_BASE_URL=https://api.truonghoc.edu.vn/
```

### 2. Cài đặt dependencies
```powershell
cd mobile/quan_ly_sinh_vien_app
flutter pub get
```

### 3. Chạy ứng dụng

**Windows:**
```powershell
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5081
```

**Web (Chrome):**
```powershell
flutter run -d chrome --web-port=5200 --dart-define=API_BASE_URL=http://localhost:5081
```

**Android (Emulator):**
```powershell
# Cần chạy: adb reverse tcp:5081 tcp:5081
flutter run -d <device-id> --dart-define=API_BASE_URL=http://10.0.2.2:5081/
```

**Android (Thiết bị thật):**
```powershell
# IP LAN của máy chạy backend
flutter run -d <device-id> --dart-define=API_BASE_URL=http://192.168.x.x:5081/
```

**iOS (macOS only):**
```powershell
cd ios && pod install && cd ..
flutter run -d ios --dart-define=API_BASE_URL=http://localhost:5081
```

### 4. Build production

**Android APK:**
```powershell
flutter build apk --release --dart-define=API_BASE_URL=https://api.truonghoc.edu.vn/
```

**Android App Bundle (Play Store):**
```powershell
flutter build appbundle --release --dart-define=API_BASE_URL=https://api.truonghoc.edu.vn/
```

**Windows:**
```powershell
flutter build windows --release --dart-define=API_BASE_URL=https://api.truonghoc.edu.vn/
```

**Web:**
```powershell
flutter build web --release --dart-define=API_BASE_URL=https://api.truonghoc.edu.vn/
```

## 🔧 Cấu hình nâng cao

### Android Permissions (android/app/src/main/AndroidManifest.xml)
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<!-- Cho FCM -->
<uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

### iOS Permissions (ios/Runner/Info.plist)
```xml
<key>NSCameraUsageDescription</key>
<string>Ứng dụng cần truy cập camera để đăng ký và điểm danh khuôn mặt</string>
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>  <!-- Chỉ Development, Production dùng HTTPS -->
</dict>
```

### FCM Setup
1. Tạo project Firebase Console
2. Thêm `google-services.json` (Android) và `GoogleService-Info.plist` (iOS)
3. Cấu hình `FCM_API_KEY` trong backend User Secrets

### Face Recognition (Backend + Python Service)
Xem chi tiết tại: [Backend Face Recognition](../backend/README.md#api-nhận-diện-khuôn-mặt-tuần-8)

## 🧪 Kiểm thử & Code Quality

```powershell
# Format code
dart format lib test

# Static analysis
dart analyze lib test

# Unit tests
flutter test

# Widget tests
flutter test test/widget_test.dart

# Integration test (cần device/emulator)
flutter test integration_test/
```

## 📂 Cấu trúc thư mục quan trọng

```
mobile/quan_ly_sinh_vien_app/
├── android/           # Android native project
├── ios/               # iOS native project
├── web/               # Web build output
├── windows/           # Windows desktop
├── macos/             # macOS desktop
├── linux/             # Linux desktop
├── assets/            # Images, fonts
├── lib/               # Dart source code (xem kiến trúc ở trên)
├── test/              # Unit & widget tests
├── pubspec.yaml       # Dependencies & config
├── pubspec.lock       # Locked versions
└── analysis_options.yaml  # Lint rules
```

## 🐛 Troubleshooting thường gặp

| Vấn đề | Giải pháp |
|--------|-----------|
| **Kết nối bị từ chối (Android)** | Chạy `adb reverse tcp:5081 tcp:5081` hoặc dùng IP LAN thay `10.0.2.2` |
| **CORS error (Web)** | Backend cấu hình CORS cho origin web (localhost:5200) |
| **Camera không mở** | Kiểm tra permission trong manifest/Info.plist, cấp quyền runtime |
| **FCM không nhận được** | Kiểm tra google-services.json, FCM token registration, backend config |
| **Face detection chậm** | Giảm resolution camera, dùng ML Kit on-device (không gửi ảnh lên server) |
| **Token hết hạn liên tục** | Kiểm tra đồng bộ thời gian thiết bị, refresh token logic |

## 📋 Tài khoản Development (Backend seed)

Sau khi chạy migration và khởi động backend Development lần đầu:

| Vai trò | Mã tài khoản | Mật khẩu |
|---------|-------------|----------|
| Admin | `admin` | `Admin@123` |
| Cán bộ QL | `CBQL001` | `CanBo@123` |
| Giảng viên | `GV001` | `GiangVien@123` |
| Sinh viên | `2001230048` | `SinhVien@123` |

> ⚠️ Chỉ dùng cho Development. Đổi mật khẩu ngay khi test luồng `MustChangePassword`.

## 📄 License & Bản quyền

Dự án đồ án tốt nghiệp - Trường Đại học Công nghiệp TP.HCM (HUIT).
Không sử dụng cho mục đích thương mại ללא sự cho phép.

## 🔗 Liên kết liên quan

- [Backend API README](../backend/README.md)
- [Backend Face AI Service](../backend/FaceAiService/README.md)
- [Web Admin Portal](../web/README.md)
- [Repository Root README](../../README.md)
- [API Design Doc](../backend/API-DESIGN.md)
- [Training Workflow](../backend/TRAINING-WORKFLOW.md)