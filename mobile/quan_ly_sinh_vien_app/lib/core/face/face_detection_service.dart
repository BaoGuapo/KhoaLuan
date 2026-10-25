import 'dart:io';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:camera/camera.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

enum FaceDetectionStatus {
  searching,
  detected,
  noFace,
  multipleFaces,
  tooSmall,
  tilted,
  obscured,
}

class FaceDetectionResult {
  const FaceDetectionResult({
    required this.status,
    required this.faceCount,
    this.face,
  });

  final FaceDetectionStatus status;
  final int faceCount;
  final Face? face;

  bool get hasSingleUsableFace => status == FaceDetectionStatus.detected;

  String get message {
    switch (status) {
      case FaceDetectionStatus.searching:
        return 'Đang tìm khuôn mặt...';
      case FaceDetectionStatus.detected:
        return 'Đã phát hiện khuôn mặt';
      case FaceDetectionStatus.noFace:
        return 'Không tìm thấy khuôn mặt';
      case FaceDetectionStatus.multipleFaces:
        return 'Vui lòng chỉ để một khuôn mặt trong khung hình';
      case FaceDetectionStatus.tooSmall:
        return 'Khuôn mặt quá nhỏ, vui lòng đưa mặt lại gần';
      case FaceDetectionStatus.tilted:
        return 'Vui lòng nhìn thẳng và giữ đầu cân bằng';
      case FaceDetectionStatus.obscured:
        return 'Khuôn mặt bị che hoặc chưa đủ rõ, vui lòng điều chỉnh';
    }
  }
}

class FaceDetectionService {
  FaceDetectionService()
      : _detector = FaceDetector(
          options: FaceDetectorOptions(
            enableLandmarks: true,
            performanceMode: FaceDetectorMode.fast,
          ),
        );

  static const double _minimumFaceHeightRatio = 0.20;
  static const double _maximumYawDegrees = 20;
  static const double _maximumRollDegrees = 15;

  final FaceDetector _detector;

  Future<FaceDetectionResult> detectCameraImage(
    CameraImage image,
    CameraDescription camera,
  ) async {
    final bytes = _convertToNv21(image);
    final inputImage = InputImage.fromBytes(
      bytes: bytes,
      metadata: InputImageMetadata(
        size: ui.Size(image.width.toDouble(), image.height.toDouble()),
        rotation:
            InputImageRotationValue.fromRawValue(camera.sensorOrientation) ??
                InputImageRotation.rotation0deg,
        format: InputImageFormat.nv21,
        bytesPerRow: image.width,
      ),
    );
    return _detect(inputImage, image.height);
  }

  Future<FaceDetectionResult> detectFile(String path) async {
    final bytes = await File(path).readAsBytes();
    final codec = await ui.instantiateImageCodec(bytes);
    final decoded = await codec.getNextFrame();
    try {
      final image = InputImage.fromFilePath(path);
      return _detect(image, decoded.image.height);
    } finally {
      decoded.image.dispose();
      codec.dispose();
    }
  }

  Future<FaceDetectionResult> _detect(
    InputImage image,
    int imageHeight,
  ) async {
    final faces = await _detector.processImage(image);
    if (faces.isEmpty) {
      return const FaceDetectionResult(
        status: FaceDetectionStatus.noFace,
        faceCount: 0,
      );
    }
    if (faces.length != 1) {
      return FaceDetectionResult(
        status: FaceDetectionStatus.multipleFaces,
        faceCount: faces.length,
      );
    }

    final face = faces.single;
    if (face.boundingBox.height / imageHeight < _minimumFaceHeightRatio) {
      return FaceDetectionResult(
        status: FaceDetectionStatus.tooSmall,
        faceCount: 1,
        face: face,
      );
    }
    if ((face.headEulerAngleY ?? 0).abs() > _maximumYawDegrees ||
        (face.headEulerAngleZ ?? 0).abs() > _maximumRollDegrees) {
      return FaceDetectionResult(
        status: FaceDetectionStatus.tilted,
        faceCount: 1,
        face: face,
      );
    }
    if (!face.landmarks.containsKey(FaceLandmarkType.leftEye) ||
        !face.landmarks.containsKey(FaceLandmarkType.rightEye)) {
      return FaceDetectionResult(
        status: FaceDetectionStatus.obscured,
        faceCount: 1,
        face: face,
      );
    }
    return FaceDetectionResult(
      status: FaceDetectionStatus.detected,
      faceCount: 1,
      face: face,
    );
  }

  Uint8List _convertToNv21(CameraImage image) {
    if (image.format.group == ImageFormatGroup.nv21 &&
        image.planes.length == 1) {
      return image.planes.first.bytes;
    }

    if (image.format.group != ImageFormatGroup.yuv420 ||
        image.planes.length != 3) {
      throw UnsupportedError(
        'Camera image format is not supported for on-device face detection.',
      );
    }

    final width = image.width;
    final height = image.height;
    final yPlane = image.planes[0];
    final uPlane = image.planes[1];
    final vPlane = image.planes[2];
    final chromaWidth = (width + 1) ~/ 2;
    final chromaHeight = (height + 1) ~/ 2;
    final output = Uint8List(width * height + 2 * chromaWidth * chromaHeight);

    for (var row = 0; row < height; row++) {
      final rowStart = row * yPlane.bytesPerRow;
      for (var column = 0; column < width; column++) {
        output[row * width + column] = yPlane.bytes[rowStart + column];
      }
    }

    var outputIndex = width * height;
    for (var row = 0; row < chromaHeight; row++) {
      for (var column = 0; column < chromaWidth; column++) {
        final uIndex =
            row * uPlane.bytesPerRow + column * uPlane.bytesPerPixel!;
        final vIndex =
            row * vPlane.bytesPerRow + column * vPlane.bytesPerPixel!;
        output[outputIndex++] = vPlane.bytes[vIndex];
        output[outputIndex++] = uPlane.bytes[uIndex];
      }
    }
    return output;
  }

  Future<void> close() => _detector.close();
}
