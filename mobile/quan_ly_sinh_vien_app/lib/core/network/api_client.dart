import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import 'api_exception.dart';

class ApiClient {
  ApiClient({
    required http.Client client,
    required Uri baseUri,
  })  : _client = client,
        _baseUri = baseUri;

  final http.Client _client;
  final Uri _baseUri;

  Future<http.Response> post(
    String path, {
    required Map<String, Object?> body,
    String? accessToken,
  }) async {
    try {
      return await _client
          .post(
            _baseUri.resolve(path),
            headers: {
              HttpHeaders.contentTypeHeader: 'application/json',
              HttpHeaders.acceptHeader: 'application/json',
              if (accessToken != null)
                HttpHeaders.authorizationHeader: 'Bearer $accessToken',
            },
            body: jsonEncode(body),
          )
          .timeout(const Duration(seconds: 30));
    } on SocketException {
      throw const ApiException(
        message: 'Không thể kết nối đến backend API. Hãy kiểm tra địa chỉ API và mạng.',
      );
    } on TimeoutException {
      throw const ApiException(
        message: 'Backend API phản hồi quá thời gian. Vui lòng thử lại.',
      );
    } on HttpException {
      throw const ApiException(message: 'Backend API không phản hồi hợp lệ.');
    } on FormatException {
      throw const ApiException(message: 'Địa chỉ backend API không hợp lệ.');
    }
  }

  ApiException errorFrom(http.Response response) {
    String? message;
    String? code;
    try {
      final value = jsonDecode(utf8.decode(response.bodyBytes));
      if (value is Map<String, dynamic>) {
        message = value['message'] as String?;
        code = value['code'] as String?;
        message ??= value['title'] as String?;
      }
    } on FormatException {
      // Dùng thông báo mặc định nếu response không phải JSON.
    }

    return ApiException(
      statusCode: response.statusCode,
      code: code,
      message: message ?? 'Yêu cầu thất bại (${response.statusCode}).',
    );
  }
}
