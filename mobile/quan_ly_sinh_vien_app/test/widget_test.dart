import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/main.dart';

void main() {
  testWidgets('EduFace App builds and renders smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const EduFaceApp());
    expect(find.byType(EduFaceApp), findsOneWidget);
  });
}
