import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

abstract interface class FaceEmbeddingService {
  bool get isAvailable;

  String? get modelVersion;

  int? get embeddingDimension;

  Future<List<double>> createEmbedding({
    required InputImage image,
    required Face face,
  });
}

// TODO: Implement this interface only after choosing a genuine face-embedding
// model. Its input size, tensor type, pixel normalization, alignment procedure,
// output dimension, and model version must come from that model's specification.
class FaceEmbeddingUnavailableException implements Exception {
  const FaceEmbeddingUnavailableException();

  @override
  String toString() =>
      'Chưa có model Face Embedding thật. Không thể tạo embedding.';
}

class MissingModelFaceEmbeddingService implements FaceEmbeddingService {
  const MissingModelFaceEmbeddingService();

  @override
  bool get isAvailable => false;

  @override
  int? get embeddingDimension => null;

  @override
  String? get modelVersion => null;

  @override
  Future<List<double>> createEmbedding({
    required InputImage image,
    required Face face,
  }) {
    throw const FaceEmbeddingUnavailableException();
  }
}
