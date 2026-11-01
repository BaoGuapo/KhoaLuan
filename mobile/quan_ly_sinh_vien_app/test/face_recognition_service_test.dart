import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/face/face_recognition_service.dart';

void main() {
  group('FaceRecognitionService.cosineSimilarity', () {
    test('returns cosine similarity for finite non-zero vectors', () {
      expect(
        FaceRecognitionService.cosineSimilarity([1, 0], [1, 0]),
        1,
      );
      expect(
        FaceRecognitionService.cosineSimilarity([1, 0], [0, 1]),
        0,
      );
      expect(
        FaceRecognitionService.cosineSimilarity([1, 0], [-1, 0]),
        -1,
      );
    });

    test('rejects vectors that cannot be compared', () {
      expect(
        () => FaceRecognitionService.cosineSimilarity([1], [1, 2]),
        throwsArgumentError,
      );
      expect(
        () => FaceRecognitionService.cosineSimilarity([0], [1]),
        throwsArgumentError,
      );
    });
  });
}
