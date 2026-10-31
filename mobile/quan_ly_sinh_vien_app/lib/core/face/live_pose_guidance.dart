class LivePoseObservation {
  const LivePoseObservation({
    required this.usable,
    required this.message,
    this.pose,
    this.minimumTurn = .12,
  });
  factory LivePoseObservation.fromJson(Map<String, dynamic> json) =>
      LivePoseObservation(
        usable: json['usable'] == true,
        message: json['message'] as String? ?? 'Đang kiểm tra khuôn mặt…',
        pose: (json['pose'] as num?)?.toDouble(),
        minimumTurn: (json['minimumTurn'] as num?)?.toDouble() ?? .12,
      );
  final bool usable;
  final String message;
  final double? pose;
  final double minimumTurn;
}

class PoseGateResult {
  const PoseGateResult(this.correct, this.message);
  final bool correct;
  final String message;
}

/// Guidance uses the same nose/eye projection and direction sign as the server.
/// Stricter margins leave room for natural movement at the final verification.
PoseGateResult checkLivePose(
  LivePoseObservation observation,
  int step,
  String direction,
  double? baseline,
) {
  final pose = observation.pose;
  if (!observation.usable || pose == null || !pose.isFinite) {
    return PoseGateResult(false, observation.message);
  }
  if (step == 0) {
    if (pose.abs() > .15) {
      return const PoseGateResult(
        false,
        'Nhìn thẳng vào camera, chưa quay đầu.',
      );
    }
  } else if (step == 1) {
    if (baseline == null) {
      return const PoseGateResult(false, 'Cần chụp tư thế nhìn thẳng trước.');
    }
    final turn = (pose - baseline) * (direction == 'left' ? 1 : -1);
    final side = direction == 'left' ? 'trái' : 'phải';
    if (turn < -.04) {
      return PoseGateResult(
        false,
        'Bạn đang quay ngược hướng. Quay sang $side của bạn.',
      );
    }
    if (turn < observation.minimumTurn + .025) {
      return PoseGateResult(false, 'Quay thêm một chút sang $side của bạn.');
    }
  } else {
    if (baseline == null || (pose - baseline).abs() > .10 || pose.abs() > .25) {
      return const PoseGateResult(false, 'Quay mặt về chính diện như ảnh đầu.');
    }
  }
  return const PoseGateResult(true, 'Đúng động tác. Giữ yên một chút…');
}
