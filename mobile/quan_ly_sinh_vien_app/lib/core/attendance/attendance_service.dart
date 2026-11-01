import 'attendance_models.dart';
import 'attendance_workflow_api.dart';

class AttendanceService {
  static final AttendanceService _instance = AttendanceService._internal();
  factory AttendanceService() => _instance;
  AttendanceService._internal();

  Future<List<TodaySessionInfo>> getSessionsToday() async {
    final api = AttendanceWorkflowApi();
    try {
      final rows = await api.request('buoi-hoc-cua-toi') as List;
      return rows
          .map(
            (x) =>
                TodaySessionInfo.fromJson(Map<String, dynamic>.from(x as Map)),
          )
          .toList();
    } finally {
      api.close();
    }
  }

  Future<AttendanceResult> confirmQr(
    Map<String, dynamic> challenge,
    List<String> frames,
  ) async {
    final api = AttendanceWorkflowApi();
    try {
      return AttendanceResult.fromSuccessJson(
        await api.submit(challenge, frames),
      );
    } on AttendanceWorkflowException catch (e) {
      return AttendanceResult.fromErrorJson(422, {
        'code': e.code,
        'message': e.message,
      });
    } finally {
      api.close();
    }
  }
}

class AttendanceServiceException implements Exception {
  const AttendanceServiceException(this.message);

  final String message;

  @override
  String toString() => message;
}
