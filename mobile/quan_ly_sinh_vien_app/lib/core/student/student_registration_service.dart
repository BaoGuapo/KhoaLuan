import 'dart:convert';

import 'package:http/http.dart' as http;

import '../api/api_constants.dart';
import '../auth/token_storage.dart';

class StudentRegistrationService {
  static const _base = '/api/v1/dang-ky-hoc-phan';

  Future<Map<String, dynamic>> getStudentProfile() async {
    final user = await _getObject(ApiConstants.userProfile);
    return {
      'maSinhVien': user['profileCode'] ?? user['userName'],
      'hoTen': user['fullName'],
    };
  }

  Future<List<Map<String, dynamic>>> getSubjects(String type) async {
    final response = await _getObject(
      '$_base/danh-sach?loaiDangKy=${Uri.encodeQueryComponent(type)}',
    );
    final sections = (response['data'] as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>();
    final courses = <String, Map<String, dynamic>>{};

    for (final section in sections) {
      final code = '${section['maMonHoc'] ?? ''}';
      if (code.isEmpty) continue;
      final course = courses.putIfAbsent(
        code,
        () => {
          'maMonHoc': code,
          'tenMonHoc': section['tenMonHoc'],
          'tongSoTinChi': _asInt(section['soTinChiLyThuyet']) +
              _asInt(section['soTinChiThucHanh']),
          'nhomHocPhans': <Map<String, dynamic>>[],
        },
      );
      (course['nhomHocPhans'] as List<Map<String, dynamic>>).add(
        _mapSection(section),
      );
    }

    return courses.values.toList();
  }

  Future<Map<String, dynamic>> getSelectedSections() async {
    final sections = await _getList('$_base/da-dang-ky');
    final courses = sections
        .map((section) => {
              ..._mapSection(section),
              'tenMonHoc': section['tenMonHoc'],
              'tongSoTinChi': _asInt(section['soTinChiLyThuyet']) +
                  _asInt(section['soTinChiThucHanh']),
            })
        .toList();
    return {
      'danhSachHocPhan': courses,
      'tongSoTinChi': courses.fold<int>(
        0,
        (total, course) => total + _asInt(course['tongSoTinChi']),
      ),
      'tongSoMon': courses.length,
      'tongSoTiet': 0,
    };
  }

  Future<void> register(String sectionCode, String type) => _send(
        'POST',
        '$_base/sinh-vien-tu-dang-ky/'
            '${Uri.encodeComponent(sectionCode)}'
            '?loaiDangKy=${Uri.encodeQueryComponent(type)}',
      );

  Map<String, dynamic> _mapSection(Map<String, dynamic> section) {
    final schedules =
        (section['lichHoc'] as List<dynamic>? ?? const []).whereType<Map>();
    final scheduleLabels = <String>[];
    final rooms = <String>{};
    for (final schedule in schedules) {
      final day = _asInt(schedule['thuTrongTuan']);
      final start = schedule['tietBatDau'];
      final end = schedule['tietKetThuc'];
      scheduleLabels.add('Thứ $day · tiết $start-$end');
      final room = schedule['tenPhong']?.toString();
      if (room != null && room.isNotEmpty) rooms.add(room);
    }

    final capacity = _asInt(section['siSoToiDa']);
    final enrolled = _asInt(section['siSoHienTai']);
    return {
      'maLopHocPhan': section['maLopHocPhan'],
      'tenNhom': section['maLopHocPhan'],
      'tenGiangVien': section['hoTen'],
      'lichHoc':
          scheduleLabels.isEmpty ? 'Chưa có lịch' : scheduleLabels.join(', '),
      'phongHoc': rooms.isEmpty ? 'Chưa xếp phòng' : rooms.join(', '),
      'siSoToiDa': capacity,
      'siSoHienTai': enrolled,
      'conTrong': (capacity - enrolled).clamp(0, capacity),
      'daDangKy': section['daDangKy'] == true,
      'lyDoKhongDuDieuKien': section['lyDoKhongDuDieuKien'],
      'tongSoTinChi': _asInt(section['soTinChiLyThuyet']) +
          _asInt(section['soTinChiThucHanh']),
    };
  }

  Future<Map<String, dynamic>> _getObject(String path) async {
    final response = await _request('GET', path);
    _ensureSuccess(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! Map<String, dynamic>) {
      throw const StudentRegistrationException('Dữ liệu máy chủ không hợp lệ.');
    }
    return decoded;
  }

  Future<List<Map<String, dynamic>>> _getList(String path) async {
    final response = await _request('GET', path);
    _ensureSuccess(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! List) {
      throw const StudentRegistrationException('Dữ liệu máy chủ không hợp lệ.');
    }
    return decoded.whereType<Map<String, dynamic>>().toList();
  }

  Future<void> _send(String method, String path,
      [Map<String, dynamic>? body]) async {
    _ensureSuccess(await _request(method, path, body: body));
  }

  Future<http.Response> _request(String method, String path,
      {Map<String, dynamic>? body}) async {
    final token = await TokenStorage.getAccessToken();
    if (token == null || token.isEmpty || token.startsWith('mock_')) {
      throw const StudentRegistrationException(
          'Đăng nhập bằng tài khoản máy chủ để đăng ký học phần.');
    }
    final uri = Uri.parse('${ApiConstants.baseUrl}$path');
    final headers = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
    try {
      switch (method) {
        case 'GET':
          return await http.get(uri, headers: headers).timeout(
                const Duration(seconds: 12),
              );
        case 'POST':
          final request = body == null
              ? http.post(uri, headers: headers)
              : http.post(uri, headers: headers, body: jsonEncode(body));
          return await request.timeout(const Duration(seconds: 12));
        case 'DELETE':
          return await http.delete(uri, headers: headers).timeout(
                const Duration(seconds: 12),
              );
        default:
          throw ArgumentError.value(
              method, 'method', 'Unsupported HTTP method');
      }
    } on StudentRegistrationException {
      rethrow;
    } catch (error) {
      throw StudentRegistrationException('Không thể kết nối máy chủ: $error');
    }
  }

  void _ensureSuccess(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) return;
    try {
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic> && decoded['message'] is String) {
        throw StudentRegistrationException(decoded['message'] as String);
      }
    } on FormatException {
      // Use a status-based message for non-JSON responses.
    }
    throw StudentRegistrationException(
        'Yêu cầu thất bại (mã lỗi ${response.statusCode}).');
  }
}

int _asInt(Object? value) =>
    value is num ? value.toInt() : int.tryParse('$value') ?? 0;

class StudentRegistrationException implements Exception {
  const StudentRegistrationException(this.message);
  final String message;

  @override
  String toString() => message;
}
