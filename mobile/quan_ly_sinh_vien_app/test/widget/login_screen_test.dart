import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/theme/app_theme.dart';
import 'package:quan_ly_sinh_vien_app/screens/auth/login_screen.dart';

/// Bọc widget trong MaterialApp với theme chuẩn của app để test đúng
Widget _wrap(Widget child) => MaterialApp(
      theme: AppTheme.lightTheme,
      home: child,
    );

void main() {
  group('LoginScreen — Widget Tests', () {
    testWidgets('hiển thị đủ các thành phần UI cơ bản', (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      // Logo và tiêu đề
      expect(find.text('EduFace'), findsOneWidget);
      expect(find.text('Cổng thông tin & Điểm danh sinh viên'), findsOneWidget);

      // Các trường nhập liệu
      expect(find.text('Mã số sinh viên'), findsOneWidget);
      expect(find.text('Mật khẩu'), findsOneWidget);

      // Nút đăng nhập
      expect(find.text('Đăng nhập'), findsOneWidget);

      // Link quên mật khẩu
      expect(find.text('Quên mật khẩu?'), findsOneWidget);
    });

    testWidgets('KHÔNG hiển thị nút Đăng ký (đã xóa tính năng này)',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      expect(find.textContaining('Đăng ký'), findsNothing);
      expect(find.textContaining('Tạo tài khoản'), findsNothing);
    });

    testWidgets('hiển thị lỗi khi submit form trống', (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      // Nhấn Đăng nhập mà không nhập gì
      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle();

      // Phải thấy validation error
      expect(find.textContaining('nhập mã sinh viên'), findsOneWidget);
    });

    testWidgets('hiển thị lỗi validate khi mssv trống nhưng mật khẩu có giá trị',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      // Chỉ nhập mật khẩu
      final passwordField = find.byWidgetPredicate((w) =>
          w is TextField && (w.obscureText == true));
      await tester.enterText(passwordField.first, 'SinhVien@123');

      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle();

      expect(find.textContaining('nhập mã sinh viên'), findsOneWidget);
    });

    testWidgets('hiển thị lỗi validate khi mssv có giá trị nhưng mật khẩu trống',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      // Nhập MSSV
      final usernameField = find.byWidgetPredicate((w) =>
          w is TextField && w.obscureText != true).first;
      await tester.enterText(usernameField, '2001230048');

      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle();

      expect(find.textContaining('nhập mật khẩu'), findsOneWidget);
    });

    testWidgets('trường mật khẩu ẩn văn bản (obscureText)', (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      final passwordFields = find.byWidgetPredicate(
          (w) => w is TextField && w.obscureText == true);
      expect(passwordFields, findsAtLeastNWidgets(1));
    });

    testWidgets('có thể nhập vào ô MSSV', (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      final usernameField = find.byWidgetPredicate((w) =>
          w is TextField && w.obscureText != true).first;
      await tester.enterText(usernameField, '2001230048');
      await tester.pump();

      expect(find.text('2001230048'), findsOneWidget);
    });

    testWidgets('nút Thử nghiệm Webcam hiển thị trên màn hình',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.pumpAndSettle();

      expect(find.textContaining('Webcam'), findsOneWidget);
    });

    testWidgets('mở trang khôi phục mật khẩu từ liên kết đăng nhập',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));

      await tester.tap(find.text('Quên mật khẩu?'));
      await tester.pumpAndSettle();

      expect(find.text('Khôi phục mật khẩu'), findsOneWidget);
      expect(find.textContaining('Nhập mã sinh viên hoặc email trường'),
          findsOneWidget);
    });

    testWidgets('trang khôi phục báo lỗi khi gửi biểu mẫu trống',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.tap(find.text('Quên mật khẩu?'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Gửi mã khôi phục'));
      await tester.pumpAndSettle();

      expect(find.text('Vui lòng nhập Email hoặc MSSV'), findsOneWidget);
    });

    testWidgets('quay lại trang đăng nhập từ trang khôi phục mật khẩu',
        (tester) async {
      await tester.pumpWidget(_wrap(const LoginScreen()));
      await tester.tap(find.text('Quên mật khẩu?'));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      expect(find.text('Đăng nhập'), findsOneWidget);
      expect(find.text('Khôi phục mật khẩu'), findsNothing);
    });
  });
}
