import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:camera/camera.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import '../../core/attendance/attendance_models.dart';
import '../../core/attendance/attendance_service.dart';
import '../../core/auth/auth_service.dart';
import '../../core/face/face_detection_service.dart';
import '../../core/theme/app_colors.dart';

// ─── Màn hình Điểm Danh Khuôn Mặt ───────────────────────────────────────────

class StudentFaceScanScreen extends StatefulWidget {
  final String? maBuoiHoc;
  final String? tenMonHoc;
  final String? phongHoc;
  final String? thoiGian;

  const StudentFaceScanScreen({
    super.key,
    this.maBuoiHoc,
    this.tenMonHoc,
    this.phongHoc,
    this.thoiGian,
  });

  @override
  State<StudentFaceScanScreen> createState() => _StudentFaceScanScreenState();
}

// ─── Các trạng thái màn hình ─────────────────────────────────────────────────

enum _ScanState {
  /// Chờ mở camera hoặc chờ người dùng bắt đầu.
  idle,

  /// Đang lấy hình ảnh từ camera để phân tích.
  capturing,

  /// Đang gửi ảnh và chờ backend xử lý.
  processing,

  /// Đã điểm danh thành công (backend xác nhận).
  success,

  /// Đã điểm danh trước đó trong buổi học này.
  alreadyCheckedIn,

  /// Không tìm thấy khuôn mặt trong ảnh.
  noFaceDetected,

  /// Nhiều khuôn mặt.
  multipleFaces,

  /// Khuôn mặt bị phát hiện là giả mạo.
  spoofDetected,

  /// Khuôn mặt chưa đăng ký trong AI hoặc không khớp tài khoản.
  faceMismatch,

  /// Sinh viên không đăng ký lớp này.
  notEnrolled,

  /// Lỗi kết nối hoặc dịch vụ AI không khả dụng.
  serviceError,
}

// ─── State ───────────────────────────────────────────────────────────────────

