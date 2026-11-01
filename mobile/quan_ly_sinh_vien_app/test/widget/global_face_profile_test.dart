import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:quan_ly_sinh_vien_app/core/attendance/attendance_workflow_api.dart';
import 'package:quan_ly_sinh_vien_app/screens/student/attendance_workflow_screen.dart';

void main() {
  AttendanceWorkflowApi api(
    bool registered,
    bool approved, {
    bool rejected = false,
  }) => AttendanceWorkflowApi(
    tokenProvider: () async => 'fixture',
    client: MockClient((request) async {
      expect(request.method, 'GET');
      expect(request.url.path, '/api/v1/qr-diem-danh/ho-so');
      return http.Response(
        jsonEncode({
          'maSinhVien': 'SV1',
          'daDangKy': registered,
          'daDuyet': approved,
          'biTuChoi': rejected,
          'lyDoTuChoi': rejected ? 'Ảnh cần chụp rõ hơn' : null,
        }),
        200,
        headers: {'content-type': 'application/json; charset=utf-8'},
      );
    }),
  );
  for (final approved in [false, true]) {
    testWidgets(
      'existing global profile hides enrollment controls, approved=$approved',
      (tester) async {
        await tester.pumpWidget(
          MaterialApp(
            home: AttendanceWorkflowScreen(
              enrollment: true,
              api: api(true, approved),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(find.text('Bắt đầu đăng ký'), findsNothing);
        expect(find.byType(CheckboxListTile), findsNothing);
        expect(find.textContaining('tất cả lớp'), findsWidgets);
        expect(find.text('Cập nhật trạng thái hồ sơ'), findsOneWidget);
        await tester.pumpWidget(const SizedBox());
      },
    );
  }
  testWidgets(
    'staff-returned profile shows reason and allows one resubmission',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: AttendanceWorkflowScreen(
            enrollment: true,
            api: api(true, false, rejected: true),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Đăng ký lại'), findsOneWidget);
      expect(find.textContaining('Ảnh cần chụp rõ hơn'), findsOneWidget);
      expect(find.byType(CheckboxListTile), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    },
  );
  testWidgets(
    'only a student without global profile can consent to first registration',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: AttendanceWorkflowScreen(
            enrollment: true,
            api: api(false, false),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Bắt đầu đăng ký'), findsOneWidget);
      expect(
        tester
            .widget<ElevatedButton>(
              find.widgetWithText(ElevatedButton, 'Bắt đầu đăng ký'),
            )
            .onPressed,
        isNull,
      );
      await tester.ensureVisible(find.byType(CheckboxListTile));
      await tester.tap(find.byType(CheckboxListTile));
      await tester.pump();
      expect(
        tester
            .widget<ElevatedButton>(
              find.widgetWithText(ElevatedButton, 'Bắt đầu đăng ký'),
            )
            .onPressed,
        isNotNull,
      );
      await tester.pumpWidget(const SizedBox());
    },
  );
}
