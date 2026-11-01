import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:quan_ly_sinh_vien_app/core/attendance/attendance_workflow_api.dart';

void main() {
  test(
    'enrollment explicitly consents to protected review-photo storage',
    () async {
      final api = AttendanceWorkflowApi(
        tokenProvider: () async => 'student',
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/qr-diem-danh/ho-so');
          final body = jsonDecode(request.body) as Map;
          expect(body['consent'], true);
          expect(body['storeReviewPhotosConsent'], true);
          return http.Response('{}', 200);
        }),
      );
      addTearDown(api.close);
      await api.submit(
        {'challengeId': 'nonce'},
        ['a', 'b', 'c'],
        enrollment: true,
      );
    },
  );
  test(
    'live guidance sends the exact camera frame and owned challenge, and reads server pose',
    () async {
      final api = AttendanceWorkflowApi(
        tokenProvider: () async => 'token',
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/qr-diem-danh/huong-dan');
          expect(jsonDecode(request.body), {
            'challengeId': 'nonce',
            'frame': base64Encode([1, 2, 3]),
          });
          return http.Response(
            jsonEncode({
              'usable': true,
              'message': 'OK',
              'pose': -.18,
              'minimumTurn': .15,
            }),
            200,
          );
        }),
      );
      addTearDown(api.close);
      final observation = await api.inspectPose('nonce', [1, 2, 3]);
      expect(observation.usable, isTrue);
      expect(observation.pose, -.18);
      expect(observation.minimumTurn, .15);
    },
  );
  test(
    'QR scan uses authenticated account and preserves server rejection',
    () async {
      final client = MockClient((request) async {
        expect(request.headers['Authorization'], 'Bearer student-token');
        expect(jsonDecode(request.body), {'qrToken': 'qr-from-teacher'});
        return http.Response(
          jsonEncode({
            'code': 'NotEnrolled',
            'message': 'Sinh viên không thuộc lớp.',
          }),
          403,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      });
      final api = AttendanceWorkflowApi(
        client: client,
        baseUri: Uri.parse('https://school.invalid/'),
        tokenProvider: () async => 'student-token',
      );
      addTearDown(api.close);
      await expectLater(
        api.scan('qr-from-teacher'),
        throwsA(
          isA<AttendanceWorkflowException>().having(
            (e) => e.code,
            'code',
            'NotEnrolled',
          ),
        ),
      );
    },
  );
  test(
    'Missing session cannot submit evidence or make a network request',
    () async {
      final api = AttendanceWorkflowApi(
        client: MockClient((_) async => throw StateError('Must not send')),
        tokenProvider: () async => null,
      );
      addTearDown(api.close);
      await expectLater(
        api.submit({'challengeId': 'id'}, ['a', 'b', 'c']),
        throwsA(
          isA<AttendanceWorkflowException>().having(
            (e) => e.code,
            'code',
            'Unauthorized',
          ),
        ),
      );
    },
  );
  test(
    'Evidence submission sends frames and nonce without client identity or live verdict',
    () async {
      final api = AttendanceWorkflowApi(
        tokenProvider: () async => 'token',
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/qr-diem-danh/xac-nhan');
          final body = jsonDecode(request.body) as Map;
          expect(body.keys.toSet(), {'challengeId', 'frames', 'consent'});
          return http.Response(jsonEncode({'trangThai': 'CoMat'}), 200);
        }),
      );
      addTearDown(api.close);
      expect(
        (await api.submit({'challengeId': 'id'}, ['a', 'b', 'c']))['trangThai'],
        'CoMat',
      );
    },
  );
}
