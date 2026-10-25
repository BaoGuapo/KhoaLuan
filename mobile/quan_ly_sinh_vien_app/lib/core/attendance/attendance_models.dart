/// Models cho chức năng điểm danh nhận diện khuôn mặt.
library;

// ─── Thông tin một buổi học hôm nay ──────────────────────────────────────────

class TodaySessionInfo {
  final String maBuoiHoc;
  final String tenMonHoc;
  final String maMonHoc;
  final String maLopHocPhan;
  final String giangVien;
  final String thoiGian;
  final String phong;
  final String trangThaiBuoiHoc;
  final bool daDiemDanh;
  final DateTime? thoiGianDiemDanhUtc;

  const TodaySessionInfo({
    required this.maBuoiHoc,
    required this.tenMonHoc,
    required this.maMonHoc,
    required this.maLopHocPhan,
    required this.giangVien,
    required this.thoiGian,
    required this.phong,
    required this.trangThaiBuoiHoc,
    required this.daDiemDanh,
    this.thoiGianDiemDanhUtc,
  });

  factory TodaySessionInfo.fromJson(Map<String, dynamic> json) {
    return TodaySessionInfo(
      maBuoiHoc: json['maBuoiHoc']?.toString() ?? '',
      tenMonHoc: json['tenMonHoc']?.toString() ?? 'Chưa có tên môn',
      maMonHoc: json['maMonHoc']?.toString() ?? '',
      maLopHocPhan: json['maLopHocPhan']?.toString() ?? '',
      giangVien: json['giangVien']?.toString() ?? '',
      thoiGian: json['thoiGian']?.toString() ??
          'Tiết ${json['tietBatDau'] ?? '?'} - ${json['tietKetThuc'] ?? '?'}',
      phong: json['phong']?.toString() ?? '',
      trangThaiBuoiHoc: json['trangThaiBuoiHoc']?.toString() ?? 'Unknown',
      daDiemDanh: json['daDiemDanh'] == true,
      thoiGianDiemDanhUtc: json['thoiGianDiemDanhUtc'] != null
          ? DateTime.tryParse(json['thoiGianDiemDanhUtc'].toString())
          : null,
    );
  }
}

// ─── Kết quả từ API điểm danh ────────────────────────────────────────────────

/// Trạng thái kết quả từ backend sau khi gọi diem-danh-khuon-mat.
enum AttendanceResultCode {
  /// Điểm danh thành công, đã ghi nhận vào SQL Server.
  success,

  /// Sinh viên đã điểm danh trong buổi học này rồi.
  alreadyCheckedIn,

  /// Không tìm thấy khuôn mặt nào trong ảnh.
  noFaceDetected,

  /// Nhiều khuôn mặt trong ảnh.
  multipleFaces,

  /// Khuôn mặt bị phát hiện là giả mạo (ảnh in, màn hình).
  spoofDetected,

  /// Khuôn mặt không có trong hệ thống AI.
  unknownFace,

  /// Khuôn mặt nhận diện không khớp với tài khoản sinh viên.
  faceMismatch,

  /// Sinh viên không đăng ký lớp học phần này.
  notEnrolled,

  /// Không tìm thấy buổi học.
  sessionNotFound,

  /// Dịch vụ AI không khả dụng.
  aiServiceUnavailable,

  /// Lỗi mạng / timeout.
  networkError,

  /// Lỗi không xác định.
  unknownError,
}

class AttendanceResult {
  final AttendanceResultCode code;
  final bool thanhCong;
  final String thongBao;

  // Thông tin khi thành công
  final String? maSinhVien;
  final String? hoTen;
  final String? maBuoiHoc;
  final String? tenMonHoc;
  final DateTime? thoiGianCheckInUtc;
  final String? trangThai;

  // Thông tin bổ sung khi lỗi
  final double? livenessScore;
  final String? detectedName;
  final String? studentName;
  final DateTime? existingCheckInUtc;

