import 'dart:math' as math;

import 'face_recognition_result.dart';

class FaceRecognitionConfig {
  const FaceRecognitionConfig._();

  // Provisional only; calibrate using genuine enrollment and validation data.
  static const double initialSimilarityThreshold = 0.75;
}

class FaceRecognitionService {
  FaceRecognitionService({
    this.threshold = FaceRecognitionConfig.initialSimilarityThreshold,
  }) {
    if (threshold < -1 || threshold > 1) {
      throw RangeError.range(threshold, -1, 1, 'threshold');
    }
  }

  final double threshold;

  FaceRecognitionResult match({
    required List<double> query,
    required String modelVersion,
    required List<FaceEmbedding> enrolledFaces,
  }) {
    if (query.isEmpty || query.any((value) => !value.isFinite)) {
      throw ArgumentError.value(query, 'query', 'Must be a finite vector.');
    }

    FaceEmbedding? bestCandidate;
    double? bestSimilarity;
    for (final candidate in enrolledFaces) {
      if (candidate.modelVersion != modelVersion ||
          candidate.values.length != query.length ||
          candidate.values.any((value) => !value.isFinite)) {
        continue;
      }
      final similarity = cosineSimilarity(query, candidate.values);
      if (bestSimilarity == null || similarity > bestSimilarity) {
        bestCandidate = candidate;
        bestSimilarity = similarity;
      }
    }

    final matched = bestSimilarity != null && bestSimilarity >= threshold;
    return FaceRecognitionResult(
      studentId: matched ? bestCandidate!.studentId : null,
      name: matched ? bestCandidate!.studentName : null,
      similarity: bestSimilarity,
      matched: matched,
    );
  }

  static double cosineSimilarity(List<double> left, List<double> right) {
    if (left.isEmpty || left.length != right.length) {
      throw ArgumentError('Vectors must have the same non-zero dimension.');
    }

    var dot = 0.0;
    var leftMagnitude = 0.0;
    var rightMagnitude = 0.0;
    for (var index = 0; index < left.length; index++) {
      final a = left[index];
      final b = right[index];
      if (!a.isFinite || !b.isFinite) {
        throw ArgumentError('Vector values must be finite.');
      }
      dot += a * b;
      leftMagnitude += a * a;
      rightMagnitude += b * b;
    }
    if (leftMagnitude == 0 || rightMagnitude == 0) {
      throw ArgumentError('Zero vectors cannot be compared.');
    }
    return dot / (math.sqrt(leftMagnitude) * math.sqrt(rightMagnitude));
  }
}
