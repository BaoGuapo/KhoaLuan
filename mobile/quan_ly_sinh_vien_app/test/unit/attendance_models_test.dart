import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/attendance/attendance_models.dart';

void main() {
  group('TodaySessionInfo.fromJson', () {
    test('parse đầy đủ buổi học hôm nay', () {
      final json = {
        'maBuoiHoc': 'BUOI-20261025-CS101-N01',
        'tenMonHoc': 'Lập trình hướng đối tượng',
        'maMonHoc': 'CS101',
        'maLopHocPhan': 'CS101-N01',
        'giangVien': 'TS. Nguyễn Văn Bình',
        'thoiGian': 'Tiết 1 - 3',
        'phong': 'E301',
        'trangThaiBuoiHoc': 'DangDienRa',
        'daDiemDanh': false,
        'thoiGianDiemDanhUtc': null,
      };
      final session = TodaySessionInfo.fromJson(json);

      expect(session.maBuoiHoc, 'BUOI-20261025-CS101-N01');
      expect(session.tenMonHoc, 'Lập trình hướng đối tượng');
      expect(session.maLopHocPhan, 'CS101-N01');
      expect(session.giangVien, 'TS. Nguyễn Văn Bình');
      expect(session.phong, 'E301');
      expect(session.trangThaiBuoiHoc, 'DangDienRa');
      expect(session.daDiemDanh, isFalse);
      expect(session.thoiGianDiemDanhUtc, isNull);
    });

    test('daDiemDanh = true và parse timestamp check-in', () {
      final json = {
        'maBuoiHoc': 'BUOI-001',
        'tenMonHoc': 'Môn A',
        'maMonHoc': 'MA',
        'maLopHocPhan': 'MA-N01',
        'giangVien': 'GV01',
        'thoiGian': 'Tiết 1 - 2',
        'phong': 'A101',
        'trangThaiBuoiHoc': 'KetThuc',
        'daDiemDanh': true,
        'thoiGianDiemDanhUtc': '2026-10-25T02:00:00Z',
      };
      final session = TodaySessionInfo.fromJson(json);

      expect(session.daDiemDanh, isTrue);
      expect(session.thoiGianDiemDanhUtc, isNotNull);
      expect(session.thoiGianDiemDanhUtc!.isUtc, isTrue);
    });

    test('daDiemDanh null → false', () {
      final json = {
        'maBuoiHoc': 'B',
        'tenMonHoc': 'X',
        'maMonHoc': 'X',
        'maLopHocPhan': 'X',
        'giangVien': 'G',
        'thoiGian': 'T',
        'phong': 'P',
        'trangThaiBuoiHoc': 'S',
      };
      final session = TodaySessionInfo.fromJson(json);
      expect(session.daDiemDanh, isFalse);
    });

    test('fallback tioGian từ tietBatDau/tietKetThuc khi thiếu thoiGian', () {
      final json = {
        'maBuoiHoc': 'B2',
        'maMonHoc': 'Y',
        'maLopHocPhan': 'Y',
        'giangVien': 'G',
        'phong': 'P',
        'trangThaiBuoiHoc': 'S',
        'tietBatDau': 4,
        'tietKetThuc': 6,
      };
      final session = TodaySessionInfo.fromJson(json);
      expect(session.thoiGian, contains('4'));
      expect(session.thoiGian, contains('6'));
    });
  });

  group('AttendanceResult.fromSuccessJson', () {
    test('parse kết quả điểm danh thành công', () {
      final json = {
        'thanhCong': true,
        'maSinhVien': '2001230048',
        'hoTen': 'Nguyễn Văn An',
        'maBuoiHoc': 'BUOI-001',
        'tenMonHoc': 'Lập trình',
        'thoiGianCheckInUtc': '2026-10-25T02:30:00Z',
        'trangThai': 'CoMat',
        'thongBao': 'Điểm danh thành công! Khuôn mặt đã khớp với hồ sơ FaceID.',
      };
      final result = AttendanceResult.fromSuccessJson(json);

      expect(result.thanhCong, isTrue);
      expect(result.code, AttendanceResultCode.success);
      expect(result.maSinhVien, '2001230048');
      expect(result.hoTen, 'Nguyễn Văn An');
      expect(result.tenMonHoc, 'Lập trình');
      expect(result.trangThai, 'CoMat');
      expect(result.thoiGianCheckInUtc, isNotNull);
      expect(result.thongBao,
          contains('Điểm danh thành công'));
    });
  });

  group('AttendanceResult.fromErrorJson', () {
    test('AlreadyCheckedIn → code alreadyCheckedIn', () {
      final result = AttendanceResult.fromErrorJson(
        409,
        {'code': 'AlreadyCheckedIn', 'message': 'Đã điểm danh rồi.'},
      );
      expect(result.code, AttendanceResultCode.alreadyCheckedIn);
      expect(result.thanhCong, isFalse);
      expect(result.thongBao, 'Đã điểm danh rồi.');
    });

    test('SpoofDetected → code spoofDetected với livenessScore', () {
      final result = AttendanceResult.fromErrorJson(
        400,
        {
          'code': 'SpoofDetected',
          'message': 'Kiểm tra chống giả mạo không đạt.',
          'livenessScore': 42.5,
        },
      );
      expect(result.code, AttendanceResultCode.spoofDetected);
      expect(result.livenessScore, closeTo(42.5, 0.001));
    });

    test('NoFaceDetected → code noFaceDetected', () {
      final result = AttendanceResult.fromErrorJson(
        400,
        {'code': 'NoFaceDetected', 'message': 'Không tìm thấy khuôn mặt.'},
      );
      expect(result.code, AttendanceResultCode.noFaceDetected);
    });

    test('MultipleFaces → code multipleFaces', () {
      final result = AttendanceResult.fromErrorJson(
        400,
        {'code': 'MultipleFaces', 'message': 'Nhiều khuôn mặt.'},
      );
      expect(result.code, AttendanceResultCode.multipleFaces);
    });

    test('FaceMismatch → code faceMismatch', () {
      final result = AttendanceResult.fromErrorJson(
        403,
        {'code': 'FaceMismatch', 'message': 'Khuôn mặt không khớp.'},
      );
      expect(result.code, AttendanceResultCode.faceMismatch);
    });

    test('NotEnrolled → code notEnrolled', () {
      final result = AttendanceResult.fromErrorJson(
        403,
        {'code': 'NotEnrolled', 'message': 'Chưa đăng ký lớp.'},
      );
      expect(result.code, AttendanceResultCode.notEnrolled);
    });

    test('AiServiceUnavailable → code aiServiceUnavailable', () {
      final result = AttendanceResult.fromErrorJson(
        503,
        {'code': 'AiServiceUnavailable', 'message': 'Dịch vụ AI không khả dụng.'},
      );
      expect(result.code, AttendanceResultCode.aiServiceUnavailable);
    });

    test('mã lỗi 409 không có code → alreadyCheckedIn', () {
      final result = AttendanceResult.fromErrorJson(409, {});
      expect(result.code, AttendanceResultCode.alreadyCheckedIn);
    });

    test('json = null → unknownError', () {
      final result = AttendanceResult.fromErrorJson(500, null);
      expect(result.code, AttendanceResultCode.unknownError);
    });

    test('mã 401 → thông báo hết hạn phiên', () {
      final result = AttendanceResult.fromErrorJson(401, null);
      expect(result.thongBao, contains('hết hạn'));
    });

    test('mã 503 → thông báo AI không khả dụng', () {
      final result = AttendanceResult.fromErrorJson(503, null);
      expect(result.thongBao, contains('AI'));
    });
  });

  group('AttendanceResult.networkError', () {
    test('tạo kết quả lỗi mạng đúng cấu trúc', () {
      final result = AttendanceResult.networkError('Timeout sau 10s');
      expect(result.code, AttendanceResultCode.networkError);
      expect(result.thanhCong, isFalse);
      expect(result.thongBao, contains('Timeout'));
    });
  });
}
