import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/face/live_pose_guidance.dart';

void main() {
  LivePoseObservation pose(double value, {double turn = .12}) =>
      LivePoseObservation(
        usable: true,
        message: 'OK',
        pose: value,
        minimumTurn: turn,
      );
  test('neutral requires front-facing usable finite landmarks', () {
    expect(checkLivePose(pose(0), 0, 'left', null).correct, isTrue);
    expect(checkLivePose(pose(.25), 0, 'left', null).correct, isFalse);
    expect(checkLivePose(pose(double.nan), 0, 'left', null).correct, isFalse);
    expect(
      checkLivePose(
        const LivePoseObservation(usable: false, message: 'No face'),
        0,
        'left',
        null,
      ).correct,
      isFalse,
    );
  });
  test('left/right sign and configured threshold match backend motion', () {
    expect(checkLivePose(pose(.18), 1, 'left', .01).correct, isTrue);
    expect(checkLivePose(pose(-.18), 1, 'right', .01).correct, isTrue);
    expect(
      checkLivePose(pose(.18), 1, 'right', .01).message,
      contains('ngược hướng'),
    );
    expect(checkLivePose(pose(.10), 1, 'left', .01).correct, isFalse);
    expect(
      checkLivePose(pose(.18, turn: .20), 1, 'left', .01).correct,
      isFalse,
    );
  });
  test('return must be close to original pose', () {
    expect(checkLivePose(pose(.02), 2, 'left', .01).correct, isTrue);
    expect(checkLivePose(pose(.15), 2, 'left', .01).correct, isFalse);
    expect(checkLivePose(pose(0), 2, 'left', null).correct, isFalse);
  });
}