class _StudentFaceScanScreenState extends State<StudentFaceScanScreen>
    with WidgetsBindingObserver {
  // ── Camera ──────────────────────────────────────────────────────────────────
  List<CameraDescription> _availableCameras = [];
  CameraController? _cameraController;
  int _selectedCameraIndex = 0;
  bool _isCameraInitializing = true;
  String? _cameraErrorMessage;

  // ── Phân tích khuôn mặt cục bộ (Google MLKit, chỉ để hướng dẫn giao diện) ─
  final FaceDetectionService? _faceDetectionService =
      kIsWeb ? null : FaceDetectionService();
  FaceDetectionResult? _localDetectionResult;
  bool _isDetectingFrame = false;
  DateTime _lastDetectionTime = DateTime.fromMillisecondsSinceEpoch(0);

  // ── Trạng thái điểm danh ────────────────────────────────────────────────────
  _ScanState _scanState = _ScanState.idle;
  bool _isProcessing = false;
  String _statusMessage = 'Nhìn thẳng vào camera, đủ sáng';
  AttendanceResult? _attendanceResult;

  // ── Thông tin buổi học ───────────────────────────────────────────────────────
  final AttendanceService _attendanceService = AttendanceService();
  List<TodaySessionInfo> _sessions = [];
  TodaySessionInfo? _selectedSession;
  bool _isLoadingSessions = true;
  String? _sessionLoadError;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _loadSessions();
    _initCamera();
  }

  // ─── Tải danh sách buổi học ──────────────────────────────────────────────────

  Future<void> _loadSessions() async {
    // Nếu đã có maBuoiHoc từ caller, tạo session tạm
    if (widget.maBuoiHoc != null && widget.maBuoiHoc!.isNotEmpty) {
      final preloaded = TodaySessionInfo(
        maBuoiHoc: widget.maBuoiHoc!,
        tenMonHoc: widget.tenMonHoc ?? 'Buổi học hiện tại',
        maMonHoc: '',
        maLopHocPhan: '',
        giangVien: '',
        thoiGian: widget.thoiGian ?? '',
        phong: widget.phongHoc ?? '',
        trangThaiBuoiHoc: 'DangDienRa',
        daDiemDanh: false,
      );
      if (mounted) {
        setState(() {
          _sessions = [preloaded];
          _selectedSession = preloaded;
          _isLoadingSessions = false;
        });
      }
      return;
    }

    // Gọi API lấy buổi học hôm nay
    try {
      final sessions = await _attendanceService.getSessionsToday();
      if (mounted) {
        setState(() {
          _sessions = sessions;
          _selectedSession = sessions.isNotEmpty ? sessions.first : null;
          _isLoadingSessions = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoadingSessions = false;
          _sessions = [];
          _selectedSession = null;
          _sessionLoadError = 'Không tải được buổi học: $e';
        });
      }
    }
  }

  // ─── Camera ─────────────────────────────────────────────────────────────────

  Future<void> _initCamera() async {
    await _releaseCamera();
    if (mounted) {
      setState(() {
        _isCameraInitializing = true;
        _cameraErrorMessage = null;
      });
    }
    if (!kIsWeb && !Platform.isAndroid && !Platform.isIOS) {
      if (mounted) {
        setState(() {
          _cameraErrorMessage =
              'Quét khuôn mặt chỉ hỗ trợ Chrome, Android và iOS.';
          _isCameraInitializing = false;
        });
      }
      return;
    }
    try {
      _availableCameras = await availableCameras();
      if (_availableCameras.isEmpty) {
        if (mounted) {
          setState(() {
            _cameraErrorMessage = 'Không tìm thấy camera trên thiết bị.';
            _isCameraInitializing = false;
          });
        }
        return;
      }
      final frontIdx = _availableCameras.indexWhere(
        (c) => c.lensDirection == CameraLensDirection.front,
      );
      _selectedCameraIndex = frontIdx != -1 ? frontIdx : 0;
      await _setupCamera(_availableCameras[_selectedCameraIndex]);
    } catch (e) {
      if (mounted) {
        setState(() {
          _cameraErrorMessage = _cameraErrorDescription(e);
          _isCameraInitializing = false;
        });
      }
    }
  }

  String _cameraErrorDescription(Object error) {
    if (error is CameraException && kIsWeb) {
      switch (error.code) {
        case 'cameraNotReadable':
          return 'Chrome không đọc được webcam. Hiện có tab khác (có chấm đỏ 🔴) hoặc ứng dụng khác (Zoom, Teams, Camera...) đang dùng webcam. Vui lòng đóng tab/ứng dụng đó rồi nhấn Thử lại.';
        case 'CameraAccessDenied':
        case 'cameraPermission':
          return 'Chrome chưa được cấp quyền dùng webcam. Hãy nhấn biểu tượng cài đặt cạnh URL (localhost:5200) -> Cho phép Camera, sau đó tải lại trang.';
        case 'cameraNotFound':
          return 'Không tìm thấy webcam. Hãy kiểm tra kết nối hoặc bật webcam trên máy rồi thử lại.';
      }
    }
    return 'Không thể mở camera: $error';
  }

  Future<void> _setupCamera(CameraDescription cam) async {
    await _releaseCamera();
    final controller = CameraController(
      cam,
      ResolutionPreset.high,
      enableAudio: false,
      imageFormatGroup: kIsWeb ? null : ImageFormatGroup.yuv420,
    );
    _cameraController = controller;
    try {
      await controller.initialize();
      if (!kIsWeb) {
        await controller.startImageStream(
          (image) => unawaited(_processCameraFrame(image, cam)),
        );
      }
      if (mounted) {
        setState(() {
          _isCameraInitializing = false;
          if (kIsWeb) {
            _statusMessage = 'Camera sẵn sàng. Nhìn thẳng vào camera, đủ sáng.';
          }
        });
      }
    } on CameraException catch (e) {
      if (identical(_cameraController, controller)) {
        _cameraController = null;
      }
      await controller.dispose();
      if (mounted) {
        setState(() {
          _cameraErrorMessage = _cameraErrorDescription(e);
          _isCameraInitializing = false;
        });
      }
      debugPrint('[FaceScan] CameraException: $e');
    }
  }

  Future<void> _processCameraFrame(
    CameraImage image,
    CameraDescription camera,
  ) async {
    final faceDetectionService = _faceDetectionService;
    if (faceDetectionService == null ||
        _isDetectingFrame ||
        _isProcessing ||
        DateTime.now().difference(_lastDetectionTime) <
            const Duration(milliseconds: 500)) {
      return;
    }
    _isDetectingFrame = true;
    _lastDetectionTime = DateTime.now();
    try {
      final result =
          await faceDetectionService.detectCameraImage(image, camera);
      if (mounted &&
          (_localDetectionResult?.status != result.status ||
              _localDetectionResult?.faceCount != result.faceCount)) {
        setState(() => _localDetectionResult = result);
      }
    } catch (_) {
    } finally {
      _isDetectingFrame = false;
    }
  }

  Future<void> _releaseCamera() async {
    final controller = _cameraController;
    _cameraController = null;
    if (controller == null) return;
    if (controller.value.isStreamingImages) {
      try {
        await controller.stopImageStream();
      } catch (_) {}
    }
    await controller.dispose();
  }

  Future<void> _toggleCamera() async {
    if (_availableCameras.length < 2 || _isProcessing) return;
    _selectedCameraIndex =
        (_selectedCameraIndex + 1) % _availableCameras.length;
    if (mounted) setState(() => _isCameraInitializing = true);
    await _setupCamera(_availableCameras[_selectedCameraIndex]);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.inactive:
      case AppLifecycleState.paused:
      case AppLifecycleState.detached:
      case AppLifecycleState.hidden:
        unawaited(_releaseCamera());
      case AppLifecycleState.resumed:
        if (_cameraController == null && mounted) {
          unawaited(_initCamera());
        }
      default:
        break;
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    unawaited(_releaseCamera());
    final faceDetectionService = _faceDetectionService;
    if (faceDetectionService != null) {
      unawaited(faceDetectionService.close());
    }
    super.dispose();
  }

  // ─── Logic điểm danh ────────────────────────────────────────────────────────

  Future<void> _onCaptureAndCheckIn() async {
    if (_isProcessing) return;

    final session = _selectedSession;
    if (session == null) {
      _showErrorSnackBar('Chưa chọn buổi học cần điểm danh.');
      return;
    }

    if (session.daDiemDanh) {
      setState(() {
        _scanState = _ScanState.alreadyCheckedIn;
        _statusMessage = 'Bạn đã được ghi nhận có mặt trong buổi học này rồi.';
      });
      return;
    }

    final controller = _cameraController;
    if (controller == null || !controller.value.isInitialized) {
      _showErrorSnackBar('Camera chưa sẵn sàng. Vui lòng thử lại.');
      return;
    }

    if (mounted) {
      setState(() {
        _isProcessing = true;
        _scanState = _ScanState.capturing;
        _statusMessage = 'Đang chụp ảnh...';
      });
    }

    String? imageBase64;
    try {
      // Dừng stream để chụp ảnh tĩnh chất lượng cao
      if (controller.value.isStreamingImages) {
        await controller.stopImageStream();
      }

      final xFile = await controller.takePicture();
      final bytes = await xFile.readAsBytes();
      final snapshot = await _faceDetectionService?.detectFile(xFile.path);

      if (snapshot != null && !snapshot.hasSingleUsableFace) {
        if (mounted && identical(_cameraController, controller)) {
          await controller.startImageStream(
            (image) => unawaited(
              _processCameraFrame(
                image,
                _availableCameras[_selectedCameraIndex],
              ),
            ),
          );
          setState(() {
            _isProcessing = false;
            _scanState = _ScanState.idle;
            _statusMessage = snapshot.message;
          });
          _showErrorSnackBar(snapshot.message);
        }
        return;
      }

      imageBase64 = base64Encode(bytes);

      if (mounted && !kIsWeb && identical(_cameraController, controller)) {
        await controller.startImageStream(
          (image) => unawaited(
            _processCameraFrame(
              image,
              _availableCameras[_selectedCameraIndex],
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted &&
          !kIsWeb &&
          identical(_cameraController, controller) &&
          controller.value.isInitialized &&
          !controller.value.isStreamingImages) {
        try {
          await controller.startImageStream(
            (image) => unawaited(
              _processCameraFrame(
                image,
                _availableCameras[_selectedCameraIndex],
              ),
            ),
          );
        } on CameraException catch (streamError) {
          debugPrint(
              '[FaceScan] Could not restart camera stream: $streamError');
        }
      }
      if (mounted) {
        setState(() {
          _isProcessing = false;
          _scanState = _ScanState.serviceError;
          _statusMessage = 'Không thể chụp ảnh: $e';
        });
      }
      return;
    }

    if (mounted) {
      setState(() {
        _scanState = _ScanState.processing;
        _statusMessage = 'Đang gửi ảnh và xử lý nhận diện...';
      });
    }

    // Gọi backend
    final result = await _attendanceService.checkInWithFace(
      maBuoiHoc: session.maBuoiHoc,
      imageBase64: imageBase64,
    );

    if (!mounted) return;

    _attendanceResult = result;

    setState(() {
      _isProcessing = false;

      switch (result.code) {
        case AttendanceResultCode.success:
          _scanState = _ScanState.success;
          _statusMessage = result.thongBao;
          // Cập nhật session để phản ánh đã điểm danh
          if (_selectedSession != null) {
            _selectedSession = TodaySessionInfo(
              maBuoiHoc: _selectedSession!.maBuoiHoc,
              tenMonHoc: _selectedSession!.tenMonHoc,
              maMonHoc: _selectedSession!.maMonHoc,
              maLopHocPhan: _selectedSession!.maLopHocPhan,
              giangVien: _selectedSession!.giangVien,
              thoiGian: _selectedSession!.thoiGian,
              phong: _selectedSession!.phong,
              trangThaiBuoiHoc: _selectedSession!.trangThaiBuoiHoc,
              daDiemDanh: true,
              thoiGianDiemDanhUtc: result.thoiGianCheckInUtc,
            );
          }
        case AttendanceResultCode.alreadyCheckedIn:
          _scanState = _ScanState.alreadyCheckedIn;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.noFaceDetected:
          _scanState = _ScanState.noFaceDetected;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.multipleFaces:
          _scanState = _ScanState.multipleFaces;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.spoofDetected:
          _scanState = _ScanState.spoofDetected;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.unknownFace:
        case AttendanceResultCode.faceMismatch:
          _scanState = _ScanState.faceMismatch;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.notEnrolled:
          _scanState = _ScanState.notEnrolled;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.sessionNotFound:
          _scanState = _ScanState.serviceError;
          _statusMessage = result.thongBao;
        case AttendanceResultCode.aiServiceUnavailable:
        case AttendanceResultCode.networkError:
        case AttendanceResultCode.unknownError:
          _scanState = _ScanState.serviceError;
          _statusMessage = result.thongBao;
      }
    });
  }

  void _onRetry() {
    if (mounted) {
      setState(() {
        _scanState = _ScanState.idle;
        _statusMessage = 'Nhìn thẳng vào camera, đủ sáng';
        _attendanceResult = null;
      });
    }
  }

  void _showErrorSnackBar(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg),
        backgroundColor: AppColors.danger,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  // ─── Thoát màn hình ────────────────────────────────────────────────────────
  Future<void> _handleExit() async {
    await _releaseCamera();
    if (!mounted) return;
    if (Navigator.canPop(context)) {
      Navigator.pop(context);
    } else {
      final authService = AuthService();
      if (authService.isAuthenticated) {
        if (authService.isLecturerOrAdmin) {
          Navigator.pushReplacementNamed(context, '/admin');
        } else {
          Navigator.pushReplacementNamed(context, '/student');
        }
      } else {
        Navigator.pushReplacementNamed(context, '/login');
      }
    }
  }

  // ─── Build ──────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: true,
      onPopInvokedWithResult: (didPop, result) {
        unawaited(_releaseCamera());
      },
      child: Scaffold(
        backgroundColor: Colors.white,
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.arrow_back, color: AppColors.textPrimary),
            onPressed: _isProcessing ? null : _handleExit,
          ),
          title: const Text(
            'Điểm danh khuôn mặt',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 18,
              fontWeight: FontWeight.w800,
            ),
          ),
          actions: [
            if (_availableCameras.length > 1)
              IconButton(
                icon: const Icon(Icons.flip_camera_ios_rounded,
                    color: AppColors.textPrimary),
                tooltip: 'Đổi camera',
                onPressed: _isProcessing ? null : _toggleCamera,
              ),
          ],
          backgroundColor: Colors.white,
          elevation: 0,
        ),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ── Khu vực camera ───────────────────────────────────────────────
                _CameraArea(
                  isCameraInitializing: _isCameraInitializing,
                  cameraErrorMessage: _cameraErrorMessage,
                  cameraController: _cameraController,
                  scanState: _scanState,
                  localDetectionResult: _localDetectionResult,
                  onRetryCamera: _initCamera,
                  onExit: _handleExit,
                ),

                const SizedBox(height: 16),

                // ── Trạng thái nhận diện ─────────────────────────────────────────
                _RecognitionStatusCard(
                  scanState: _scanState,
                  statusMessage: _statusMessage,
                  localDetectionResult: _localDetectionResult,
                  cameraError: _cameraErrorMessage,
                  isProcessing: _isProcessing,
                  attendanceResult: _attendanceResult,
                ),

                const SizedBox(height: 16),

                // ── Thông tin buổi học ───────────────────────────────────────────
                _SessionInfoCard(
                  isLoading: _isLoadingSessions,
                  error: _sessionLoadError,
                  sessions: _sessions,
                  selectedSession: _selectedSession,
                  onSessionChanged: _isProcessing
                      ? null
                      : (s) => setState(() => _selectedSession = s),
                  onReload: _loadSessions,
                ),

                const SizedBox(height: 20),

                // ── Nút hành động ────────────────────────────────────────────────
                _ActionButtons(
                  scanState: _scanState,
                  isProcessing: _isProcessing,
                  isCameraReady: _cameraController?.value.isInitialized == true,
                  hasSession: _selectedSession != null,
                  onCheckIn: _onCaptureAndCheckIn,
                  onRetry: _onRetry,
                  onBack: _handleExit,
                ),

                const SizedBox(height: 20),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ─── Widget: Khu vực camera ──────────────────────────────────────────────────

