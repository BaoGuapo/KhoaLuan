import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../../../core/face/live_pose_guidance.dart';

/// A single mounted preview for the whole challenge. In particular, neither
/// capture progress nor status messages may replace the web platform view.
class GuidedFaceCapture extends StatefulWidget {
  const GuidedFaceCapture({
    super.key,
    required this.preview,
    required this.captureFrame,
    required this.inspectPose,
    required this.onSubmit,
    required this.onCancel,
    required this.direction,
    required this.identity,
    required this.remaining,
    this.classLabel,
  });

  final Widget preview;
  final Future<Uint8List> Function() captureFrame;
  final Future<LivePoseObservation> Function(Uint8List) inspectPose;
  final Future<void> Function(List<String>) onSubmit;
  final VoidCallback onCancel;
  final String direction;
  final String identity;
  final int remaining;
  final String? classLabel;

  @override
  State<GuidedFaceCapture> createState() => _GuidedFaceCaptureState();
}

class _GuidedFaceCaptureState extends State<GuidedFaceCapture> {
  final List<Uint8List> _photos = [];
  final Set<String> _encodedFrames = {};
  Timer? _timer;
  int _stable = 0;
  double? _baseline, _previousPose;
  String _feedback = 'Đặt mặt trong khung rồi bắt đầu xác thực trực tiếp.';
  bool _running = false, _capturing = false, _submitting = false;
  String? _error;
  int _run = 0;

  List<String> get _instructions => [
    'Nhìn thẳng vào camera',
    'Quay nhẹ đầu sang ${widget.direction == 'left' ? 'trái' : 'phải'} của bạn',
    'Nhìn thẳng trở lại',
  ];

  void _start() {
    if (_running || _capturing || _submitting || _photos.length == 3) return;
    if (widget.remaining < 8) {
      setState(
        () => _error = 'Không đủ thời gian chụp. Hãy hủy và bắt đầu lại.',
      );
      return;
    }
    _run++;
    setState(() {
      _error = null;
      _running = true;
      _stable = 0;
      _previousPose = null;
      _feedback = 'Đang tìm khuôn mặt và kiểm tra động tác…';
    });
    unawaited(_observeFrame(_run));
  }

  void _pause() {
    _timer?.cancel();
    _run++;
    setState(() {
      _running = false;
      _stable = 0;
      _previousPose = null;
      _feedback = 'Đã tạm dừng. Các bước đã nhận được giữ lại.';
    });
  }

  Future<void> _observeFrame(int run) async {
    if (_capturing || !mounted || run != _run) return;
    setState(() => _capturing = true);
    try {
      if (widget.remaining <= 0) {
        throw StateError('Lượt xác minh đã hết hạn. Hãy bắt đầu lại.');
      }
      final bytes = await widget.captureFrame().timeout(
        const Duration(seconds: 12),
        onTimeout: () => throw TimeoutException(
          'Camera chưa trả ảnh mới. Hãy hủy và mở lại camera.',
        ),
      );
      if (!mounted || run != _run) return;
      final observation = await widget.inspectPose(bytes);
      if (!mounted || run != _run) return;
      final encoded = base64Encode(bytes);
      final gate = checkLivePose(
        observation,
        _photos.length,
        widget.direction,
        _baseline,
      );
      if (_encodedFrames.contains(encoded)) {
        setState(() {
          _stable = 0;
          _previousPose = null;
          _feedback =
              'Camera trả lại ảnh cũ. Cử động nhẹ và kiểm tra camera trực tiếp.';
        });
        return;
      }
      setState(() {
        _feedback = gate.message;
        if (!gate.correct) {
          _stable = 0;
          _previousPose = null;
          return;
        }
        final pose = observation.pose!;
        _stable = _previousPose != null && (pose - _previousPose!).abs() < .045
            ? _stable + 1
            : 1;
        _previousPose = pose;
        if (_stable < 3) return;
        // Keep precisely the JPEG inspected by the server, never take another
        // unvalidated photo after a successful pose observation.
        if (_photos.isEmpty) _baseline = pose;
        _photos.add(bytes);
        _encodedFrames.add(encoded);
        _stable = 0;
        _previousPose = null;
        _running = _photos.length < 3;
        _feedback = _running
            ? 'Đã nhận đúng động tác. Làm theo hướng dẫn tiếp theo.'
            : 'Đã nhận đủ ba động tác. Kiểm tra ảnh trước khi gửi.';
      });
    } catch (e) {
      if (mounted && run == _run) {
        setState(() {
          _running = false;
          _error = e.toString();
        });
      }
    } finally {
      if (mounted) {
        setState(() => _capturing = false);
        if (run == _run && _running) {
          // Single-flight requests: no overlapping inference or stale response
          // can advance the next stage. Delay is pacing, never a capture gate.
          _timer = Timer(
            const Duration(milliseconds: 650),
            () => unawaited(_observeFrame(run)),
          );
        }
      }
    }
  }

