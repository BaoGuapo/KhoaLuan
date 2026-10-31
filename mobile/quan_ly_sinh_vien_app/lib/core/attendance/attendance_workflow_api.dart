import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../auth/token_storage.dart';
import '../config/api_config.dart';
import '../face/live_pose_guidance.dart';

class AttendanceWorkflowException implements Exception {
  const AttendanceWorkflowException(this.message, {this.code});
  final String message;
  final String? code;
  @override
  String toString() => message;
}

class AttendanceWorkflowApi {
  AttendanceWorkflowApi({
    http.Client? client,
    Future<String?> Function()? tokenProvider,
    Uri? baseUri,
  }) : _client = client ?? http.Client(),
       _tokenProvider = tokenProvider ?? accessTokenProvider,
       _baseUri = baseUri ?? ApiConfig.baseUri;

  static Future<String?> Function() accessTokenProvider =
      TokenStorage.getAccessToken;
  final http.Client _client;
  final Future<String?> Function() _tokenProvider;
  final Uri _baseUri;

  Future<dynamic> request(
    String path, {
    Map<String, Object?>? body,
    Duration timeout = const Duration(seconds: 90),
  }) async {
    final token = await _tokenProvider();
    if (token == null || token.isEmpty) {
      throw const AttendanceWorkflowException(
        'Cần đăng nhập trước khi điểm danh.',
        code: 'Unauthorized',
      );
    }
    final uri = _baseUri.resolve('api/v1/qr-diem-danh/$path');
    final headers = {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    };
    try {
      final response =
          await (body == null
                  ? _client.get(uri, headers: headers)
                  : _client.post(uri, headers: headers, body: jsonEncode(body)))
              .timeout(timeout);
      dynamic value;
      try {
        value = response.bodyBytes.isEmpty
            ? <String, dynamic>{}
            : jsonDecode(utf8.decode(response.bodyBytes));
      } on FormatException {
        throw const AttendanceWorkflowException(
          'Máy chủ trả về dữ liệu không hợp lệ.',
        );
      }
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AttendanceWorkflowException(
          value is Map
              ? (value['message']?.toString() ??
                    'Yêu cầu thất bại (${response.statusCode}).')
              : 'Yêu cầu thất bại (${response.statusCode}).',
          code: value is Map ? value['code']?.toString() : null,
        );
      }
      return value;
    } on TimeoutException {
      throw const AttendanceWorkflowException(
        'Xác minh quá thời gian. Kiểm tra kết quả trước khi thử lại.',
      );
    } on http.ClientException {
      throw const AttendanceWorkflowException(
        'Không thể kết nối máy chủ. Hãy kiểm tra mạng.',
      );
    }
  }

  Future<Map<String, dynamic>> scan(String qrToken) async =>
      Map<String, dynamic>.from(
        await request('quet', body: {'qrToken': qrToken}) as Map,
      );
  Future<Map<String, dynamic>> challenge() async => Map<String, dynamic>.from(
    await request('ho-so/thu-thach', body: {}) as Map,
  );
  Future<LivePoseObservation> inspectPose(
    String challengeId,
    List<int> frame,
  ) async => LivePoseObservation.fromJson(
    Map<String, dynamic>.from(
      await request(
            'huong-dan',
            timeout: const Duration(seconds: 8),
            body: {'challengeId': challengeId, 'frame': base64Encode(frame)},
          )
          as Map,
    ),
  );
  Future<Map<String, dynamic>> submit(
    Map<String, dynamic> challenge,
    List<String> frames, {
    bool enrollment = false,
  }) async => Map<String, dynamic>.from(
    await request(
          enrollment ? 'ho-so' : 'xac-nhan',
          body: {
            'challengeId': challenge['challengeId'],
            'frames': frames,
            'consent': true,
            if (enrollment) 'storeReviewPhotosConsent': true,
          },
        )
        as Map,
  );
  void close() => _client.close();
}
