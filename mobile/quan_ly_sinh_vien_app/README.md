# EduFace Flutter app

Flutter client for Android, Web and Windows. The app communicates with
`backend/QuanLySinhVien.Api`; it does not access SQL Server directly.

For the complete Vietnamese guide—including database setup, all platform
instructions, demo accounts, features and known limitations—see the
[repository README](../../README.md).

## Quick start on Windows

Start the API in a separate PowerShell window from the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5081"
dotnet run --project backend\QuanLySinhVien.Api --no-launch-profile
```

Then start a Flutter target:

```powershell
Set-Location D:\KhoaLuan-main\mobile\quan_ly_sinh_vien_app
flutter pub get
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5081
```

For Chrome, use:

```powershell
flutter run -d chrome --web-port=5200 --dart-define=API_BASE_URL=http://localhost:5081
```

For an Android phone connected by USB, first run `adb reverse tcp:5081 tcp:5081`,
then:

```powershell
flutter run -d <device-id> --dart-define=API_BASE_URL=http://127.0.0.1:5081
```

## Face recognition status (Android and Chrome)

Face enrollment and attendance are integrated across Flutter, ASP.NET Core and
the private Python Face AI service:

- From the student profile, the app captures three JPEG samples with the
  front-facing camera, obtains consent, and posts them with the student's JWT
  to `POST /api/v1/me/ho-so-khuon-mat/dang-ky`.
- The backend calls Face AI `POST /encode`, requires one live face in each
  sample, and stores the samples and 128-value face embeddings protected with
  ASP.NET Data Protection. The API never sends embeddings back to the app.
- During attendance, Flutter uploads the camera image to the backend. The
  backend checks liveness and matches it only against the embedding enrolled
  for the JWT's student before recording attendance.
- In the admin student-management screen, Admin/CanBoQuanLy can view each
  student's FaceID registration status and registration date. Only Admin can
  remove an enrollment; the confirmation explains that this deletes stored
  face samples and embeddings but keeps attendance history.

To run the flow, configure the same `FaceAi:ApiKey` in ASP.NET User Secrets
and Python's `FACE_AI_API_KEY`, start Face AI, and make the API reachable from
the phone. See [backend setup](../../backend/README.md) and the repository's
[Android instructions](../../README.md#chạycài-ứng-dụng-trên-điện-thoại-android).
Students with old profiles must enroll again because their previous records
do not contain embeddings.

The matching threshold (`0.40`) and liveness minimum (`65%`) need validation
with representative real-world data before production use. Protect and
persist the Data Protection key ring across backend restarts/instances.

## Verification and builds

```powershell
dart format lib
dart analyze lib
flutter test
flutter build apk --debug --dart-define=API_BASE_URL=http://127.0.0.1:5081
flutter build windows --dart-define=API_BASE_URL=https://<api-host>
flutter build web --dart-define=API_BASE_URL=https://<api-host>
```

Use HTTPS and an API host reachable by end-user devices for distributed builds.
Do not ship secrets, development accounts or demo grades/tuition as official
school data.