  void _retake() {
    if (_submitting) return;
    _timer?.cancel();
    _run++;
    setState(() {
      _photos.clear();
      _encodedFrames.clear();
      _baseline = null;
      _previousPose = null;
      _stable = 0;
      _running = false;
      _error = null;
      _feedback = 'Đặt mặt trong khung rồi bắt đầu xác thực trực tiếp.';
    });
  }

  Future<void> _submit() async {
    if (_photos.length != 3 || _submitting) return;
    if (widget.remaining <= 0) {
      setState(() => _error = 'Lượt xác minh hết hạn. Hãy bắt đầu lại.');
      return;
    }
    setState(() => _submitting = true);
    try {
      await widget.onSubmit(
        List<String>.unmodifiable(_photos.map(base64Encode)),
      );
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Future<void> _showPhoto(int index) => showDialog<void>(
    context: context,
    builder: (context) => Dialog(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Ảnh bước ${index + 1}',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 12),
            ConstrainedBox(
              constraints: BoxConstraints(
                maxHeight: MediaQuery.sizeOf(context).height * .55,
              ),
              child: Image.memory(_photos[index], fit: BoxFit.contain),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Đóng'),
            ),
          ],
        ),
      ),
    ),
  );

  @override
  void dispose() {
    _run++;
    _timer?.cancel();
    _photos.clear();
    _encodedFrames.clear();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    const ink = Color(0xff182a42);
    const accent = Color(0xff167d79);
    final complete = _photos.length == 3;
    final step = _photos.length.clamp(0, 2);
    final title = complete ? 'Kiểm tra lại ảnh của bạn' : _instructions[step];
    final subtitle = complete
        ? 'Chạm ảnh nhỏ để xem rõ. Bạn có thể chụp lại trước khi gửi.'
        : step == 1
        ? 'Xoay đầu khoảng 20–30°, giữ mặt trong khung oval.'
        : 'Đưa mặt vào khung, ngang tầm mắt và giữ đủ ánh sáng.';
    return ColoredBox(
      color: const Color(0xfff4f7fa),
      child: SafeArea(
        top: false,
        child: LayoutBuilder(
          builder: (context, constraints) {
            // A non-lazy scroll view also keeps the video mounted on short or
            // landscape screens. Status changes never shift its element slot.
            return SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.verified_user_outlined,
                          color: accent,
                          size: 18,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            widget.identity,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        Text(
                          '${widget.remaining}s',
                          style: const TextStyle(
                            color: ink,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    if (widget.classLabel != null)
                      Text(
                        widget.classLabel!,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontSize: 12),
                      ),
                    const SizedBox(height: 12),
                    Row(
                      children: List.generate(
                        3,
                        (index) => Expanded(
                          child: Container(
                            margin: EdgeInsets.only(right: index < 2 ? 6 : 0),
                            height: 4,
                            decoration: BoxDecoration(
                              color: index < _photos.length
                                  ? accent
                                  : const Color(0xffdce4ec),
                              borderRadius: BorderRadius.circular(4),
                            ),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Text(
                      title,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 21,
                        fontWeight: FontWeight.w700,
                        color: ink,
                      ),
                    ),
                    SizedBox(
                      height: 44,
                      child: Center(
                        child: Text(
                          subtitle,
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            fontSize: 13,
                            color: Color(0xff586b80),
                          ),
                        ),
                      ),
                    ),
                    SizedBox(
                      height:
                          (constraints.maxHeight -
                                  378 -
                                  (widget.classLabel == null ? 0 : 18))
                              .clamp(220.0, 440.0),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(24),
                        child: Stack(
                          fit: StackFit.expand,
                          children: [
                            ColoredBox(
                              color: const Color(0xff102130),
                              child: Center(child: widget.preview),
                            ),
                            IgnorePointer(
                              child: CustomPaint(
                                painter: _FaceGuidePainter(
                                  complete ? 3 : _photos.length,
                                ),
                              ),
                            ),
                            Positioned(
                              top: 12,
                              left: 12,
                              child: DecoratedBox(
                                decoration: BoxDecoration(
                                  color: Colors.black54,
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(
                                    horizontal: 10,
                                    vertical: 6,
                                  ),
                                  child: Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Icon(
                                        Icons.circle,
                                        size: 7,
                                        color: Color(0xff68e0a5),
                                      ),
                                      SizedBox(width: 6),
                                      Text(
                                        'Camera trực tiếp',
                                        style: TextStyle(
                                          color: Colors.white,
                                          fontSize: 11,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ),
                            Positioned(
                              top: 12,
                              right: 12,
                              child: CircleAvatar(
                                backgroundColor: _running && _stable == 0
                                    ? const Color(0xffb7791f)
                                    : accent,
                                foregroundColor: Colors.white,
                                radius: 20,
                                child: _running
                                    ? Text(
                                        '$_stable/3',
                                        style: const TextStyle(
                                          fontSize: 16,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      )
                                    : Icon(
                                        complete
                                            ? Icons.check
                                            : step == 1
                                            ? (widget.direction == 'left'
                                                  ? Icons.turn_left
                                                  : Icons.turn_right)
                                            : Icons.face,
                                        size: 24,
                                      ),
                              ),
                            ),
                            Positioned(
                              bottom: 12,
                              left: 12,
                              right: 12,
                              child: Text(
                                complete
                                    ? 'Đã chụp đủ 3 ảnh'
                                    : 'Bước ${step + 1}/3 · ${_running ? 'Giữ động tác theo hướng dẫn' : 'Sẵn sàng khi bạn đã căn mặt'}',
                                textAlign: TextAlign.center,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 12,
                                  shadows: [Shadow(blurRadius: 4)],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                      children: List.generate(
                        3,
                        (index) => InkWell(
                          borderRadius: BorderRadius.circular(12),
                          onTap: complete && !_submitting
                              ? () => unawaited(_showPhoto(index))
                              : null,
                          child: _PhotoStep(
                            bytes: index < _photos.length
                                ? _photos[index]
                                : null,
                            label: [
                              'Nhìn thẳng',
                              widget.direction == 'left'
                                  ? 'Quay trái'
                                  : 'Quay phải',
                              'Thẳng lại',
                            ][index],
                            active: !complete && index == step,
                          ),
                        ),
                      ),
                    ),
                    SizedBox(
                      height: 72,
                      child: Center(
                        child: Text(
                          _error ?? _feedback,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 12,
                            color: _error != null
                                ? const Color(0xffb42318)
                                : _stable > 0
                                ? accent
                                : const Color(0xff586b80),
                          ),
                        ),
                      ),
                    ),
                    SizedBox(
                      height: 46,
                      child: FilledButton.icon(
                        onPressed: _submitting
                            ? null
                            : _running
                            ? _pause
                            : complete
                            ? _submit
                            : _capturing
                            ? null
                            : _start,
                        icon: _submitting
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : Icon(
                                complete
                                    ? Icons.check_circle_outline
                                    : Icons.camera_alt_outlined,
                              ),
                        label: Text(
                          _submitting
                              ? 'Đang xác minh…'
                              : complete
                              ? 'Gửi xác minh'
                              : _running
                              ? 'Tạm dừng'
                              : _photos.isEmpty
                              ? 'Bắt đầu xác thực trực tiếp'
                              : 'Tiếp tục xác thực bước ${step + 1}',
                        ),
                        style: FilledButton.styleFrom(
                          backgroundColor: accent,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14),
                          ),
                        ),
                      ),
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                      children: [
                        TextButton(
                          onPressed:
                              _submitting || (_photos.isEmpty && !_running)
                              ? null
                              : _retake,
                          child: const Text('Chụp lại từ đầu'),
                        ),
                        TextButton(
                          onPressed: _submitting ? null : widget.onCancel,
                          child: const Text('Hủy'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

class _PhotoStep extends StatelessWidget {
  const _PhotoStep({
    required this.bytes,
    required this.label,
    required this.active,
  });
  final Uint8List? bytes;
  final String label;
  final bool active;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      Container(
        width: 48,
        height: 48,
        decoration: BoxDecoration(
          color: const Color(0xffe7edf3),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: active || bytes != null
                ? const Color(0xff167d79)
                : Colors.transparent,
            width: 2,
          ),
        ),
        child: bytes == null
            ? Icon(
                Icons.face_outlined,
                color: active
                    ? const Color(0xff167d79)
                    : const Color(0xff8c9bac),
              )
            : ClipRRect(
                borderRadius: BorderRadius.circular(10),
                child: Image.memory(
                  bytes!,
                  fit: BoxFit.cover,
                  gaplessPlayback: true,
                  excludeFromSemantics: true,
                ),
              ),
      ),
      const SizedBox(height: 3),
      Text(
        label,
        style: TextStyle(
          fontSize: 11,
          color: active ? const Color(0xff167d79) : const Color(0xff586b80),
        ),
      ),
    ],
  );
}

class _FaceGuidePainter extends CustomPainter {
  const _FaceGuidePainter(this.completed);
  final int completed;
  @override
  void paint(Canvas canvas, Size size) {
    final height = size.height * .78;
    final width = (height * .73).clamp(100.0, size.width * .8);
    final oval = Rect.fromCenter(
      center: size.center(Offset.zero),
      width: width,
      height: height,
    );
    final mask = Path()
      ..fillType = PathFillType.evenOdd
      ..addRect(Offset.zero & size)
      ..addOval(oval);
    canvas.drawPath(mask, Paint()..color = const Color(0x66102130));
    canvas.drawOval(
      oval,
      Paint()
        ..color = const Color(0xffa9c5cf)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2,
    );
    canvas.drawArc(
      oval,
      -1.5708,
      6.2832 * completed / 3,
      false,
      Paint()
        ..color = const Color(0xff68e0a5)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 4
        ..strokeCap = StrokeCap.round,
    );
  }

  @override
  bool shouldRepaint(_FaceGuidePainter oldDelegate) =>
      completed != oldDelegate.completed;
}
