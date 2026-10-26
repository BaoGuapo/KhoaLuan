import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:http/http.dart' as http;
import 'package:path_provider/path_provider.dart';

import '../api/api_constants.dart';
import '../auth/auth_provider.dart';
import 'face_detection_service.dart';
import 'face_embedding_service.dart';

class FaceEnrollmentService {
  FaceEnrollmentService({
    FaceEmbeddingService? embeddingService,
    FaceDetectionService? detectionService,
  }) : _embeddingService = embeddingService ??
            const MissingModelFaceEmbeddingService(),
       _detectionService = detectionService ?? FaceDetectionService();

  final FaceEmbeddingService _embeddingService;
  final FaceDetectionService _detectionService;

  Future<String> enroll(List<String> imageBase64Samples) async {
    if (!_embeddingService.isAvailable) {
      throw const FaceEnrollmentException(
        'Model Face Embedding chưa được tích hợp. '
        'Vui lòng đặt file model vào assets/models/face_embedding.tflite '
        'và rebuild ứng dụng.',
      );
    }

    final descriptors = <List<double>>[];
    for (final base64 in imageBase64Samples) {
      final bytes = base64Decode(base64);
      final faceBox = await _detectSingleFaceBox(bytes);
      final embedding = await _embeddingService.createEmbeddingFromBytes(
        imageBytes: bytes,
        faceBox: faceBox,
      );
      descriptors.add(embedding);
    }

    if (descriptors.length < 3 || descriptors.length > 5) {
      throw const FaceEnrollmentException(
        'Cần từ 3 đến 5 mẫu khuôn mặt liên tiếp.',
      );
    }

    final token = AuthProvider.instance.accessToken;
    if (token == null || token.isEmpty) {
      throw const FaceEnrollmentException('Chưa đăng nhập.');
    }

    final response = await http
        .post(
          Uri.parse(
            '${ApiConstants.baseUrl}${ApiConstants.registerFace}',
          ),
          headers: {
            'Authorization': 'Bearer $token',
            'Content-Type': 'application/json',
          },
          body: jsonEncode({'Descriptors': descriptors}),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode == 201 || response.statusCode == 204) {
      return 'Đăng ký FaceID thành công.';
    }
    if (response.statusCode == 409) {
      throw const FaceEnrollmentException(
        'Sinh viên đã có hồ sơ khuôn mặt.',
      );
    }
    throw FaceEnrollmentException(
      'Đăng ký FaceID thất bại (mã lỗi ${response.statusCode}).',
    );
  }

  Future<ui.Rect> _detectSingleFaceBox(Uint8List bytes) async {
    final path = await _saveTempFile(bytes);
    final result = await _detectionService.detectFile(path);
    try {
      await File(path).delete();
    } catch (_) {}

    if (result.face == null) {
      throw const FaceEnrollmentException(
        'Không phát hiện khuôn mặt trong ảnh.',
      );
    }
    if (result.faceCount != 1) {
      throw const FaceEnrollmentException(
        'Ảnh phải chứa đúng 1 khuôn mặt.',
      );
    }
    return result.face!.boundingBox;
  }

  Future<String> _saveTempFile(Uint8List bytes) async {
    final dir = await getTemporaryDirectory();
    final file = File(
      '${dir.path}/face_${DateTime.now().millisecondsSinceEpoch}.jpg',
    );
    await file.writeAsBytes(bytes);
    return file.path;
  }

}

class FaceEnrollmentException implements Exception {
  const FaceEnrollmentException(this.message);

  final String message;

  @override
  String toString() => message;
}
