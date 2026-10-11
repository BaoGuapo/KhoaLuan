import 'dart:convert';

import '../../core/network/api_client.dart';
import '../../core/network/api_exception.dart';
import '../local/secure_session_storage.dart';
import '../models/auth_session.dart';

class AuthRepository {
  AuthRepository({
    required ApiClient apiClient,
    required SecureSessionStorage sessionStorage,
  })  : _apiClient = apiClient,
        _sessionStorage = sessionStorage;

  final ApiClient _apiClient;
  final SecureSessionStorage _sessionStorage;

  Future<AuthSession> login({
    required String userNameOrEmail,
    required String password,
  }) async {
    final response = await _apiClient.post(
      'api/auth/login',
      body: {
        'userNameOrEmail': userNameOrEmail.trim(),
        'password': password,
      },
    );

    if (response.statusCode == 401) {
      throw const InvalidCredentialsException();
    }
    if (response.statusCode != 200) {
      throw _apiClient.errorFrom(response);
    }

    final session = _parseSession(response.bodyBytes);
    await _sessionStorage.save(session);
    return session;
  }

  Future<AuthSession?> restoreSession() async {
    final stored = await _sessionStorage.read();
    if (stored == null) return null;
    if (!stored.isAccessTokenExpired) return stored;

    try {
      return await _refresh(stored.refreshToken);
    } on ApiException {
      await _sessionStorage.clear();
      return null;
    }
  }

  Future<void> changePassword({
    required AuthSession session,
    required String currentPassword,
    required String newPassword,
  }) async {
    var activeSession = session;
    if (activeSession.isAccessTokenExpired) {
      activeSession = await _refresh(activeSession.refreshToken);
    }

    final response = await _apiClient.post(
      'api/auth/change-password',
      accessToken: activeSession.accessToken,
      body: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
    if (response.statusCode != 204) {
      throw _apiClient.errorFrom(response);
    }

    // Backend thu hồi refresh token sau khi đổi mật khẩu.
    await _sessionStorage.clear();
  }

  Future<void> logout(AuthSession session) async {
    try {
      await _apiClient.post(
        'api/auth/logout',
        accessToken: session.accessToken,
        body: {'refreshToken': session.refreshToken},
      );
    } on ApiException {
      // Đăng xuất cục bộ vẫn phải hoàn tất khi mất mạng.
    } finally {
      await _sessionStorage.clear();
    }
  }

  Future<AuthSession> _refresh(String refreshToken) async {
    final response = await _apiClient.post(
      'api/auth/refresh-token',
      body: {'refreshToken': refreshToken},
    );
    if (response.statusCode != 200) {
      throw _apiClient.errorFrom(response);
    }

    final session = _parseSession(response.bodyBytes);
    await _sessionStorage.save(session);
    return session;
  }

  AuthSession _parseSession(List<int> bodyBytes) {
    final json = jsonDecode(utf8.decode(bodyBytes));
    if (json is! Map<String, dynamic>) {
      throw const ApiException(message: 'Response đăng nhập không hợp lệ.');
    }
    try {
      return AuthSession.fromJson(json);
    } on Object {
      throw const ApiException(message: 'Response đăng nhập thiếu dữ liệu.');
    }
  }
}
