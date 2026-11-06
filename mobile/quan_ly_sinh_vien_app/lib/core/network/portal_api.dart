import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import 'api_exception.dart';

/// Gọi các API đọc dữ liệu (báo cáo, lịch học...) bằng access token của phiên hiện tại.
class PortalApi {
  PortalApi({
    http.Client? client,
    Future<String?> Function()? tokenProvider,
    Uri? baseUri,
  })  : _client = client ?? http.Client(),
        _tokenProvider = tokenProvider ?? accessTokenProvider,
        _baseUri = baseUri ?? ApiConfig.baseUri;

  /// Gán trong main(); hot reload không chạy lại main() nên cần hot restart sau khi đổi chỗ này.
  static Future<String?> Function() accessTokenProvider = () async =>
      throw StateError('PortalApi.accessTokenProvider chưa được cấu hình trong main().');

  final http.Client _client;
  final Future<String?> Function() _tokenProvider;
  final Uri _baseUri;

  Future<dynamic> get(String path, {Map<String, String>? query}) async {
    final response = await _send(path, query: query, accept: 'application/json');
    return response.bodyBytes.isEmpty
        ? null
        : _tryDecode(utf8.decode(response.bodyBytes));
  }

  /// Tải tệp nhị phân (ví dụ ảnh thẻ); trả về null khi máy chủ báo không có (404).
  Future<List<int>?> getBytes(String path) async {
    try {
      return (await _send(path, accept: '*/*')).bodyBytes;
    } on ApiException catch (error) {
      if (error.statusCode == 404) return null;
      rethrow;
    }
  }

  Future<http.Response> _send(
    String path, {
    Map<String, String>? query,
    required String accept,
  }) async {
    final token = await _tokenProvider();
    if (token == null || token.isEmpty) {
      throw const ApiException(
        message: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        statusCode: 401,
      );
    }
    var uri = _baseUri.resolve(path);
    if (query != null && query.isNotEmpty) {
      uri = uri.replace(queryParameters: query);
    }

    final http.Response response;
    try {
      response = await _client.get(uri, headers: {
        HttpHeaders.acceptHeader: accept,
        HttpHeaders.authorizationHeader: 'Bearer $token',
      }).timeout(const Duration(seconds: 30));
    } on SocketException {
      throw const ApiException(
        message: 'Không thể kết nối đến máy chủ. Hãy kiểm tra mạng.',
      );
    } on TimeoutException {
      throw const ApiException(
        message: 'Máy chủ phản hồi quá thời gian. Vui lòng thử lại.',
      );
    } on http.ClientException {
      throw const ApiException(message: 'Không thể kết nối đến máy chủ.');
    }

    if (response.statusCode >= 200 && response.statusCode < 300) return response;

    final body = response.bodyBytes.isEmpty
        ? null
        : _tryDecode(utf8.decode(response.bodyBytes, allowMalformed: true));
    // Ưu tiên thông báo của server (ví dụ tài khoản bị khóa, quyền đã thay đổi).
    final serverMessage =
        body is Map ? (body['message'] ?? body['title'])?.toString() : null;
    final message = serverMessage ??
        switch (response.statusCode) {
          401 => 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
          403 => 'Tài khoản không có quyền xem dữ liệu này.',
          _ => null,
        };
    throw ApiException(
      statusCode: response.statusCode,
      message: message ?? 'Yêu cầu thất bại (${response.statusCode}).',
    );
  }

  Future<List<Map<String, dynamic>>> getList(
    String path, {
    Map<String, String>? query,
  }) async {
    final value = await get(path, query: query);
    if (value is! List) return const [];
    return value.whereType<Map>().map(Map<String, dynamic>.from).toList();
  }

  Future<Map<String, dynamic>> getObject(
    String path, {
    Map<String, String>? query,
  }) async {
    final value = await get(path, query: query);
    if (value is! Map) {
      throw const ApiException(message: 'Dữ liệu máy chủ trả về không hợp lệ.');
    }
    return Map<String, dynamic>.from(value);
  }

  void close() => _client.close();

  static Object? _tryDecode(String text) {
    try {
      return jsonDecode(text);
    } on FormatException {
      return null;
    }
  }
}
