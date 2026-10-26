import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../../data/local/secure_session_storage.dart';
import '../../data/repositories/auth_repository.dart';
import '../../data/models/auth_session.dart';
import '../config/api_config.dart';
import '../constants/role_names.dart';
import '../network/api_client.dart';

class AuthProvider {
  AuthProvider._internal();
  static final AuthProvider instance = AuthProvider._internal();

  AuthRepository? _repository;
  AuthSession? _session;

  Future<void> initialize() async {
    if (_repository == null) {
      final apiClient = ApiClient(
        client: http.Client(),
        baseUri: ApiConfig.baseUri,
      );
      _repository = AuthRepository(
        apiClient: apiClient,
        sessionStorage: SecureSessionStorage(const FlutterSecureStorage()),
      );
    }
    _session = await _repository!.restoreSession();
  }

  AuthSession? get session => _session;

  void setSession(AuthSession? session) {
    _session = session;
  }
  String? get accessToken => _session?.accessToken;
  bool get isAuthenticated => _session != null;
  String get userName => _session?.userName ?? '';
  List<String> get roles => _session?.roles ?? const [];
  bool get mustChangePassword => _session?.mustChangePassword ?? false;

  bool get isStudent => roles.contains(RoleNames.sinhVien);
  bool get isLecturerOrAdmin =>
      roles.contains(RoleNames.giangVien) ||
      roles.contains(RoleNames.admin) ||
      roles.contains(RoleNames.canBoQuanLy);

  Future<bool> login({
    required String accountCode,
    required String password,
  }) async {
    if (_repository == null) return false;
    try {
      _session = await _repository!.login(
        userNameOrEmail: accountCode,
        password: password,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    if (_session == null || _repository == null) return false;
    try {
      await _repository!.changePassword(
        session: _session!,
        currentPassword: currentPassword,
        newPassword: newPassword,
      );
      _session = null;
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    if (_session != null && _repository != null) {
      await _repository!.logout(_session!);
    }
    _session = null;
  }

  void setRepository(AuthRepository repository) {
    _repository = repository;
  }
}
