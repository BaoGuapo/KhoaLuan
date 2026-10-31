import 'dart:convert';

import 'package:http/http.dart' as http;

import '../api/api_constants.dart';
import '../auth/token_storage.dart';

class StudentPortalException implements Exception {
  const StudentPortalException(this.message);
  final String message;

  @override
  String toString() => message;
}

class StudentContact {
  const StudentContact({
    required this.studentCode,
    required this.fullName,
    required this.email,
    this.phone = '',
    this.address = '',
    this.emergencyPhone = '',
    this.className = '',
    this.major = '',
    this.cohort = '',
    this.birthDate,
  });

  final String studentCode;
  final String fullName;
  final String email;
  final String phone;
  final String address;
  final String emergencyPhone;
  final String className;
  final String major;
  final String cohort;
  final DateTime? birthDate;

  factory StudentContact.fromJson(Map<String, dynamic> json) => StudentContact(
        studentCode: '${json['maSinhVien'] ?? ''}',
        fullName: '${json['hoTen'] ?? ''}',
        email: '${json['email'] ?? ''}',
        phone: '${json['soDienThoai'] ?? ''}',
        address: '${json['diaChi'] ?? ''}',
        emergencyPhone: '${json['soDienThoaiNguoiThan'] ?? ''}',
        className: '${json['lopSinhHoat'] ?? ''}',
        major: '${json['nganhHoc'] ?? ''}',
        cohort: '${json['khoaHoc'] ?? ''}',
        birthDate: DateTime.tryParse('${json['ngaySinh'] ?? ''}'),
      );
}

class SemesterGrades {
  const SemesterGrades({
    required this.code,
    required this.name,
    required this.creditsWithGrades,
    required this.gpa,
    required this.courseCount,
    required this.courses,
  });

  final String code;
  final String name;
  final int creditsWithGrades;
  final double? gpa;
  final int courseCount;
  final List<Map<String, dynamic>> courses;

