import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../api/api_constants.dart';
import '../notifications/fcm_notification_service.dart';
import 'token_storage.dart';
import 'user_model.dart';

class AuthService extends ChangeNotifier {
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  UserModel? _currentUser;
  bool _isLoading = false;
  bool _mustChangePassword = false;
  String? _errorMessage;

  UserModel? get currentUser => _currentUser;
  bool get isAuthenticated => _currentUser != null;
  bool get isLoading => _isLoading;
  bool get mustChangePassword => _mustChangePassword;
  String? get errorMessage => _errorMessage;

  bool get isStudent => _currentUser?.role == UserRole.sinhVien;
  bool get isLecturerOrAdmin =>
      _currentUser?.role == UserRole.giangVien ||
      _currentUser?.role == UserRole.admin ||
      _currentUser?.role == UserRole.canBoQuanLy;

  // Initialize and check saved session
  Future<void> initialize() async {
    final hasToken = await TokenStorage.hasValidToken();
    if (hasToken) {
      _currentUser = await TokenStorage.getUser();
      _mustChangePassword = await TokenStorage.mustChangePassword();
      notifyListeners();
    }
  }

  // Login handler
  Future<bool> login(String usernameOrEmail, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.login}');
      final response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({
              'userNameOrEmail': usernameOrEmail.trim(),
              'password': password,
            }),
          )
          .timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final accessToken = data['accessToken'] as String? ?? '';
        final refreshToken = data['refreshToken'] as String? ?? '';
        final expiresStr = data['accessTokenExpiresAtUtc'] as String?;
        final expiresAt = expiresStr != null
            ? DateTime.tryParse(expiresStr) ??
                DateTime.now().add(const Duration(hours: 24))
            : DateTime.now().add(const Duration(hours: 24));

        final rolesList = (data['roles'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            [];
        if (accessToken.isEmpty || rolesList.isEmpty) {
          _errorMessage = 'Máy chủ trả về thông tin đăng nhập không hợp lệ.';
          return false;
        }
        final role = _determineRole(rolesList);
        if (role == null) {
          _errorMessage = 'Tài khoản không có vai trò hợp lệ.';
          return false;
        }

        final user = UserModel(
          id: data['userName']?.toString() ?? usernameOrEmail,
          userName: data['userName']?.toString() ?? usernameOrEmail,
          fullName: data['fullName']?.toString() ??
              data['userName']?.toString() ??
              usernameOrEmail,
          email: data['email']?.toString() ?? '',
          code: data['userName']?.toString() ?? usernameOrEmail,
          role: role,
        );
        _mustChangePassword = data['mustChangePassword'] == true;

        await TokenStorage.saveTokens(
          accessToken: accessToken,
          refreshToken: refreshToken,
          expiresAt: expiresAt,
          mustChangePassword: _mustChangePassword,
        );
        await TokenStorage.saveUser(user);

        _currentUser = user;
        _isLoading = false;
        notifyListeners();
        await FcmNotificationService().syncForCurrentUser();
        return true;
      }
      final payload = _tryReadObject(response.body);
      _errorMessage = payload?['message']?.toString() ??
          (response.statusCode == 401
              ? 'Tài khoản hoặc mật khẩu không chính xác.'
              : 'Đăng nhập thất bại (mã lỗi ${response.statusCode}).');
    } catch (error) {
      _errorMessage = 'Không thể kết nối máy chủ đăng nhập: $error';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  // Change Password
  Future<bool> changePassword(
      String currentPassword, String newPassword) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final token = await TokenStorage.getAccessToken();
      if (token != null && token.isNotEmpty && !token.startsWith('mock_')) {
        final url =
            Uri.parse('${ApiConstants.baseUrl}${ApiConstants.changePassword}');
        final response = await http.post(
          url,
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
          body: jsonEncode({
            'currentPassword': currentPassword,
            'newPassword': newPassword,
          }),
        );
        if (response.statusCode == 204 || response.statusCode == 200) {
          _mustChangePassword = false;
          await TokenStorage.setMustChangePassword(false);
          _isLoading = false;
          notifyListeners();
          return true;
        }
        final payload = _tryReadObject(response.body);
        _errorMessage = payload?['message']?.toString() ??
            'Đổi mật khẩu thất bại (mã lỗi ${response.statusCode}).';
      } else {
        _errorMessage = 'Cần đăng nhập máy chủ để đổi mật khẩu.';
      }
    } catch (error) {
      _errorMessage = 'Không thể kết nối máy chủ đổi mật khẩu: $error';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  // Logout handler
  Future<void> logout() async {
    try {
      final token = await TokenStorage.getAccessToken();
      final refreshToken = await TokenStorage.getRefreshToken();
      if (token != null && refreshToken != null && !token.startsWith('mock_')) {
        final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.logout}');
        await http.post(
          url,
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
          body: jsonEncode({'refreshToken': refreshToken}),
        );
      }
    } catch (_) {}

    await FcmNotificationService().removeCurrentDevice();
    await TokenStorage.clear();
    _currentUser = null;
    _mustChangePassword = false;
    notifyListeners();
  }

  // Helper to determine role from backend response or username
  UserRole? _determineRole(List<String> roles) {
    for (final r in roles) {
      final lower = r.toLowerCase();
      if (lower.contains('admin')) return UserRole.admin;
      if (lower.contains('giangvien') || lower.contains('lecturer')) {
        return UserRole.giangVien;
      }
      if (lower.contains('canbo')) return UserRole.canBoQuanLy;
      if (lower.contains('sinhvien') || lower.contains('student')) {
        return UserRole.sinhVien;
      }
    }
    return null;
  }

  Map<String, dynamic>? _tryReadObject(String body) {
    try {
      final value = jsonDecode(body);
      return value is Map<String, dynamic> ? value : null;
    } on FormatException {
      return null;
    }
  }
}
