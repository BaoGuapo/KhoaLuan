import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as image;
import 'package:quan_ly_sinh_vien_app/core/face/camera_frame_codec.dart';

void main() {
  Uint8List fixture({int width = 320, int height = 240}) {
    final frame = image.Image(width: width, height: height);
    for (var y = 0; y < height; y++) {
      for (var x = 0; x < width; x++) {
        frame.setPixelRgb(
          x,
          y,
          x < width ~/ 2 ? 255 : 0,
          0,
          x < width ~/ 2 ? 0 : 255,
        );
      }
    }
    return Uint8List.fromList(image.encodePng(frame));
  }

  test('PNG camera data is encoded as a bounded JPEG accepted by backend', () {
    final result = encodeFaceCameraFrame(fixture(width: 1920, height: 1080));
    expect(result.take(2), [255, 216]);
    final decoded = image.decodeJpg(result)!;
    expect(decoded.width, 960);
    expect(decoded.height, 540);
    expect(result.length, lessThan(1050000));
  });
  test('browser selfie mirroring is reversed before pose verification', () {
    final raw = fixture();
    final native = image.decodeJpg(encodeFaceCameraFrame(raw))!;
    final browser = image.decodeJpg(
      encodeFaceCameraFrame(raw, mirrored: true),
    )!;
    expect(native.getPixel(20, 100).r, greaterThan(220));
    expect(native.getPixel(20, 100).b, lessThan(30));
    expect(browser.getPixel(20, 100).b, greaterThan(220));
    expect(browser.getPixel(20, 100).r, lessThan(30));
  });
  test('unreadable and empty camera frames are rejected before upload', () {
    expect(
      () => encodeFaceCameraFrame(Uint8List.fromList([1, 2, 3])),
      throwsFormatException,
    );
    final tiny = Uint8List.fromList(
      image.encodePng(image.Image(width: 10, height: 10)),
    );
    expect(() => encodeFaceCameraFrame(tiny), throwsFormatException);
  });
}
