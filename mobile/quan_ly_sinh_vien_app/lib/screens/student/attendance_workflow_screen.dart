import 'dart:async';
import 'dart:io';
import 'dart:typed_data';
import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../core/attendance/attendance_workflow_api.dart';
import '../../core/face/camera_frame_codec.dart';
import 'widgets/guided_face_capture.dart';

class AttendanceWorkflowScreen extends StatefulWidget {
  const AttendanceWorkflowScreen({
    super.key,
    this.enrollment = false,
    this.api,
  });
  final bool enrollment;
  final AttendanceWorkflowApi? api;
  @override
  State<AttendanceWorkflowScreen> createState() =>
      _AttendanceWorkflowScreenState();
}

class _AttendanceWorkflowScreenState extends State<AttendanceWorkflowScreen>
    with WidgetsBindingObserver {
  late final _api = widget.api ?? AttendanceWorkflowApi();
  CameraController? _camera;
  Widget? _cameraPreview;
  MobileScannerController? _scanner;
  Map<String, dynamic>? _challenge;
  Map<String, dynamic>? _profile;
  List<dynamic> _history = [];
  bool _busy = false, _scanning = false, _consent = false, _completed = false;
  String? _message;
  Timer? _clock;
  int _cameraGeneration = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    unawaited(_loadProfile());
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted || _challenge == null) return;
      if (_remaining <= 0 && !_busy) {
        unawaited(_reset());
        setState(() => _message = 'Lượt xác minh đã hết hạn. Hãy bắt đầu lại.');
      } else {
        setState(() {});
      }
    });
  }

  int get _remaining => _challenge == null
      ? 0
      : DateTime.parse(
          _challenge!['expiresAtUtc'] as String,
        ).difference(DateTime.now().toUtc()).inSeconds;
  Future<void> _loadProfile() async {
    try {
      final value = await _api.request('ho-so');
      final history = !widget.enrollment
          ? await _api.request('ket-qua-cua-toi') as List
          : <dynamic>[];
      if (mounted) {
        setState(() {
          _profile = Map<String, dynamic>.from(value as Map);
          _history = history;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _message = e.toString());
    }
  }

  Future<void> _release() async {
    final camera = _camera;
    _camera = null;
    _cameraPreview = null;
    final scanner = _scanner;
    _scanner = null;
    if (scanner != null) {
      await scanner.stop();
      await scanner.dispose();
    }
    await camera?.dispose();
  }

  Future<void> _reset() async {
    _cameraGeneration++;
    _scanning = false;
    _challenge = null;
    await _release();
    if (mounted) setState(() {});
  }

  Future<void> _scan() async {
    await _reset();
    if (!mounted) return;
    setState(() {
      _scanner = MobileScannerController(formats: [BarcodeFormat.qrCode]);
      _scanning = true;
      _message = null;
      _completed = false;
    });
  }

  Future<void> _detected(BarcodeCapture capture) async {
    if (_busy || !_scanning) return;
    final values = capture.barcodes.map((b) => b.rawValue).whereType<String>();
    if (values.isEmpty) return;
    setState(() {
      _busy = true;
      _message = 'Đang kiểm tra tài khoản và lớp học…';
      _scanning = false;
    });
    try {
      await _release();
      final challenge = await _api.scan(values.first);
      await _openFaceCamera(challenge);
    } catch (e) {
      await _reset();
      if (mounted) setState(() => _message = e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _beginEnrollment() async {
    if (_busy || !_consent) return;
    setState(() {
      _busy = true;
      _message = null;
      _completed = false;
    });
    try {
      final value = await _api.challenge();
      await _openFaceCamera(value);
    } catch (e) {
      await _reset();
      if (mounted) setState(() => _message = e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _openFaceCamera(Map<String, dynamic> challenge) async {
    if (!mounted) return;
    final generation = _cameraGeneration;
    final cameras = await availableCameras();
    if (cameras.isEmpty) {
      throw const AttendanceWorkflowException('Không tìm thấy camera.');
    }
    final front = cameras.where(
      (c) => c.lensDirection == CameraLensDirection.front,
    );
    final controller = CameraController(
      front.isEmpty ? cameras.first : front.first,
      ResolutionPreset.medium,
      enableAudio: false,
    );
    try {
      await controller.initialize();
      if (!mounted || generation != _cameraGeneration) {
        await controller.dispose();
        return;
      }
      setState(() {
        _camera = controller;
        _cameraPreview = CameraPreview(controller);
        _challenge = challenge;
        _message = null;
      });
    } catch (_) {
      await controller.dispose();
      rethrow;
    }
  }

  Future<Uint8List> _captureFrame() async {
    final camera = _camera;
    if (camera == null || _challenge == null || _remaining <= 0) {
      throw const AttendanceWorkflowException(
        'Lượt xác minh hết hạn. Hãy bắt đầu lại.',
      );
    }
    final generation = _cameraGeneration;
    if (camera.value.isPreviewPaused) await camera.resumePreview();
    final photo = await camera.takePicture();
    try {
      final capturedBytes = await photo.readAsBytes();
      if (!mounted || generation != _cameraGeneration) {
        throw const AttendanceWorkflowException(
          'Lượt chụp đã hủy. Hãy bắt đầu lại.',
        );
      }
      final bytes = encodeFaceCameraFrame(
        capturedBytes,
        mirrored:
            kIsWeb &&
            camera.description.lensDirection != CameraLensDirection.back,
      );
      if (bytes.length > 1050000) {
        throw const AttendanceWorkflowException(
          'Ảnh quá lớn. Hãy thử lại với độ phân giải thấp hơn.',
        );
      }
      return bytes;
    } finally {
      if (!kIsWeb) {
        final file = File(photo.path);
        if (await file.exists()) await file.delete();
      }
    }
  }

  Future<void> _submitFrames(List<String> frames) async {
    final challenge = _challenge;
    if (_busy || challenge == null) return;
    setState(() {
      _busy = true;
      _message = 'Đang kiểm tra người thật và đối chiếu khuôn mặt…';
    });
    try {
      await _release();
      if (mounted) setState(() {});
      final value = await _api.submit(
        challenge,
        frames,
        enrollment: widget.enrollment,
      );
      await _reset();
      if (mounted) {
        setState(() {
          _completed = true;
          _message =
              value['message']?.toString() ??
              'Điểm danh thành công. Đã ghi nhận có mặt.';
        });
      }
      await _loadProfile();
    } catch (e) {
      await _reset();
      if (mounted) {
        setState(
          () => _message = '$e Hãy bắt đầu lại để lấy lượt xác minh mới.',
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Safari can become inactive while asking for camera permission. No camera
    // has been installed yet, so do not cancel that initial permission flow.
    if (state == AppLifecycleState.inactive && _busy && _camera == null) return;
    if (state != AppLifecycleState.resumed) {
      unawaited(_reset());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _clock?.cancel();
    unawaited(_release());
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final challenge = _challenge;
    final camera = _camera;
    final title = widget.enrollment ? 'Đăng ký khuôn mặt' : 'Điểm danh QR';
    if (challenge != null &&
        camera != null &&
        camera.value.isInitialized &&
        _cameraPreview != null) {
      return Scaffold(
        appBar: AppBar(title: Text(title)),
        body: GuidedFaceCapture(
          key: ValueKey(_cameraGeneration),
          preview: _cameraPreview!,
          captureFrame: _captureFrame,
          inspectPose: (frame) =>
              _api.inspectPose(challenge['challengeId'] as String, frame),
          onSubmit: _submitFrames,
          onCancel: () => unawaited(_reset()),
          direction: challenge['direction'] as String,
          identity: '${challenge['maSinhVien']} · ${challenge['hoTen']}',
          classLabel: challenge['maLopHocPhan'] == null
              ? null
              : '${challenge['maLopHocPhan']} · ${challenge['tenMonHoc']}',
          remaining: _remaining,
        ),
      );
    }
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          if (_message != null)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  _message!,
                  style: TextStyle(
                    color: _completed ? Colors.green.shade800 : null,
                  ),
                ),
              ),
            ),
          if (_busy) const LinearProgressIndicator(),
          if (_scanning && _scanner != null) ...[
            const Text('Quét QR trên màn hình của giảng viên.'),
            const SizedBox(height: 12),
            SizedBox(
              height: 360,
              child: MobileScanner(
                controller: _scanner!,
                onDetect: (value) => unawaited(_detected(value)),
              ),
            ),
            TextButton(
              onPressed: () => unawaited(_reset()),
              child: const Text('Hủy quét'),
            ),
          ] else if (!_busy) ...[
            if (widget.enrollment) ...[
              Text(
                _profile == null
                    ? 'Đang kiểm tra hồ sơ khuôn mặt…'
                    : _profile?['biTuChoi'] == true
                    ? 'CBQL/Admin yêu cầu đăng ký lại khuôn mặt.'
                    : _profile?['daDuyet'] == true
                    ? 'Hồ sơ đã được duyệt, dùng cho tất cả lớp.'
                    : _profile?['daDangKy'] == true
                    ? 'Đã đăng ký, đang chờ duyệt một lần để dùng cho tất cả lớp.'
                    : 'Chưa có hồ sơ khuôn mặt.',
              ),
              const SizedBox(height: 8),
              const Text(
                'Mỗi sinh viên chỉ đăng ký một hồ sơ khuôn mặt. Sau khi được duyệt, bạn dùng hồ sơ đó điểm danh ở mọi lớp đã tham gia.',
              ),
              TextButton(
                onPressed: _loadProfile,
                child: const Text('Cập nhật trạng thái hồ sơ'),
              ),
              if (_profile?['biTuChoi'] == true)
                Text('Lý do: ${_profile?['lyDoTuChoi']}'),
              if (_profile != null &&
                  (_profile?['daDangKy'] != true ||
                      _profile?['biTuChoi'] == true)) ...[
                const SizedBox(height: 12),
                const ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.face_retouching_natural),
                  title: Text('Căn mặt và chụp theo hướng dẫn'),
                  subtitle: Text(
                    'Camera luôn hiển thị và kiểm tra động tác trực tiếp. Chỉ khi đúng hướng và giữ ổn định, ứng dụng mới nhận ảnh rồi chuyển bước.',
                  ),
                ),
                const ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.photo_library_outlined),
                  title: Text('Xem lại trước khi gửi'),
                  subtitle: Text(
                    'Bạn có thể kiểm tra ba ảnh và chụp lại. Bỏ khẩu trang, giữ đủ ánh sáng và chỉ để một người trong khung.',
                  ),
                ),
                const Text(
                  'Ảnh được kiểm tra người thật và động tác. Ảnh đăng ký và mẫu khuôn mặt được lưu mã hóa để CBQL/Admin đối chiếu với ảnh thẻ và phục vụ điểm danh.',
                ),
                CheckboxListTile(
                  value: _consent,
                  onChanged: (v) => setState(() => _consent = v ?? false),
                  title: const Text(
                    'Tôi đồng ý lưu ảnh đăng ký và mẫu khuôn mặt mã hóa để CBQL/Admin đối chiếu và phục vụ điểm danh.',
                  ),
                ),
                ElevatedButton(
                  onPressed: _consent ? _beginEnrollment : null,
                  child: Text(
                    _profile?['biTuChoi'] == true
                        ? 'Đăng ký lại'
                        : 'Bắt đầu đăng ký',
                  ),
                ),
                const Text(
                  'CBQL/Admin đối chiếu ảnh thẻ với ảnh đăng ký để duyệt; bạn không cần có mặt hoặc chụp lại để duyệt. Mỗi buổi điểm danh vẫn xác minh với hồ sơ đã có. Chỉ đăng ký lại khi được yêu cầu.',
                ),
              ],
            ] else ...[
              const Text(
                '1. Quét QR của giảng viên\n2. Xác nhận đúng tài khoản và lớp\n3. Xác minh người thật và khuôn mặt',
                style: TextStyle(fontSize: 18, height: 1.6),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: _scan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Quét QR điểm danh'),
              ),
              TextButton(
                onPressed: () async {
                  await Navigator.push(
                    context,
                    MaterialPageRoute<void>(
                      builder: (_) =>
                          const AttendanceWorkflowScreen(enrollment: true),
                    ),
                  );
                  if (mounted) await _loadProfile();
                },
                child: Text(
                  _profile?['daDangKy'] == true
                      ? 'Xem hồ sơ khuôn mặt dùng chung'
                      : 'Đăng ký khuôn mặt một lần',
                ),
              ),
              const Divider(),
              Text(
                'Kết quả điểm danh của tôi',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              TextButton(
                onPressed: _loadProfile,
                child: const Text('Cập nhật kết quả'),
              ),
              if (_history.isEmpty) const Text('Chưa có kết quả điểm danh.'),
              for (final row in _history)
                ListTile(
                  title: Text('${row['maLopHocPhan']} · ${row['tenMonHoc']}'),
                  subtitle: Text(
                    '${row['ngayHoc'].toString().split('T').first} · ${row['trangThai']}',
                  ),
                ),
            ],
          ],
        ],
      ),
    );
  }
}
