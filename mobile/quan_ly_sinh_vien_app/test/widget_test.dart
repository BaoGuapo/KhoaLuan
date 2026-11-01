import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/app.dart';
import 'package:quan_ly_sinh_vien_app/data/repositories/auth_repository.dart';
import 'package:quan_ly_sinh_vien_app/presentation/auth/auth_controller.dart';
import 'package:quan_ly_sinh_vien_app/presentation/shared/loading_screen.dart';

class _Repository extends Fake implements AuthRepository {}

void main() {
  testWidgets(
    'Application shows authentication loading before session restore',
    (tester) async {
      final controller = AuthController(_Repository());
      addTearDown(controller.dispose);
      await tester.pumpWidget(QuanLySinhVienApp(authController: controller));
      expect(find.byType(LoadingScreen), findsOneWidget);
    },
  );
}