  factory SemesterGrades.fromJson(Map<String, dynamic> json) => SemesterGrades(
        code: '${json['maHocKy'] ?? ''}',
        name: '${json['tenHocKy'] ?? ''}',
        creditsWithGrades: _asInt(json['tongTinChi']),
        gpa: _asDouble(json['gpaHeBon']),
        courseCount: _asInt(json['soMon']),
        courses: (json['monHoc'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .toList(),
      );
}

class AcademicResults {
  const AcademicResults({
    required this.isDemo,
    required this.cumulativeGpa,
    required this.gradedCredits,
    required this.semesters,
  });

  final bool isDemo;
  final double? cumulativeGpa;
  final int gradedCredits;
  final List<SemesterGrades> semesters;

  factory AcademicResults.fromJson(Map<String, dynamic> json) =>
      AcademicResults(
        isDemo: json['laDuLieuDemo'] == true,
        cumulativeGpa: _asDouble(json['gpaTichLuyHeBon']),
        gradedCredits: _asInt(json['tongTinChiDaCoDiem']),
        semesters: (json['hocKy'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(SemesterGrades.fromJson)
            .toList(),
      );
}

class TuitionFee {
  const TuitionFee({
    required this.id,
    required this.studentCode,
    required this.semesterCode,
    required this.total,
    required this.paid,
    required this.dueDate,
    required this.note,
    required this.status,
    this.isDemo = false,
    this.studentName = '',
  });

  final String id;
  final String studentCode;
  final String semesterCode;
  final double total;
  final double paid;
  final DateTime dueDate;
  final String note;
  final String status;
  final bool isDemo;
  final String studentName;
  double get remaining => total - paid;
  String get semesterLabel {
    final parts = semesterCode.split('-');
    final year = parts.length == 2 ? int.tryParse(parts[0]) : null;
    if (year != null && year >= 1900 && year <= 9999) {
      return 'Học kỳ ${parts[1]} · $year–${year + 1}';
    }
    return semesterCode;
  }

  factory TuitionFee.fromJson(Map<String, dynamic> json) => TuitionFee(
        id: '${json['id'] ?? ''}',
        studentCode: '${json['maSinhVien'] ?? ''}',
        studentName: '${json['hoTen'] ?? ''}',
        semesterCode: '${json['maHocKy'] ?? ''}',
        total: _asDouble(json['tongHocPhi']) ?? 0,
        paid: _asDouble(json['daThanhToan']) ?? 0,
        dueDate: DateTime.tryParse('${json['hanThanhToan'] ?? ''}') ??
            DateTime.now(),
        note: '${json['ghiChu'] ?? ''}',
        status: '${json['trangThai'] ?? ''}',
        isDemo: json['laDuLieuDemo'] == true,
      );
}

class StudentPortalService {
  Future<StudentContact> getProfile() async {
    final json = await _getObject(ApiConstants.studentPortalProfile);
    return StudentContact.fromJson(json);
  }

  Future<void> updateContact({
    required String phone,
    required String address,
    required String emergencyPhone,
  }) async {
    await _send(
      'PUT',
      ApiConstants.studentContact,
      {
        'soDienThoai': phone.trim(),
        'diaChi': address.trim(),
        'soDienThoaiNguoiThan': emergencyPhone.trim(),
      },
      expectedStatus: 204,
    );
  }

  Future<AcademicResults> getGrades() async =>
      AcademicResults.fromJson(await _getObject(ApiConstants.studentGrades));

  Future<List<TuitionFee>> getMyFees() async =>
      (await _getList(ApiConstants.studentFees))
          .map(TuitionFee.fromJson)
          .toList();

  Future<List<TuitionFee>> getFeesForManagement({String? studentCode}) async {
    final query = studentCode == null || studentCode.trim().isEmpty
        ? ''
        : '?maSinhVien=${Uri.encodeQueryComponent(studentCode.trim())}';
    return (await _getList('${ApiConstants.feeManagement}$query'))
        .map(TuitionFee.fromJson)
        .toList();
  }

  Future<void> updateFee({
    required String id,
    required double total,
    required double paid,
    required DateTime dueDate,
    required String note,
  }) async {
    await _send(
      'PUT',
      '${ApiConstants.feeManagement}/$id',
      {
        'tongHocPhi': total,
        'daThanhToan': paid,
        'hanThanhToan': dueDate.toIso8601String(),
        'ghiChu': note.trim(),
      },
      expectedStatus: 204,
    );
  }

  Future<Map<String, dynamic>> _getObject(String path) async {
    final response = await _request('GET', path);
    if (response.statusCode != 200) _throwResponse(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! Map<String, dynamic>) {
      throw const StudentPortalException('Dữ liệu máy chủ không hợp lệ.');
    }
    return decoded;
  }

  Future<List<Map<String, dynamic>>> _getList(String path) async {
    final response = await _request('GET', path);
    if (response.statusCode != 200) _throwResponse(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! List) {
      throw const StudentPortalException('Dữ liệu máy chủ không hợp lệ.');
    }
    return decoded.whereType<Map<String, dynamic>>().toList();
  }

  Future<void> _send(
    String method,
    String path,
    Map<String, dynamic> body, {
    required int expectedStatus,
  }) async {
    final response = await _request(method, path, body: body);
    if (response.statusCode != expectedStatus) _throwResponse(response);
  }

  Future<http.Response> _request(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final token = await TokenStorage.getAccessToken();
    if (token == null || token.isEmpty) {
      throw const StudentPortalException('Vui lòng đăng nhập lại.');
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
                const Duration(seconds: 10),
              );
        case 'PUT':
          return await http
              .put(uri, headers: headers, body: jsonEncode(body))
              .timeout(const Duration(seconds: 10));
        default:
          throw ArgumentError.value(
              method, 'method', 'Unsupported HTTP method');
      }
    } on StudentPortalException {
      rethrow;
    } catch (error) {
      throw StudentPortalException('Không thể kết nối máy chủ: $error');
    }
  }

  Never _throwResponse(http.Response response) {
    try {
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic> &&
          decoded['message'] is String &&
          (decoded['message'] as String).isNotEmpty) {
        throw StudentPortalException(decoded['message'] as String);
      }
    } on FormatException {
      // Fall through to a status-based error for non-JSON responses.
    }
    throw StudentPortalException(
      'Yêu cầu thất bại (mã lỗi ${response.statusCode}).',
    );
  }
}

int _asInt(Object? value) =>
    value is num ? value.toInt() : int.tryParse('$value') ?? 0;

double? _asDouble(Object? value) =>
    value is num ? value.toDouble() : double.tryParse('$value');
