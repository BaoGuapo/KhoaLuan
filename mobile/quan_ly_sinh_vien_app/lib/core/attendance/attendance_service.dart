import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../api/api_constants.dart';
import '../auth/auth_provider.dart';
import 'attendance_models.dart';

class AttendanceService {
  static final AttendanceService _instance = AttendanceService._internal();
  factory AttendanceService() => _instance;
  AttendanceService._internal();

  Future<List<TodaySessionInfo>> getSessionsToday() async {
    final token = AuthProvider.instance.accessToken;
    if (token == null || token.isEmpty) {
      throw const AttendanceServiceException(
        'Đăng nhập máy chủ để tải danh sách buổi học.',
      );
    }

    final response = await http
        .get(
          Uri.parse('${ApiConstants.baseUrl}${ApiConstants.buoiHocHomNay}'),
          headers: {
            'Authorization': 'Bearer $token',
            'Content-Type': 'application/json',
          },
        )
        .timeout(const Duration(seconds: 10));

    if (response.statusCode != 200) {
      throw AttendanceServiceException(
        'Không tải được buổi học (mã lỗi ${response.statusCode}).',
      );
    }

    final List<dynamic> list;
    try {
      list = jsonDecode(response.body) as List<dynamic>;
    } on FormatException {
      throw const AttendanceServiceException('Dữ liệu buổi học không hợp lệ.');
    }

    return list.map((item) {
      if (item is! Map<String, dynamic>) {
        return TodaySessionInfo(
          maBuoiHoc: '',
          tenMonHoc: '',
          maMonHoc: '',
          maLopHocPhan: '',
          giangVien: '',
          thoiGian: '',
          phong: '',
          trangThaiBuoiHoc: '',
          daDiemDanh: false,
        );
      }
      return TodaySessionInfo(
        maBuoiHoc: item['MaBuoiHoc']?.toString() ?? '',
        tenMonHoc: item['TenMonHoc']?.toString() ?? 'Chưa có tên môn',
        maMonHoc: item['MaMonHoc']?.toString() ?? '',
        maLopHocPhan: item['MaLopHocPhan']?.toString() ?? '',
        giangVien: item['GiangVien']?.toString() ?? '',
        thoiGian: item['ThoiGian']?.toString() ?? '',
        phong: item['Phong']?.toString() ?? '',
        trangThaiBuoiHoc: item['TrangThaiBuoiHoc']?.toString() ?? 'Unknown',
        daDiemDanh: item['DaDiemDanh'] == true,
      );
    }).toList();
  }

  Future<AttendanceResult> checkInWithFace({
    required String maBuoiHoc,
    required List<double> descriptors,
  }) async {
    final token = AuthProvider.instance.accessToken;
    if (token == null || token.isEmpty) {
      return AttendanceResult.networkError('Chưa đăng nhập.');
    }

    try {
      final response = await http
          .post(
            Uri.parse(
              '${ApiConstants.baseUrl}${ApiConstants.diemDanhKhuonMat(maBuoiHoc)}',
            ),
            headers: {
              'Authorization': 'Bearer $token',
              'Content-Type': 'application/json',
            },
            body: jsonEncode({'Descriptors': [descriptors]}),
          )
          .timeout(const Duration(seconds: 30));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        return AttendanceResult(
          code: AttendanceResultCode.success,
          thanhCong: true,
          thongBao: data['thongBao']?.toString() ?? 'Điểm danh thành công!',
          maSinhVien: data['maSinhVien']?.toString(),
          hoTen: data['hoTen']?.toString(),
          maBuoiHoc: data['maBuoiHoc']?.toString(),
          trangThai: data['trangThai']?.toString(),
          thoiGianCheckInUtc: data['thoiGianCheckIn'] != null
              ? DateTime.tryParse(data['thoiGianCheckIn'].toString())
              : null,
          existingCheckInUtc: data['daDiemDanhTruocDo'] == true
              ? DateTime.tryParse(data['thoiGianCheckIn'].toString())
              : null,
        );
      }

      final errorData = _tryDecode(response.body);
      final code = errorData?['code']?.toString() ?? '';
      final message = errorData?['message']?.toString() ??
          'Điểm danh thất bại (mã lỗi ${response.statusCode}).';

      AttendanceResultCode resultCode;
      switch (code) {
        case 'AlreadyCheckedIn':
          resultCode = AttendanceResultCode.alreadyCheckedIn;
        case 'FaceNotMatched':
        case 'FaceMismatch':
          resultCode = AttendanceResultCode.faceMismatch;
        case 'NotEnrolled':
          resultCode = AttendanceResultCode.notEnrolled;
        case 'AttendanceFinalized':
          resultCode = AttendanceResultCode.sessionNotFound;
        default:
          resultCode = AttendanceResultCode.unknownError;
      }

      return AttendanceResult(
        code: resultCode,
        thanhCong: false,
        thongBao: message,
      );
    } on SocketException {
      return AttendanceResult.networkError('Không thể kết nối đến máy chủ.');
    } on TimeoutException {
      return AttendanceResult.networkError('Máy chủ phản hồi quá chậm.');
    } on FormatException {
      return AttendanceResult.networkError('Dữ liệu từ máy chủ không hợp lệ.');
    } catch (e) {
      return AttendanceResult.networkError('Lỗi không xác định: $e');
    }
  }

  Map<String, dynamic>? _tryDecode(String body) {
    try {
      final decoded = jsonDecode(body);
      return decoded is Map<String, dynamic> ? decoded : null;
    } on FormatException {
      return null;
    }
  }
}

class AttendanceServiceException implements Exception {
  const AttendanceServiceException(this.message);

  final String message;

  @override
  String toString() => message;
}