class _CameraArea extends StatelessWidget {
  final bool isCameraInitializing;
  final String? cameraErrorMessage;
  final CameraController? cameraController;
  final _ScanState scanState;
  final FaceDetectionResult? localDetectionResult;
  final VoidCallback onRetryCamera;
  final VoidCallback onExit;

  const _CameraArea({
    required this.isCameraInitializing,
    required this.cameraErrorMessage,
    required this.cameraController,
    required this.scanState,
    required this.localDetectionResult,
    required this.onRetryCamera,
    required this.onExit,
  });

  Color get _frameColor {
    switch (scanState) {
      case _ScanState.success:
        return AppColors.success;
      case _ScanState.alreadyCheckedIn:
        return AppColors.primary;
      case _ScanState.noFaceDetected:
      case _ScanState.multipleFaces:
      case _ScanState.spoofDetected:
      case _ScanState.faceMismatch:
      case _ScanState.notEnrolled:
      case _ScanState.serviceError:
        return AppColors.danger;
      default:
        final hasGoodFace = localDetectionResult?.hasSingleUsableFace == true;
        return hasGoodFace ? AppColors.success : AppColors.primary;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        width: 280,
        height: 320,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: _frameColor, width: 2.5),
        ),
        clipBehavior: Clip.antiAlias,
        child: _buildContent(),
      ),
    );
  }

  Widget _buildContent() {
    // Hiển thị thành công
    if (scanState == _ScanState.success) {
      return Container(
        color: const Color(0xFFD1FAE5),
        child: const Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.check_circle_rounded,
                color: AppColors.success, size: 80),
            SizedBox(height: 12),
            Text(
              'Điểm danh\nThành công!',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.success,
              ),
            ),
          ],
        ),
      );
    }

    // Đã điểm danh rồi
    if (scanState == _ScanState.alreadyCheckedIn) {
      return Container(
        color: const Color(0xFFEFF6FF),
        child: const Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.verified_rounded, color: AppColors.primary, size: 80),
            SizedBox(height: 12),
            Text(
              'Đã điểm danh\nbuổi học này',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w800,
                color: AppColors.primary,
              ),
            ),
          ],
        ),
      );
    }

    if (isCameraInitializing) {
      return Container(
        color: const Color(0xFF1E293B),
        child: const Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircularProgressIndicator(
                  color: AppColors.primary, strokeWidth: 2.5),
              SizedBox(height: 12),
              Text('Đang mở camera...',
                  style: TextStyle(color: Colors.white70, fontSize: 13)),
            ],
          ),
        ),
      );
    }

    if (cameraErrorMessage != null) {
      return Container(
        color: const Color(0xFF1E293B),
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.no_photography_rounded,
                color: Colors.white54, size: 64),
            const SizedBox(height: 12),
            Text(
              cameraErrorMessage!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white70, fontSize: 13),
            ),
            const SizedBox(height: 16),
            Wrap(
              alignment: WrapAlignment.center,
              spacing: 12,
              runSpacing: 8,
              children: [
                TextButton.icon(
                  onPressed: onRetryCamera,
                  icon: const Icon(Icons.refresh, color: AppColors.primary),
                  label: const Text('Thử lại',
                      style: TextStyle(color: AppColors.primary)),
                ),
                TextButton.icon(
                  onPressed: onExit,
                  icon: const Icon(Icons.arrow_back, color: Colors.white70),
                  label: const Text('Thoát ra',
                      style: TextStyle(color: Colors.white70)),
                ),
              ],
            ),
          ],
        ),
      );
    }

    if (cameraController?.value.isInitialized == true) {
      final controller = cameraController!;
      return LayoutBuilder(
        builder: (context, constraints) {
          final previewRatio = 1 / controller.value.aspectRatio;
          final areaRatio = constraints.maxWidth / constraints.maxHeight;
          final previewWidth = previewRatio > areaRatio
              ? constraints.maxHeight * previewRatio
              : constraints.maxWidth;
          final previewHeight = previewRatio > areaRatio
              ? constraints.maxHeight
              : constraints.maxWidth / previewRatio;

          return ClipRect(
            child: Stack(
              fit: StackFit.expand,
              children: [
                ColoredBox(
                  color: const Color(0xFF0F172A),
                  child: Center(
                    child: SizedBox(
                      width: previewWidth,
                      height: previewHeight,
                      child: CameraPreview(controller),
                    ),
                  ),
                ),
                Center(
                  child: Container(
                    width: 180,
                    height: 220,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(120),
                      border: Border.all(
                        color: Colors.white.withAlpha(190),
                        width: 2,
                      ),
                    ),
                  ),
                ),
                const Positioned(
                  left: 0,
                  right: 0,
                  bottom: 14,
                  child: Text(
                    'Căn mặt vào khung, giữ điện thoại ngang tầm mắt',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      shadows: [Shadow(blurRadius: 5, color: Colors.black87)],
                    ),
                  ),
                ),
              ],
            ),
          );
        },
      );
    }

    return Container(
      color: const Color(0xFF1E293B),
      child: const Center(
        child: Icon(Icons.person, size: 80, color: Colors.white38),
      ),
    );
  }
}

