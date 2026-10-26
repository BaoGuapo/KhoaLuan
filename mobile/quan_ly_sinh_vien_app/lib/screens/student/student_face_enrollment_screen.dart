import 'dart:async';
import 'dart:convert';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';

import '../../core/face/face_detection_service.dart';
import '../../core/face/face_embedding_service.dart';
import '../../core/face/face_enrollment_service.dart';
import '../../core/theme/app_colors.dart';

class StudentFaceEnrollmentScreen extends StatefulWidget {
  const StudentFaceEnrollmentScreen({super.key});

  @override
  State<StudentFaceEnrollmentScreen> createState() =>
      _StudentFaceEnrollmentScreenState();
}

class _StudentFaceEnrollmentScreenState
    extends State<StudentFaceEnrollmentScreen> with WidgetsBindingObserver {
  static const _sampleInstructions = [
    'Nhìn thẳng vào camera',
    'Nghiêng nhẹ đầu sang trái',
    'Nghiêng nhẹ đầu sang phải',
  ];

  final FaceEnrollmentService _enrollmentService = FaceEnrollmentService(
    embeddingService: TfLiteFaceEmbeddingService(),
    detectionService: FaceDetectionService(),
  );
  final FaceDetectionService _faceDetectionService = FaceDetectionService();
  final List<String> _samples = [];
  CameraController? _cameraController;
  bool _cameraLoading = true;
  bool _isSubmitting = false;
  bool _consentGiven = false;
  bool _isEnrolled = false;
  String? _cameraError;
  String? _message;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    unawaited(_initializeCamera());
  }

  Future<void> _initializeCamera() async {
    await _releaseCamera();
    if (!mounted) return;
    setState(() {
      _cameraLoading = true;
      _cameraError = null;
    });

    try {
      final cameras = await availableCameras();
      if (cameras.isEmpty) {
        throw CameraException('cameraNotFound', 'Không tìm thấy camera.');
      }
      final frontCamera = cameras.indexWhere(
        (camera) => camera.lensDirection == CameraLensDirection.front,
      );
      final camera = cameras[frontCamera < 0 ? 0 : frontCamera];
      final controller = CameraController(
        camera,
        ResolutionPreset.high,
        enableAudio: false,
      );
      _cameraController = controller;
      await controller.initialize();
      if (!mounted) {
        await _releaseCamera();
        return;
      }
      setState(() => _cameraLoading = false);
    } on CameraException catch (error) {
      if (!mounted) return;
      setState(() {
        _cameraError = error.description ?? error.code;
        _cameraLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _cameraError = 'Không thể mở camera: $error';
        _cameraLoading = false;
      });
    }
  }

  Future<void> _releaseCamera() async {
    final controller = _cameraController;
    _cameraController = null;
    if (controller == null) return;
    await controller.dispose();
  }

  Future<void> _captureSample() async {
    final controller = _cameraController;
    if (_isSubmitting ||
        controller == null ||
        !controller.value.isInitialized ||
        _samples.length >= _sampleInstructions.length) {
      return;
    }

    try {
      final photo = await controller.takePicture();
      final bytes = await photo.readAsBytes();
      final face = await _faceDetectionService.detectFile(photo.path);
      if (!face.hasSingleUsableFace) {
        throw FaceEnrollmentException(face.message);
      }
      if (bytes.length > 1200000) {
        throw const FaceEnrollmentException(
          'Ảnh vượt quá 1.2 MB. Hãy đưa mặt gần hơn và chụp lại.',
        );
      }
      if (bytes.length < 4 ||
          bytes[0] != 0xFF ||
          bytes[1] != 0xD8 ||
          bytes[bytes.length - 2] != 0xFF ||
          bytes[bytes.length - 1] != 0xD9) {
        throw const FaceEnrollmentException(
          'Ảnh camera không đúng định dạng JPEG. Vui lòng chụp lại.',
        );
      }
      if (!mounted) return;
      setState(() {
        _samples.add(base64Encode(bytes));
        _message = null;
      });
    } on FaceEnrollmentException catch (error) {
      if (mounted) setState(() => _message = error.message);
    } catch (error) {
      if (mounted) setState(() => _message = 'Không thể chụp ảnh: $error');
    }
  }

  Future<void> _submitSamples() async {
    if (_samples.length != _sampleInstructions.length ||
        !_consentGiven ||
        _isSubmitting) {
      return;
    }
    setState(() {
      _isSubmitting = true;
      _message = null;
    });

    try {
      final message = await _enrollmentService.enroll(_samples);
      if (!mounted) return;
      setState(() {
        _isEnrolled = true;
        _message = message;
      });
    } catch (error) {
      if (mounted) setState(() => _message = error.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _removeSample(int index) {
    setState(() {
      _samples.removeAt(index);
      _isEnrolled = false;
      _message = null;
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.inactive ||
        state == AppLifecycleState.paused ||
        state == AppLifecycleState.hidden ||
        state == AppLifecycleState.detached) {
      unawaited(_releaseCamera());
    } else if (state == AppLifecycleState.resumed && mounted) {
      unawaited(_initializeCamera());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    unawaited(_releaseCamera());
    unawaited(_faceDetectionService.close());
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final controller = _cameraController;
    final nextSample = _samples.length;

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Đăng ký khuôn mặt',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontWeight: FontWeight.w800,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.textPrimary),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            const Text(
              'Chụp 3 ảnh trực tiếp để tạo mẫu FaceID cho tài khoản sinh viên. '
              'Mỗi ảnh cần có một khuôn mặt rõ, nhìn thấy hai mắt, đủ sáng và '
              'giữ yên khi chụp. Hãy thử chụp ở nơi có ánh sáng phía trước.',
              style: TextStyle(color: AppColors.textSecondary, height: 1.5),
            ),
            const SizedBox(height: 16),
            ClipRRect(
              borderRadius: BorderRadius.circular(20),
              child: AspectRatio(
                aspectRatio: 4 / 5,
                child: _cameraLoading
                    ? const ColoredBox(
                        color: Color(0xFF1E293B),
                        child: Center(
                          child: CircularProgressIndicator(
                            color: AppColors.primary,
                          ),
                        ),
                      )
                    : _cameraError != null
                        ? ColoredBox(
                            color: const Color(0xFF1E293B),
                            child: Center(
                              child: Padding(
                                padding: const EdgeInsets.all(20),
                                child: Column(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Text(
                                      _cameraError!,
                                      textAlign: TextAlign.center,
                                      style: const TextStyle(
                                        color: Colors.white,
                                      ),
                                    ),
                                    const SizedBox(height: 12),
                                    OutlinedButton(
                                      onPressed: _initializeCamera,
                                      child: const Text('Thử mở camera lại'),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          )
                        : controller?.value.isInitialized == true
                            ? CameraPreview(controller!)
                            : const ColoredBox(
                                color: Color(0xFF1E293B),
                                child: Center(
                                  child: Text(
                                    'Camera chưa sẵn sàng.',
                                    style: TextStyle(color: Colors.white),
                                  ),
                                ),
                              ),
              ),
            ),
            const SizedBox(height: 16),
            if (!_isEnrolled) ...[
              Text(
                nextSample < _sampleInstructions.length
                    ? 'Ảnh ${nextSample + 1}/3: ${_sampleInstructions[nextSample]}'
                    : 'Đã đủ ảnh mẫu. Gửi ảnh để xác minh và đăng ký FaceID.',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 12),
              Wrap(
                alignment: WrapAlignment.center,
                spacing: 10,
                children: List.generate(_sampleInstructions.length, (index) {
                  final captured = index < _samples.length;
                  return InputChip(
                    label: Text('Ảnh ${index + 1}'),
                    avatar: Icon(
                      captured ? Icons.check_circle : Icons.camera_alt,
                      size: 18,
                      color: captured ? AppColors.success : AppColors.primary,
                    ),
                    onDeleted: captured ? () => _removeSample(index) : null,
                    deleteIconColor: AppColors.danger,
                  );
                }),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: _cameraLoading ||
                        _cameraError != null ||
                        nextSample >= _sampleInstructions.length
                    ? null
                    : _captureSample,
                icon: const Icon(Icons.camera_alt),
                label: Text(
                  nextSample >= _sampleInstructions.length
                      ? 'Đã chụp đủ 3 ảnh'
                      : 'Chụp ảnh ${nextSample + 1}',
                ),
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  minimumSize: const Size.fromHeight(48),
                ),
              ),
              const SizedBox(height: 12),
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                value: _consentGiven,
                onChanged: (value) =>
                    setState(() => _consentGiven = value ?? false),
                controlAffinity: ListTileControlAffinity.leading,
                title: const Text(
                  'Tôi đồng ý gửi ảnh lên máy chủ của trường để tạo và lưu '
                  'mẫu khuôn mặt phục vụ điểm danh.',
                  style: TextStyle(fontSize: 13, height: 1.4),
                ),
              ),
              FilledButton(
                onPressed: _samples.length == _sampleInstructions.length &&
                        _consentGiven &&
                        !_isSubmitting
                    ? _submitSamples
                    : null,
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.success,
                  minimumSize: const Size.fromHeight(48),
                ),
                child: _isSubmitting
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Text('Xác minh và đăng ký FaceID'),
              ),
            ],
            if (_message != null) ...[
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: _isEnrolled
                      ? const Color(0xFFD1FAE5)
                      : const Color(0xFFFFEBEE),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  _message!,
                  style: TextStyle(
                    color: _isEnrolled ? AppColors.success : AppColors.danger,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
            if (_isEnrolled) ...[
              const SizedBox(height: 16),
              FilledButton(
                onPressed: () => Navigator.pop(context, true),
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  minimumSize: const Size.fromHeight(48),
                ),
                child: const Text('Hoàn tất'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
