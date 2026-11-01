import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/auth_session.dart';

class SecureSessionStorage {
  SecureSessionStorage(this._storage);

  static const _sessionKey = 'auth_session_v1';
  final FlutterSecureStorage _storage;

  Future<void> save(AuthSession session) => _storage.write(
        key: _sessionKey,
        value: jsonEncode(session.toStorageJson()),
      );

  Future<AuthSession?> read() async {
    final raw = await _storage.read(key: _sessionKey);
    if (raw == null || raw.isEmpty) return null;

    try {
      final json = jsonDecode(raw);
      if (json is! Map<String, dynamic>) return null;
      return AuthSession.fromJson(json);
    } on Object {
      await clear();
      return null;
    }
  }

  Future<void> clear() => _storage.delete(key: _sessionKey);
}
