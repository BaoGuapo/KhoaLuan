class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAtUtc,
    required this.userName,
    required this.roles,
    required this.mustChangePassword,
  });

  final String accessToken;
  final String refreshToken;
  final DateTime accessTokenExpiresAtUtc;
  final String userName;
  final List<String> roles;
  final bool mustChangePassword;

  bool get isAccessTokenExpired => accessTokenExpiresAtUtc.isBefore(
        DateTime.now().toUtc().add(const Duration(seconds: 30)),
      );

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    final roles = json['roles'];
    if (roles is! List) {
      throw const FormatException('Response đăng nhập thiếu roles.');
    }

    return AuthSession(
      accessToken: json['accessToken'] as String,
      refreshToken: json['refreshToken'] as String,
      accessTokenExpiresAtUtc: DateTime.parse(
        json['accessTokenExpiresAtUtc'] as String,
      ).toUtc(),
      userName: json['userName'] as String,
      roles: roles.map((role) => role.toString()).toList(growable: false),
      mustChangePassword: json['mustChangePassword'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toStorageJson() => {
        'accessToken': accessToken,
        'refreshToken': refreshToken,
        'accessTokenExpiresAtUtc': accessTokenExpiresAtUtc.toIso8601String(),
        'userName': userName,
        'roles': roles,
        'mustChangePassword': mustChangePassword,
      };
}