// ─── Widget: Trạng thái nhận diện ────────────────────────────────────────────

class _RecognitionStatusCard extends StatelessWidget {
  final _ScanState scanState;
  final String statusMessage;
  final FaceDetectionResult? localDetectionResult;
  final String? cameraError;
  final bool isProcessing;
  final AttendanceResult? attendanceResult;

  const _RecognitionStatusCard({
    required this.scanState,
    required this.statusMessage,
    required this.localDetectionResult,
    required this.cameraError,
    required this.isProcessing,
    required this.attendanceResult,
  });

  _StatusConfig get _config {
    switch (scanState) {
      case _ScanState.idle:
        if (cameraError != null) {
          return _StatusConfig(
            icon: Icons.camera_alt_outlined,
            color: AppColors.danger,
            bgColor: const Color(0xFFFFEBEE),
            label: 'Lỗi camera',
            detail: cameraError!,
          );
        }
        final localOk = localDetectionResult?.hasSingleUsableFace == true;
        return _StatusConfig(
          icon: localOk
              ? Icons.face_retouching_natural
              : Icons.center_focus_strong,
          color: localOk ? AppColors.success : AppColors.primary,
          bgColor: localOk ? const Color(0xFFD1FAE5) : const Color(0xFFEFF6FF),
          label: localOk
              ? 'Phát hiện khuôn mặt'
              : kIsWeb
                  ? 'Sẵn sàng quét'
                  : 'Chờ mở camera',
          detail: localDetectionResult?.message ?? statusMessage,
        );
      case _ScanState.capturing:
        return const _StatusConfig(
          icon: Icons.photo_camera_rounded,
          color: AppColors.primary,
          bgColor: Color(0xFFEFF6FF),
          label: 'Đang lấy hình ảnh',
          detail: 'Giữ nguyên khuôn mặt trong khung...',
          showProgress: true,
        );
      case _ScanState.processing:
        return const _StatusConfig(
          icon: Icons.memory_rounded,
          color: AppColors.primary,
          bgColor: Color(0xFFEFF6FF),
          label: 'Đang xử lý nhận diện',
          detail: 'AI đang phân tích khuôn mặt, vui lòng chờ...',
          showProgress: true,
        );
      case _ScanState.success:
        return _StatusConfig(
          icon: Icons.check_circle_rounded,
          color: AppColors.success,
          bgColor: const Color(0xFFD1FAE5),
          label: 'Điểm danh thành công!',
          detail: statusMessage,
        );
      case _ScanState.alreadyCheckedIn:
        return _StatusConfig(
          icon: Icons.verified_rounded,
          color: AppColors.primary,
          bgColor: const Color(0xFFEFF6FF),
          label: 'Đã điểm danh',
          detail: statusMessage,
        );
      case _ScanState.noFaceDetected:
        return _StatusConfig(
          icon: Icons.face_outlined,
          color: AppColors.danger,
          bgColor: const Color(0xFFFFEBEE),
          label: 'Không tìm thấy khuôn mặt',
          detail: statusMessage,
        );
      case _ScanState.multipleFaces:
        return _StatusConfig(
          icon: Icons.group_outlined,
          color: AppColors.danger,
          bgColor: const Color(0xFFFFEBEE),
          label: 'Phát hiện nhiều khuôn mặt',
          detail: statusMessage,
        );
      case _ScanState.spoofDetected:
        return _StatusConfig(
          icon: Icons.warning_amber_rounded,
          color: const Color(0xFFF59E0B),
          bgColor: const Color(0xFFFEF3C7),
          label: 'Kiểm tra chống giả mạo không đạt',
          detail: statusMessage,
        );
      case _ScanState.faceMismatch:
        return _StatusConfig(
          icon: Icons.person_off_outlined,
          color: AppColors.danger,
          bgColor: const Color(0xFFFFEBEE),
          label: 'Không khớp danh tính',
          detail: statusMessage,
        );
      case _ScanState.notEnrolled:
        return _StatusConfig(
          icon: Icons.block_outlined,
          color: AppColors.danger,
          bgColor: const Color(0xFFFFEBEE),
          label: 'Chưa đăng ký lớp học phần',
          detail: statusMessage,
        );
      case _ScanState.serviceError:
        return _StatusConfig(
          icon: Icons.cloud_off_rounded,
          color: AppColors.danger,
          bgColor: const Color(0xFFFFEBEE),
          label: 'Lỗi kết nối hoặc dịch vụ',
          detail: statusMessage,
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final cfg = _config;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: cfg.bgColor,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          cfg.showProgress
              ? SizedBox(
                  width: 28,
                  height: 28,
                  child: CircularProgressIndicator(
                    color: cfg.color,
                    strokeWidth: 2.5,
                  ),
                )
              : Icon(cfg.icon, color: cfg.color, size: 28),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  cfg.label,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                    color: cfg.color,
                  ),
                ),
                if (cfg.detail.isNotEmpty) ...[
                  const SizedBox(height: 3),
                  Text(
                    cfg.detail,
                    style: TextStyle(
                      fontSize: 12,
                      color: cfg.color.withAlpha(204),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusConfig {
  final IconData icon;
  final Color color;
  final Color bgColor;
  final String label;
  final String detail;
  final bool showProgress;

  const _StatusConfig({
    required this.icon,
    required this.color,
    required this.bgColor,
    required this.label,
    required this.detail,
    this.showProgress = false,
  });
}

// ─── Widget: Thông tin buổi học ──────────────────────────────────────────────

class _SessionInfoCard extends StatelessWidget {
  final bool isLoading;
  final String? error;
  final List<TodaySessionInfo> sessions;
  final TodaySessionInfo? selectedSession;
  final ValueChanged<TodaySessionInfo?>? onSessionChanged;
  final VoidCallback onReload;

  const _SessionInfoCard({
    required this.isLoading,
    required this.error,
    required this.sessions,
    required this.selectedSession,
    required this.onSessionChanged,
    required this.onReload,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'BUỔI HỌC',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: AppColors.primary,
                  letterSpacing: 0.8,
                ),
              ),
              if (!isLoading)
                GestureDetector(
                  onTap: onReload,
                  child: const Icon(Icons.refresh,
                      size: 18, color: AppColors.primary),
                ),
            ],
          ),
          const SizedBox(height: 10),
          if (isLoading)
            const Center(
              child: SizedBox(
                height: 20,
                width: 20,
                child: CircularProgressIndicator(
                    strokeWidth: 2, color: AppColors.primary),
              ),
            )
          else if (error != null && sessions.isEmpty)
            _SessionErrorView(message: error!, onReload: onReload)
          else if (sessions.isEmpty)
            const Text(
              'Hôm nay không có buổi học nào.',
              style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
            )
          else
            _buildSessionContent(),
        ],
      ),
    );
  }

  Widget _buildSessionContent() {
    final session = selectedSession;

    if (sessions.length > 1) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          DropdownButtonFormField<TodaySessionInfo>(
            value: selectedSession,
            items: sessions
                .map(
                  (s) => DropdownMenuItem(
                    value: s,
                    child: Text(
                      s.tenMonHoc,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 14),
                    ),
                  ),
                )
                .toList(),
            onChanged: onSessionChanged,
            decoration: const InputDecoration(
              isDense: true,
              border: OutlineInputBorder(),
              contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            ),
          ),
          if (session != null) ...[
            const SizedBox(height: 10),
            _SessionDetails(session: session),
          ],
        ],
      );
    }

    if (session == null) return const SizedBox.shrink();
    return _SessionDetails(session: session);
  }
}

