import 'package:flutter/foundation.dart';

import '../../core/network/api_exception.dart';
import '../../data/models/auth_session.dart';
import '../../data/repositories/auth_repository.dart';

enum AuthStatus { initializing, unauthenticated, authenticated }

class AuthController extends ChangeNotifier {
  AuthController(this._repository);

  final AuthRepository _repository;
  AuthStatus _status = AuthStatus.initializing;
  AuthSession? _session;
  bool _isBusy = false;
  String? _errorMessage;
  String? _noticeMessage;

  AuthStatus get status => _status;
  AuthSession? get session => _session;
  bool get isBusy => _isBusy;
  String? get errorMessage => _errorMessage;
  String? get noticeMessage => _noticeMessage;
  bool get mustChangePassword => _session?.mustChangePassword ?? false;

  Future<void> initialize() async {
    try {
      _session = await _repository.restoreSession();
      _status = _session == null
          ? AuthStatus.unauthenticated
          : AuthStatus.authenticated;
    } on Object {
      _session = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = 'Không thể khôi phục phiên đăng nhập.';
    }
    notifyListeners();
  }

  Future<bool> login({
    required String accountCode,
    required String password,
  }) async {
    _setBusy(true);
    try {
      _session = await _repository.login(
        userNameOrEmail: accountCode,
        password: password,
      );
      _status = AuthStatus.authenticated;
      _errorMessage = null;
      _noticeMessage = _session!.mustChangePassword
          ? 'Đây là lần đăng nhập đầu tiên. Bạn phải đổi mật khẩu tạm.'
          : null;
      return true;
    } on ApiException catch (error) {
      _errorMessage = error.message;
      return false;
    } on Object {
      _errorMessage = 'Đã xảy ra lỗi khi đăng nhập.';
      return false;
    } finally {
      _setBusy(false);
    }
  }

  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final currentSession = _session;
    if (currentSession == null) return false;

    _setBusy(true);
    try {
      await _repository.changePassword(
        session: currentSession,
        currentPassword: currentPassword,
        newPassword: newPassword,
      );
      _session = null;
      _status = AuthStatus.unauthenticated;
      _errorMessage = null;
      _noticeMessage = 'Đổi mật khẩu thành công. Hãy đăng nhập bằng mật khẩu mới.';
      return true;
    } on ApiException catch (error) {
      _errorMessage = error.message;
      return false;
    } on Object {
      _errorMessage = 'Đã xảy ra lỗi khi đổi mật khẩu.';
      return false;
    } finally {
      _setBusy(false);
    }
  }

  Future<void> logout() async {
    final currentSession = _session;
    _setBusy(true);
    if (currentSession != null) {
      await _repository.logout(currentSession);
    }
    _session = null;
    _status = AuthStatus.unauthenticated;
    _noticeMessage = 'Bạn đã đăng xuất.';
    _setBusy(false);
  }

  void clearMessages() {
    _errorMessage = null;
    _noticeMessage = null;
  }

  void _setBusy(bool value) {
    _isBusy = value;
    notifyListeners();
  }
}
