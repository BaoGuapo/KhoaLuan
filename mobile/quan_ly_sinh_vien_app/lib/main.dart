import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import 'app.dart';
import 'core/auth/auth_provider.dart';
import 'core/config/api_config.dart';
import 'core/network/api_client.dart';
import 'data/local/secure_session_storage.dart';
import 'data/repositories/auth_repository.dart';
import 'presentation/auth/auth_controller.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final apiClient = ApiClient(
    client: http.Client(),
    baseUri: ApiConfig.baseUri,
  );
  final sessionStorage = SecureSessionStorage(
    const FlutterSecureStorage(),
  );
  final authRepository = AuthRepository(
    apiClient: apiClient,
    sessionStorage: sessionStorage,
  );
  final authController = AuthController(authRepository);

  AuthProvider.instance.setRepository(authRepository);
  unawaited(AuthProvider.instance.initialize());

  runApp(QuanLySinhVienApp(authController: authController));
  unawaited(authController.initialize());
}
