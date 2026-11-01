import 'package:geolocator/geolocator.dart';

/// Lấy vị trí thiết bị để gửi kèm lượt điểm danh.
///
/// Vị trí chỉ là thông tin bổ sung: backend ghi lại và cảnh báo cho giảng viên
/// chứ không chặn. Nên mọi trục trặc ở đây đều trả về null và điểm danh vẫn
/// chạy tiếp, không hiện lỗi cho sinh viên.
class ViTriService {
  const ViTriService();

  static const Duration _hetGio = Duration(seconds: 8);

  /// Trả về map đúng dạng `ViTriRequest` của API, hoặc null khi không lấy được.
  Future<Map<String, Object?>?> layViTri() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) return null;

      var quyen = await Geolocator.checkPermission();
      if (quyen == LocationPermission.denied) {
        quyen = await Geolocator.requestPermission();
      }
      if (quyen == LocationPermission.denied ||
          quyen == LocationPermission.deniedForever) {
        return null;
      }

      final viTri = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: _hetGio,
        ),
      );
      return {
        'viDo': viTri.latitude,
        'kinhDo': viTri.longitude,
        'doChinhXacMet': viTri.accuracy,
      };
    } catch (_) {
      // Hết giờ, thiếu quyền ở nền tảng, hoặc thiết bị không có GPS.
      return null;
    }
  }
}
