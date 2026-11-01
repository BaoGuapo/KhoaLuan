import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';

class ApiConstants {
  static const _configuredBaseUrl = String.fromEnvironment('API_BASE_URL');

  // Android Emulator reaches the host machine through 10.0.2.2.
  static String get baseUrl {
    if (_configuredBaseUrl.isNotEmpty) return _configuredBaseUrl;
    if (kIsWeb) return 'http://localhost:5081';
    try {
      if (Platform.isAndroid) {
        return 'http://10.0.2.2:5081';
      }
    } catch (_) {}
    return 'http://localhost:5081';
  }

  // Auth endpoints
  static const String login = '/api/auth/login';
  static const String logout = '/api/auth/logout';
  static const String changePassword = '/api/auth/change-password';
  static const String refreshToken = '/api/auth/refresh-token';

  // Feature endpoints
  static const String userProfile = '/api/v1/nguoi-dung/me';
  static const String studentPortalProfile = '/api/v1/me/ho-so-sinh-vien';
  static const String studentContact = '/api/v1/me/ho-so-sinh-vien/lien-he';
  static const String studentGrades = '/api/v1/me/ket-qua-hoc-tap';
  static const String studentFees = '/api/v1/me/hoc-phi';
  static const String feeManagement = '/api/v1/quan-ly/hoc-phi';
  static const String studentSchedule = '/api/lich-hoc/sinh-vien';
  static const String notifications = '/api/v1/thong-bao';
  static const String notificationDevice = '/api/v1/thong-bao/thiet-bi';
  static const String classes = '/api/dao-tao/lop-hoc-phan';

  // Attendance (Face Recognition) endpoints
  static const String buoiHocHomNay = '/api/v1/sinh-vien/buoi-hoc-hom-nay';
  static const String registerFace = '/api/v1/me/ho-so-khuon-mat/dang-ky';
  static const String managedFaceEnrollments =
      '/api/v1/quan-ly/ho-so-khuon-mat';
  static String deleteStudentFaceEnrollment(String maSinhVien) =>
      '/api/v1/sinh-vien/$maSinhVien/ho-so-khuon-mat';
  static String diemDanhKhuonMat(String maBuoiHoc) =>
      '/api/v1/buoi-hoc/$maBuoiHoc/diem-danh-khuon-mat';
}
