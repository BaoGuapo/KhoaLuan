import '../attendance/attendance_workflow_api.dart';

class FaceEnrollmentService {
  Future<String> enroll(
    List<String> imageSamples, {
    required Map<String, dynamic> challenge,
  }) async {
    final api = AttendanceWorkflowApi();
    try {
      final value = await api.submit(challenge, imageSamples, enrollment: true);
      return value['message']?.toString() ??
          'Đã đăng ký, chờ CBQL hoặc Admin duyệt.';
    } finally {
      api.close();
    }
  }
}

class FaceEnrollmentException implements Exception {
  const FaceEnrollmentException(this.message);

  final String message;

  @override
  String toString() => message;
}