  const AttendanceResult({
    required this.code,
    required this.thanhCong,
    required this.thongBao,
    this.maSinhVien,
    this.hoTen,
    this.maBuoiHoc,
    this.tenMonHoc,
    this.thoiGianCheckInUtc,
    this.trangThai,
    this.livenessScore,
    this.detectedName,
    this.studentName,
    this.existingCheckInUtc,
  });

  factory AttendanceResult.fromSuccessJson(Map<String, dynamic> json) {
    return AttendanceResult(
      code: AttendanceResultCode.success,
      thanhCong: true,
      thongBao: json['thongBao']?.toString() ?? 'Điểm danh thành công!',
      maSinhVien: json['maSinhVien']?.toString(),
      hoTen: json['hoTen']?.toString(),
      maBuoiHoc: json['maBuoiHoc']?.toString(),
      tenMonHoc: json['tenMonHoc']?.toString(),
      thoiGianCheckInUtc: json['thoiGianCheckInUtc'] != null
          ? DateTime.tryParse(json['thoiGianCheckInUtc'].toString())
          : null,
      trangThai: json['trangThai']?.toString(),
    );
  }

  factory AttendanceResult.fromErrorJson(
    int statusCode,
    Map<String, dynamic>? json,
  ) {
    final rawCode = json?['code']?.toString() ?? '';
    final message = json?['message']?.toString() ?? _defaultMessage(rawCode, statusCode);

    AttendanceResultCode code;
    switch (rawCode) {
      case 'AlreadyCheckedIn':
        code = AttendanceResultCode.alreadyCheckedIn;
      case 'NoFaceDetected':
        code = AttendanceResultCode.noFaceDetected;
      case 'MultipleFaces':
        code = AttendanceResultCode.multipleFaces;
      case 'SpoofDetected':
        code = AttendanceResultCode.spoofDetected;
      case 'UnknownFace':
        code = AttendanceResultCode.unknownFace;
      case 'FaceMismatch':
        code = AttendanceResultCode.faceMismatch;
      case 'NotEnrolled':
        code = AttendanceResultCode.notEnrolled;
      case 'SessionNotFound':
        code = AttendanceResultCode.sessionNotFound;
      case 'AiServiceUnavailable':
        code = AttendanceResultCode.aiServiceUnavailable;
      case 'MissingImage':
      case 'MissingSession':
        code = AttendanceResultCode.unknownError;
      default:
        code = statusCode == 409
            ? AttendanceResultCode.alreadyCheckedIn
            : AttendanceResultCode.unknownError;
    }

    return AttendanceResult(
      code: code,
      thanhCong: false,
      thongBao: message,
      livenessScore: (json?['livenessScore'] as num?)?.toDouble(),
      detectedName: json?['detectedName']?.toString(),
      studentName: json?['studentName']?.toString(),
      existingCheckInUtc: json?['thoiGianCheckInUtc'] != null
          ? DateTime.tryParse(json!['thoiGianCheckInUtc'].toString())
          : null,
    );
  }

  factory AttendanceResult.networkError(String detail) => AttendanceResult(
        code: AttendanceResultCode.networkError,
        thanhCong: false,
        thongBao: 'Không thể kết nối máy chủ. $detail',
      );

  static String _defaultMessage(String code, int statusCode) {
    switch (statusCode) {
      case 401:
        return 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.';
      case 403:
        return 'Bạn không có quyền thực hiện thao tác này.';
      case 404:
        return 'Không tìm thấy buổi học hoặc tài khoản sinh viên.';
      case 409:
        return 'Bạn đã điểm danh trong buổi học này rồi.';
      case 503:
        return 'Dịch vụ AI nhận diện khuôn mặt hiện không khả dụng.';
      default:
        return 'Có lỗi xảy ra (mã $statusCode). Vui lòng thử lại.';
    }
  }
}
