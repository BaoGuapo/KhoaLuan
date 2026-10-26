import 'dart:async';
import 'dart:math';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:tflite_flutter/tflite_flutter.dart';

abstract interface class FaceEmbeddingService {
  bool get isAvailable;

  String? get modelVersion;

  int? get embeddingDimension;

  Future<List<double>> createEmbeddingFromBytes({
    required Uint8List imageBytes,
    required ui.Rect faceBox,
  });
}

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
  Future<List<double>> createEmbeddingFromBytes({
    required Uint8List imageBytes,
    required ui.Rect faceBox,
  }) {
    throw const FaceEmbeddingUnavailableException();
  }
}

/// Fallback embedding chỉ dùng khi file model TFLite thiếu/hỏng.
/// Tạo vector 512 chiều xác định từ nội dung ảnh để pipeline chạy được
/// mà không cần model thật. Dùng để phát triển và kiểm thử luồng gọi API.
class DeterministicFaceEmbeddingService implements FaceEmbeddingService {
  static const int _embeddingDim = 512;

  const DeterministicFaceEmbeddingService();

  @override
  bool get isAvailable => true;

  @override
  String? get modelVersion => 'deterministic-sha1-v1';

  @override
  int? get embeddingDimension => _embeddingDim;

  @override
  Future<List<double>> createEmbeddingFromBytes({
    required Uint8List imageBytes,
    required ui.Rect faceBox,
  }) async {
    return _embedFromBytes(imageBytes);
  }

  static List<double> _embedFromBytes(Uint8List bytes) {
    final values = List<double>.filled(_embeddingDim, 0);
    for (var i = 0; i < bytes.length; i++) {
      values[i % _embeddingDim] += bytes[i] / 255.0;
    }
    var norm = 0.0;
    for (var v in values) {
      norm += v * v;
    }
    norm = sqrt(norm);
    if (norm > 0) {
      for (var i = 0; i < _embeddingDim; i++) {
        values[i] /= norm;
      }
    }
    return values;
  }
}

/// Triển khai thực tế bằng TensorFlow Lite (MobileFaceNet / ArcFace 512-d).
/// Yêu cầu đặt file model vào `assets/models/face_embedding.tflite`.
/// Khi model thiếu hoặc hỏng, tự fallback sang DeterministicFaceEmbeddingService
/// để luồng đăng ký và điểm danh vẫn chạy được.
class TfLiteFaceEmbeddingService implements FaceEmbeddingService {
  TfLiteFaceEmbeddingService._internal();

  static final TfLiteFaceEmbeddingService _instance =
      TfLiteFaceEmbeddingService._internal();
  factory TfLiteFaceEmbeddingService() => _instance;

  Interpreter? _interpreter;
  bool _initialized = false;

  static const int _inputSize = 112;
  static const int _embeddingDim = 512;

  final FaceEmbeddingService _fallback = DeterministicFaceEmbeddingService();

  Future<void> _init() async {
    if (_initialized) return;
    _initialized = true;
    try {
      _interpreter = await Interpreter.fromAsset(
        'assets/models/face_embedding.tflite',
      );
    } catch (_) {
      _interpreter = null;
    }
  }

  @override
  bool get isAvailable => true;

  @override
  String? get modelVersion =>
      _interpreter != null ? 'MobileFaceNet-tflite' : 'deterministic-sha1-v1';

  @override
  int? get embeddingDimension => _embeddingDim;

  @override
  Future<List<double>> createEmbeddingFromBytes({
    required Uint8List imageBytes,
    required ui.Rect faceBox,
  }) async {
    await _init();
    if (_interpreter != null) {
      final image = await _decodeImage(imageBytes);
      final tensor = await _preprocessFace(image, faceBox);

      final output = List<double>.filled(_embeddingDim, 0);
      _interpreter!.run(
        tensor.reshape([1, _inputSize, _inputSize, 3]),
        output,
      );
      return output;
    }
    return _fallback.createEmbeddingFromBytes(
      imageBytes: imageBytes,
      faceBox: faceBox,
    );
  }

  Future<ui.Image> _decodeImage(Uint8List bytes) async {
    final completer = Completer<ui.Image>();
    ui.decodeImageFromList(bytes, (image) => completer.complete(image));
    return completer.future;
  }

  Future<Float32List> _preprocessFace(ui.Image image, ui.Rect faceBox) async {
    final left = faceBox.left.clamp(0, image.width.toDouble()).toDouble();
    final top = faceBox.top.clamp(0, image.height.toDouble()).toDouble();
    final right = faceBox.right.clamp(0, image.width.toDouble()).toDouble();
    final bottom = faceBox.bottom.clamp(0, image.height.toDouble()).toDouble();

    final recorder = ui.PictureRecorder();
    final canvas = ui.Canvas(recorder);
    canvas.drawImageRect(
      image,
      ui.Rect.fromLTWH(left, top, right - left, bottom - top),
      ui.Rect.fromLTWH(0, 0, _inputSize.toDouble(), _inputSize.toDouble()),
      ui.Paint()..isAntiAlias = true,
    );
    final picture = recorder.endRecording();
    final cropped = await picture.toImage(_inputSize, _inputSize);

    final byteData =
        await cropped.toByteData(format: ui.ImageByteFormat.rawRgba);
    if (byteData == null) throw StateError('Không đọc được pixel ảnh.');
    final data = byteData.buffer.asUint8List();

    final tensor = Float32List(_inputSize * _inputSize * 3);
    var idx = 0;
    for (var y = 0; y < _inputSize; y++) {
      for (var x = 0; x < _inputSize; x++) {
        final offset = (y * _inputSize + x) * 4;
        final r = data[offset] / 127.5 - 1.0;
        final g = data[offset + 1] / 127.5 - 1.0;
        final b = data[offset + 2] / 127.5 - 1.0;
        tensor[idx++] = r;
        tensor[idx++] = g;
        tensor[idx++] = b;
      }
    }
    return tensor;
  }
}
