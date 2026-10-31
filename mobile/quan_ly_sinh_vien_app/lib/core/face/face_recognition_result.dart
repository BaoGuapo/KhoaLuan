class FaceEmbedding {
  const FaceEmbedding({
    required this.studentId,
    required this.studentName,
    required this.values,
    required this.modelVersion,
  });

  final String studentId;
  final String studentName;
  final List<double> values;
  final String modelVersion;
}

class FaceRecognitionResult {
  const FaceRecognitionResult({
    required this.studentId,
    required this.name,
    required this.similarity,
    required this.matched,
  });

  final String? studentId;
  final String? name;
  final double? similarity;
  final bool matched;

  Map<String, Object?> toJson() => {
        'student_id': studentId,
        'name': name,
        'similarity': similarity,
        'matched': matched,
      };
}
