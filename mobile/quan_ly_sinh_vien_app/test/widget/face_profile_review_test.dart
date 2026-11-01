import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:image/image.dart' as image;
import 'package:quan_ly_sinh_vien_app/core/attendance/attendance_workflow_api.dart';
import 'package:quan_ly_sinh_vien_app/screens/admin/face_profile_review_screen.dart';

void main() {
  String photo() =>
      'data:image/jpeg;base64,${base64Encode(image.encodeJpg(image.Image(width: 160, height: 160)))}';
  for (final hasReference in [true, false]) {
    testWidgets(
      'staff reviews stored images and decides without recapture, reference=$hasReference',
      (tester) async {
        final requests = <http.Request>[];
        final api = AttendanceWorkflowApi(
          tokenProvider: () async => 'staff',
          client: MockClient((request) async {
            requests.add(request);
            if (request.method == 'GET') {
              expect(
                request.url.path,
                '/api/v1/qr-diem-danh/ho-so/SV1/doi-chieu',
              );
              return http.Response(
                jsonEncode({
                  'maSinhVien': 'SV1',
                  'hoTen': 'Sinh viên',
                  'daDuyet': false,
                  'biTuChoi': false,
                  'profileVersion': 'p1',
                  'identityPhotoVersion': hasReference ? 'i1' : null,
                  'anhThe': hasReference ? photo() : null,
                  'anhDangKy': [photo(), photo(), photo()],
                }),
                200,
                headers: {'content-type': 'application/json; charset=utf-8'},
              );
            }
            expect(
              request.url.path,
              '/api/v1/qr-diem-danh/ho-so/SV1/${hasReference ? 'duyet' : 'tu-choi'}',
            );
            final body = jsonDecode(request.body) as Map;
            expect(body.keys.toSet(), {
              'profileVersion',
              'identityPhotoVersion',
              'confirmCompared',
              'note',
            });
            expect(body['profileVersion'], 'p1');
            expect(body['confirmCompared'], hasReference);
            if (!hasReference) {
              expect(body['note'], 'Thiếu ảnh thẻ trong hồ sơ');
            }
            return http.Response(
              jsonEncode({'message': 'Đã xử lý'}),
              200,
              headers: {'content-type': 'application/json; charset=utf-8'},
            );
          }),
        );
        await tester.pumpWidget(
          MaterialApp(
            home: Builder(
              builder: (context) => Scaffold(
                body: TextButton(
                  onPressed: () => Navigator.push(
                    context,
                    MaterialPageRoute<void>(
                      builder: (_) =>
                          FaceProfileReviewScreen(studentCode: 'SV1', api: api),
                    ),
                  ),
                  child: const Text('Mở hồ sơ'),
                ),
              ),
            ),
          ),
        );
        await tester.tap(find.text('Mở hồ sơ'));
        await tester.pumpAndSettle();
        expect(find.byType(Image), findsNWidgets(hasReference ? 4 : 3));
        final approve = find.widgetWithText(FilledButton, 'Duyệt hồ sơ');
        await tester.ensureVisible(approve);
        expect(tester.widget<FilledButton>(approve).onPressed, isNull);
        if (hasReference) {
          await tester.ensureVisible(find.byType(CheckboxListTile));
          await tester.tap(find.byType(CheckboxListTile));
          await tester.pump();
          expect(tester.widget<FilledButton>(approve).onPressed, isNotNull);
          await tester.ensureVisible(approve);
          await tester.tap(approve);
        } else {
          final reject = find.widgetWithText(
            OutlinedButton,
            'Yêu cầu đăng ký lại',
          );
          await tester.ensureVisible(reject);
          await tester.tap(reject);
          await tester.pump();
          expect(requests.length, 1);
          await tester.ensureVisible(find.byType(TextField));
          await tester.enterText(
            find.byType(TextField),
            'Thiếu ảnh thẻ trong hồ sơ',
          );
          await tester.ensureVisible(reject);
          await tester.tap(reject);
        }
        await tester.pumpAndSettle();
        expect(requests.length, 2);
        expect(find.text('Mở hồ sơ'), findsOneWidget);
        expect(tester.takeException(), isNull);
      },
    );
  }
}
