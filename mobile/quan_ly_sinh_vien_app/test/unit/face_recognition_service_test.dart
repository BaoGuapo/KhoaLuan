import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/face/face_recognition_result.dart';
import 'package:quan_ly_sinh_vien_app/core/face/face_recognition_service.dart';

// Tạo vector mẫu 128 chiều cho test
List<double> _makeVector(double value) =>
    List.filled(128, value).toList()..replaceRange(0, 1, [value + 0.01]);

const _modelV1 = 'dlib-resnet-128-yolov11-face-v1';

FaceEmbedding _embedding(String studentId, List<double> values) =>
    FaceEmbedding(
      studentId: studentId,
      studentName: 'SV $studentId',
      values: values,
      modelVersion: _modelV1,
    );

void main() {
  group('FaceRecognitionService.cosineSimilarity', () {
    test('hai vector giống nhau → similarity = 1.0', () {
      final v = [1.0, 0.0];
      expect(FaceRecognitionService.cosineSimilarity(v, v), closeTo(1.0, 1e-9));
    });

    test('hai vector vuông góc → similarity = 0.0', () {
      expect(FaceRecognitionService.cosineSimilarity([1.0, 0.0], [0.0, 1.0]),
          closeTo(0.0, 1e-9));
    });

    test('hai vector ngược chiều → similarity = -1.0', () {
      expect(FaceRecognitionService.cosineSimilarity([1.0, 0.0], [-1.0, 0.0]),
          closeTo(-1.0, 1e-9));
    });

    test('ném ArgumentError nếu độ dài khác nhau', () {
      expect(() => FaceRecognitionService.cosineSimilarity([1.0], [1.0, 2.0]),
          throwsArgumentError);
    });

    test('ném ArgumentError nếu vector rỗng', () {
      expect(() => FaceRecognitionService.cosineSimilarity([], []),
          throwsArgumentError);
    });

    test('ném ArgumentError nếu vector zero-norm', () {
      expect(() => FaceRecognitionService.cosineSimilarity([0.0], [1.0]),
          throwsArgumentError);
    });

    test('ném ArgumentError nếu giá trị là NaN', () {
      expect(() => FaceRecognitionService.cosineSimilarity([double.nan], [1.0]),
          throwsArgumentError);
    });

    test('ném ArgumentError nếu giá trị là Infinity', () {
      expect(
          () =>
              FaceRecognitionService.cosineSimilarity([double.infinity], [1.0]),
          throwsArgumentError);
    });
  });

  group('FaceRecognitionService constructor', () {
    test('threshold hợp lệ không ném lỗi', () {
      expect(() => FaceRecognitionService(threshold: 0.75), returnsNormally);
      expect(() => FaceRecognitionService(threshold: -1.0), returnsNormally);
      expect(() => FaceRecognitionService(threshold: 1.0), returnsNormally);
    });

    test('threshold ngoài [-1, 1] ném RangeError', () {
      expect(() => FaceRecognitionService(threshold: 1.1), throwsRangeError);
      expect(() => FaceRecognitionService(threshold: -1.1), throwsRangeError);
    });
  });

  group('FaceRecognitionService.match', () {
    final svc = FaceRecognitionService(threshold: 0.75);

    test('khớp khuôn mặt khi similarity >= threshold', () {
      final query = _makeVector(0.5);
      final enrolled = [_embedding('SV001', _makeVector(0.5))];

      final result = svc.match(
        query: query,
        modelVersion: _modelV1,
        enrolledFaces: enrolled,
      );

      expect(result.matched, isTrue);
      expect(result.studentId, 'SV001');
      expect(result.similarity, greaterThanOrEqualTo(0.75));
    });

    test('không khớp nếu danh sách enrollment rỗng', () {
      final result = svc.match(
        query: _makeVector(0.5),
        modelVersion: _modelV1,
        enrolledFaces: [],
      );
      expect(result.matched, isFalse);
      expect(result.studentId, isNull);
    });

    test('bỏ qua embedding có model version khác', () {
      final query = _makeVector(0.5);
      final enrolled = [
        FaceEmbedding(
          studentId: 'SV002',
          studentName: 'SV SV002',
          values: _makeVector(0.5),
          modelVersion: 'old-model-v0',
        ),
      ];

      final result = svc.match(
        query: query,
        modelVersion: _modelV1,
        enrolledFaces: enrolled,
      );
      expect(result.matched, isFalse);
    });

    test('bỏ qua embedding có số chiều khác', () {
      final enrolled = [
        FaceEmbedding(
          studentId: 'SV003',
          studentName: 'SV SV003',
          values: [0.5, 0.5], // chỉ 2 chiều, query 128 chiều
          modelVersion: _modelV1,
        ),
      ];
      final result = svc.match(
        query: _makeVector(0.5),
        modelVersion: _modelV1,
        enrolledFaces: enrolled,
      );
      expect(result.matched, isFalse);
    });

    test('chọn khuôn mặt có similarity cao nhất trong danh sách', () {
      final query = _makeVector(0.9);
      final enrolled = [
        _embedding('SV010', _makeVector(0.1)), // thấp hơn
        _embedding('SV011', _makeVector(0.9)), // cao nhất, gần query nhất
        _embedding('SV012', _makeVector(0.5)),
      ];

      final result = svc.match(
        query: query,
        modelVersion: _modelV1,
        enrolledFaces: enrolled,
      );
      expect(result.matched, isTrue);
      expect(result.studentId, 'SV011');
    });

    test('ném ArgumentError với query không hợp lệ (rỗng)', () {
      expect(
        () => svc.match(
          query: [],
          modelVersion: _modelV1,
          enrolledFaces: [],
        ),
        throwsArgumentError,
      );
    });

    test('ném ArgumentError với query chứa giá trị NaN', () {
      expect(
        () => svc.match(
          query: [double.nan, 1.0],
          modelVersion: _modelV1,
          enrolledFaces: [],
        ),
        throwsArgumentError,
      );
    });
  });

  group('FaceRecognitionResult', () {
    test('toJson chứa đúng các trường', () {
      const result = FaceRecognitionResult(
        studentId: 'SV001',
        name: 'Nguyễn Văn An',
        similarity: 0.92,
        matched: true,
      );
      final json = result.toJson();

      expect(json['student_id'], 'SV001');
      expect(json['name'], 'Nguyễn Văn An');
      expect(json['similarity'], 0.92);
      expect(json['matched'], isTrue);
    });

    test('toJson với matched = false', () {
      const result = FaceRecognitionResult(
        studentId: null,
        name: null,
        similarity: 0.3,
        matched: false,
      );
      final json = result.toJson();
      expect(json['matched'], isFalse);
      expect(json['student_id'], isNull);
    });
  });
}