class _SessionDetails extends StatelessWidget {
  final TodaySessionInfo session;

  const _SessionDetails({required this.session});

  @override
  Widget build(BuildContext context) {
    final isDone = session.daDiemDanh;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                session.tenMonHoc,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color:
                    isDone ? const Color(0xFFD1FAE5) : const Color(0xFFFEF3C7),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                isDone ? 'Đã điểm danh' : 'Chưa điểm danh',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: isDone ? AppColors.success : const Color(0xFFF59E0B),
                ),
              ),
            ),
          ],
        ),
        if (session.thoiGian.isNotEmpty || session.phong.isNotEmpty) ...[
          const SizedBox(height: 4),
          Text(
            [
              if (session.thoiGian.isNotEmpty) session.thoiGian,
              if (session.phong.isNotEmpty) session.phong,
            ].join(' · '),
            style:
                const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ],
        if (session.giangVien.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(
            'GV: ${session.giangVien}',
            style:
                const TextStyle(fontSize: 12, color: AppColors.textSecondary),
          ),
        ],
        if (session.maLopHocPhan.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(
            'Mã LHP: ${session.maLopHocPhan}',
            style:
                const TextStyle(fontSize: 11, color: AppColors.textSecondary),
          ),
        ],
        if (isDone && session.thoiGianDiemDanhUtc != null) ...[
          const SizedBox(height: 4),
          Text(
            'Đã điểm danh lúc: ${_formatTime(session.thoiGianDiemDanhUtc!.toLocal())}',
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.success,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ],
    );
  }

  String _formatTime(DateTime t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')} '
      '${t.day.toString().padLeft(2, '0')}/${t.month.toString().padLeft(2, '0')}/${t.year}';
}

