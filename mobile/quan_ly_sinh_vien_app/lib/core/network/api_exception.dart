class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.statusCode,
    this.code,
  });

  final String message;
  final int? statusCode;
  final String? code;

  @override
  String toString() => message;
}

class InvalidCredentialsException extends ApiException {
  const InvalidCredentialsException()
      : super(
          message: 'Mã tài khoản hoặc mật khẩu không đúng.',
          statusCode: 401,
        );
}
