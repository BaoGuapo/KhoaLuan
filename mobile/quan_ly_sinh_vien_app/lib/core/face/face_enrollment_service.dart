class FaceEnrollmentService {
  Future<String> enroll(List<String> imageSamples) async =>
      throw const FaceEnrollmentException(
        'Backend GitHub yêu cầu face descriptors, nhưng ứng dụng chưa có model '
        'tạo descriptors. Ảnh khuôn mặt chưa được gửi lên máy chủ.',
      );
}

class FaceEnrollmentException implements Exception {
  const FaceEnrollmentException(this.message);

  final String message;

  @override
  String toString() => message;
}
