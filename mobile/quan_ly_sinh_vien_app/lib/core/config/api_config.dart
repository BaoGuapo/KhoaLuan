class ApiConfig {
  ApiConfig._();

  /// Android Emulator truy cập máy host qua 10.0.2.2.
  /// Khi chạy thiết bị thật hoặc production, truyền API_BASE_URL bằng --dart-define.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5081/',
  );

  static Uri get baseUri {
    final uri = Uri.parse(baseUrl);
    if (!uri.hasScheme || uri.host.isEmpty) {
      throw StateError('API_BASE_URL không hợp lệ: $baseUrl');
    }
    return uri.path.endsWith('/') ? uri : uri.replace(path: '${uri.path}/');
  }
}