class _SessionErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onReload;

  const _SessionErrorView({required this.message, required this.onReload});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          message,
          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 8),
        TextButton.icon(
          onPressed: onReload,
          icon: const Icon(Icons.refresh, size: 18, color: AppColors.primary),
          label:
              const Text('Tải lại', style: TextStyle(color: AppColors.primary)),
        ),
      ],
    );
  }
}

// ─── Widget: Nút hành động ───────────────────────────────────────────────────

class _ActionButtons extends StatelessWidget {
  final _ScanState scanState;
  final bool isProcessing;
  final bool isCameraReady;
  final bool hasSession;
  final VoidCallback onCheckIn;
  final VoidCallback onRetry;
  final VoidCallback onBack;

  const _ActionButtons({
    required this.scanState,
    required this.isProcessing,
    required this.isCameraReady,
    required this.hasSession,
    required this.onCheckIn,
    required this.onRetry,
    required this.onBack,
  });

  bool get _isTerminalState =>
      scanState == _ScanState.success ||
      scanState == _ScanState.alreadyCheckedIn ||
      scanState == _ScanState.notEnrolled;

  bool get _canCheckIn =>
      !isProcessing &&
      isCameraReady &&
      hasSession &&
      scanState != _ScanState.success &&
      scanState != _ScanState.alreadyCheckedIn &&
      scanState != _ScanState.notEnrolled;

