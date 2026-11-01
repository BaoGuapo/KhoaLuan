import 'dart:typed_data';
import 'package:image/image.dart' as image;

/// Normalize the captured file, not the preview. camera_web mirrors selfies;
/// native camera JPEGs and the server's movement checks use unmirrored frames.
Uint8List encodeFaceCameraFrame(Uint8List bytes, {bool mirrored = false}) {
  final decoded = image.decodeImage(bytes);
  if (decoded == null || decoded.width < 160 || decoded.height < 160) {
    throw const FormatException(
      'Ảnh camera chưa sẵn sàng hoặc không đủ rõ. Hãy chụp lại.',
    );
  }
  var frame = image.bakeOrientation(decoded);
  if (mirrored) frame = image.flipHorizontal(frame);
  final longest = frame.width > frame.height ? frame.width : frame.height;
  if (longest > 960) {
    frame = image.copyResize(
      frame,
      width: (frame.width * 960 / longest).round(),
      height: (frame.height * 960 / longest).round(),
    );
  }
  return Uint8List.fromList(image.encodeJpg(frame, quality: 85));
}
