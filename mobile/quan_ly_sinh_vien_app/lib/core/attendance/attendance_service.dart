import 'attendance_models.dart';

class AttendanceService {
  static final AttendanceService _instance = AttendanceService._internal();
  factory AttendanceService() => _instance;
  AttendanceService._internal();

  Future<List<TodaySessionInfo>> getSessionsToday() async {
    throw const AttendanceServiceException(
      'Backend GitHub chưa có API liệt kê buổi học hôm nay cho sinh viên.',
    );
  }

  Future<AttendanceResult> checkInWithFace({
    required String maBuoiHoc,
    required String imageBase64,
  }) async =>
      AttendanceResult.networkError(
        'Backend GitHub chưa hỗ trợ sinh viên tự điểm danh bằng ảnh khuôn mặt. '
        'Ảnh chưa được gửi lên máy chủ.',
      );
}

class AttendanceServiceException implements Exception {
  const AttendanceServiceException(this.message);

  final String message;

  @override
  String toString() => message;
}