  @override
  Widget build(BuildContext context) {
    if (_isTerminalState) {
      return SizedBox(
        width: double.infinity,
        height: 52,
        child: ElevatedButton.icon(
          onPressed: onBack,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
            elevation: 0,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
          icon: const Icon(Icons.check_rounded, size: 20),
          label: const Text(
            'Quay về trang chính',
            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
          ),
        ),
      );
    }

    return Row(
      children: [
        // Nút hủy / quay lại
        Expanded(
          child: SizedBox(
            height: 52,
            child: OutlinedButton(
              onPressed: isProcessing ? null : onBack,
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: Color(0xFFE2E8F0)),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14)),
              ),
              child: const Text(
                'Hủy bỏ',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 12),
        // Nút thử lại (nếu có lỗi)
        if (scanState != _ScanState.idle &&
            scanState != _ScanState.capturing &&
            scanState != _ScanState.processing &&
            !_isTerminalState)
          Expanded(
            child: SizedBox(
              height: 52,
              child: OutlinedButton.icon(
                onPressed: isProcessing ? null : onRetry,
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: AppColors.primary),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14)),
                ),
                icon: const Icon(Icons.refresh, color: AppColors.primary),
                label: const Text(
                  'Thử lại',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: AppColors.primary,
                  ),
                ),
              ),
            ),
          ),
        if (scanState == _ScanState.idle) ...[
          // Nút nhận diện chính
          Expanded(
            flex: 2,
            child: SizedBox(
              height: 52,
              child: ElevatedButton.icon(
                onPressed: _canCheckIn ? onCheckIn : null,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.success,
                  foregroundColor: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14)),
                ),
                icon: isProcessing
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            color: Colors.white, strokeWidth: 2))
                    : const Icon(Icons.camera_alt_rounded,
                        size: 20, color: Colors.white),
                label: Text(
                  isProcessing ? 'Đang xử lý...' : 'Điểm danh ngay',
                  style: const TextStyle(
                      fontSize: 15, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}
