import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/data/models/auth_session.dart';

void main() {
  test('đọc trạng thái bắt buộc đổi mật khẩu từ response API', () {
    final session = AuthSession.fromJson({
      'accessToken': 'access-token',
      'refreshToken': 'refresh-token',
      'accessTokenExpiresAtUtc': '2030-01-01T00:00:00Z',
      'userName': '2001239999',
      'roles': ['SinhVien'],
      'mustChangePassword': true,
    });

    expect(session.userName, '2001239999');
    expect(session.roles, ['SinhVien']);
    expect(session.mustChangePassword, isTrue);
  });

  test('tương thích response cũ chưa có mustChangePassword', () {
    final session = AuthSession.fromJson({
      'accessToken': 'access-token',
      'refreshToken': 'refresh-token',
      'accessTokenExpiresAtUtc': '2030-01-01T00:00:00Z',
      'userName': 'GV001',
      'roles': ['GiangVien'],
    });

    expect(session.mustChangePassword, isFalse);
  });
}
