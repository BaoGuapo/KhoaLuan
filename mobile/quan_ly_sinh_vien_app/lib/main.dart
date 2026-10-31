import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import 'app.dart';
import 'core/attendance/attendance_workflow_api.dart';
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
  AttendanceWorkflowApi.accessTokenProvider = () async => authController.session?.accessToken;

  runApp(QuanLySinhVienApp(authController: authController));
  unawaited(authController.initialize());
}
