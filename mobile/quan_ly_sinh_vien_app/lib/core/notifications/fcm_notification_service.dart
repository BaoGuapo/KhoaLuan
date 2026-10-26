import 'dart:async';
import 'dart:convert';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:http/http.dart' as http;

import '../api/api_constants.dart';
import '../auth/auth_provider.dart';
import 'notification_model.dart';

// ─── Background handler (top-level, required by FCM) ─────────────────────────
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  if (Firebase.apps.isEmpty) {
    await Firebase.initializeApp();
  }
}

// ─── Local notifications setup ────────────────────────────────────────────────
final FlutterLocalNotificationsPlugin _localNotificationsPlugin =
    FlutterLocalNotificationsPlugin();

const AndroidNotificationChannel _highImportanceChannel =
    AndroidNotificationChannel(
  'eduface_high_importance',
  'EduFace Thông báo',
  description:
      'Kênh thông báo chính của EduFace (lịch học, lịch thi, vắng học)',
  importance: Importance.high,
  enableVibration: true,
  playSound: true,
);

/// Singleton service quản lý FCM & danh sách thông báo trong app.
class FcmNotificationService extends ChangeNotifier {
  static final FcmNotificationService _instance =
      FcmNotificationService._internal();

  factory FcmNotificationService() => _instance;
  FcmNotificationService._internal();

  // ── State ──────────────────────────────────────────────────────────────────
  final List<AppNotificationModel> _notifications = [];
  final StreamController<AppNotificationModel> _inAppStreamCtrl =
      StreamController<AppNotificationModel>.broadcast();
  final StreamController<String> _notificationOpenStreamCtrl =
      StreamController<String>.broadcast();

  String? _fcmToken;
  String? _pendingNotificationId;
  bool _isInitialized = false;
  bool _isFirebaseAvailable = false;
  bool _isLoading = false;
  String? _loadError;

  StreamSubscription<String>? _tokenRefreshSubscription;
  StreamSubscription<RemoteMessage>? _foregroundMessageSubscription;
  StreamSubscription<RemoteMessage>? _openedMessageSubscription;

  // ── Public getters ─────────────────────────────────────────────────────────
  List<AppNotificationModel> get notifications =>
      List.unmodifiable(_notifications);
  int get unreadCount => _notifications.where((n) => !n.isRead).length;
  bool get isInitialized => _isInitialized;
  bool get isFirebaseAvailable => _isFirebaseAvailable;
  bool get isLoading => _isLoading;
  String? get fcmToken => _fcmToken;
  String? get loadError => _loadError;

  /// Stream of incoming push notifications for in-app banner overlay.
  Stream<AppNotificationModel> get inAppNotificationStream =>
      _inAppStreamCtrl.stream;
  Stream<String> get notificationOpenStream =>
      _notificationOpenStreamCtrl.stream;

  AppNotificationModel? notificationById(String id) {
    for (final notification in _notifications) {
      if (notification.id == id) return notification;
    }
    return null;
  }

  String? takePendingNotificationId() {
    final id = _pendingNotificationId;
    _pendingNotificationId = null;
    return id;
  }

  // ── Initialise ─────────────────────────────────────────────────────────────
  Future<void> initialize() async {
    if (_isInitialized) return;
    _isInitialized = true;

    await _initLocalNotifications();

    if (Firebase.apps.isNotEmpty) {
      _isFirebaseAvailable = true;
      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

      final messaging = FirebaseMessaging.instance;
      try {
        final settings = await messaging.requestPermission(
          alert: true,
          announcement: false,
          badge: true,
          carPlay: false,
          criticalAlert: false,
          provisional: false,
          sound: true,
        );
        debugPrint('FCM permission: ${settings.authorizationStatus}');

        await messaging.setForegroundNotificationPresentationOptions(
          alert: true,
          badge: true,
          sound: true,
        );

        _fcmToken = await messaging.getToken();
        debugPrint('FCM Token: $_fcmToken');
      } catch (error) {
        debugPrint('Unable to obtain FCM token: $error');
      }

      // Listen for token refreshes
      _tokenRefreshSubscription = messaging.onTokenRefresh.listen((token) {
        _fcmToken = token;
        unawaited(_registerDevice(token));
        debugPrint('FCM Token refreshed: $token');
      });

      // Foreground messages → show local notification banner + add to list
      _foregroundMessageSubscription =
          FirebaseMessaging.onMessage.listen(_handleForegroundMessage);

      // Tap on notification while app in background (not terminated)
      _openedMessageSubscription =
          FirebaseMessaging.onMessageOpenedApp.listen(_handleRemoteMessage);

      // App was terminated – opened via notification tap
      final initialMessage = await messaging.getInitialMessage();
      if (initialMessage != null) _handleRemoteMessage(initialMessage);
    }

    await syncForCurrentUser();
    notifyListeners();
  }

  // ── Local Notifications Setup ──────────────────────────────────────────────
  Future<void> _initLocalNotifications() async {
    if (kIsWeb ||
        (defaultTargetPlatform != TargetPlatform.android &&
            defaultTargetPlatform != TargetPlatform.iOS)) {
      return;
    }

    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const darwinInit = DarwinInitializationSettings(
      requestAlertPermission: false,
      requestBadgePermission: false,
      requestSoundPermission: false,
    );
    const initSettings =
        InitializationSettings(android: androidInit, iOS: darwinInit);

    await _localNotificationsPlugin.initialize(
      initSettings,
      onDidReceiveNotificationResponse: (details) {
        if (details.payload != null) {
          _openNotification(details.payload!);
        }
      },
    );

    final launchDetails =
        await _localNotificationsPlugin.getNotificationAppLaunchDetails();
    final launchPayload = launchDetails?.notificationResponse?.payload;
    if (launchDetails?.didNotificationLaunchApp == true &&
        launchPayload != null) {
      _openNotification(launchPayload);
    }

    // Create the high-importance channel on Android 8+
    final androidImpl =
        _localNotificationsPlugin.resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>();
    await androidImpl?.createNotificationChannel(_highImportanceChannel);
  }

  /// Shows a heads-up / banner notification when the app is in the foreground.
  Future<void> _showLocalNotification(AppNotificationModel item) async {
    if (kIsWeb ||
        (defaultTargetPlatform != TargetPlatform.android &&
            defaultTargetPlatform != TargetPlatform.iOS)) {
      return;
    }

    final androidDetails = AndroidNotificationDetails(
      _highImportanceChannel.id,
      _highImportanceChannel.name,
      channelDescription: _highImportanceChannel.description,
      importance: Importance.high,
      priority: Priority.high,
      color: item.category.bannerColor,
      styleInformation: BigTextStyleInformation(item.body),
      icon: '@mipmap/ic_launcher',
    );
    const iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentBadge: true,
      presentSound: true,
    );
    final details =
        NotificationDetails(android: androidDetails, iOS: iosDetails);

    await _localNotificationsPlugin.show(
      item.id.hashCode,
      item.title,
      item.body,
      details,
      payload: item.id,
    );
  }

  // ── Message handlers ───────────────────────────────────────────────────────
  void _handleForegroundMessage(RemoteMessage message) {
    final incoming = _remoteMessageToModel(message);
    _upsertNotification(incoming);
    _inAppStreamCtrl.add(incoming); // trigger in-app banner overlay
    unawaited(_showLocalNotification(incoming));
    unawaited(loadNotifications());
  }

  void _handleRemoteMessage(RemoteMessage message) {
    final incoming = _remoteMessageToModel(message);
    _upsertNotification(incoming);
    _openNotification(incoming.id);
    unawaited(loadNotifications());
  }

  AppNotificationModel _remoteMessageToModel(RemoteMessage message) {
    final category = NotificationCategoryExtension.fromApiValue(
        message.data['category']?.toString());
    final notificationId = message.data['notificationId']?.toString() ??
        'fcm-${DateTime.now().microsecondsSinceEpoch}';
    return AppNotificationModel(
      id: notificationId,
      title: message.notification?.title ?? 'Thông báo mới',
      body: message.notification?.body ?? '',
      category: category,
      timestamp: DateTime.now(),
      isRead: false,
      detailContent: message.data['detailContent']?.toString(),
      data: message.data,
    );
  }

  void _upsertNotification(AppNotificationModel incoming) {
    final existingIndex = _notifications.indexWhere((n) => n.id == incoming.id);
    if (existingIndex == -1) {
      _notifications.insert(0, incoming);
      notifyListeners();
    }
  }

  void _openNotification(String id) {
    unawaited(markAsRead(id));
    if (_notificationOpenStreamCtrl.hasListener) {
      _notificationOpenStreamCtrl.add(id);
    } else {
      _pendingNotificationId = id;
    }
  }

  // ── Sync ───────────────────────────────────────────────────────────────────
  Future<void> syncForCurrentUser() async {
    await loadNotifications();
    if (_isFirebaseAvailable && _fcmToken != null) {
      await _registerDevice(_fcmToken!);
    }
  }

  // ── API calls ──────────────────────────────────────────────────────────────
  Future<void> loadNotifications() async {
    final token = AuthProvider.instance.accessToken;
    if (!_hasServerSession(token)) {
      _loadError = 'Đăng nhập máy chủ để tải thông báo.';
      notifyListeners();
      return;
    }

    _isLoading = true;
    notifyListeners();

    try {
      final response = await http
          .get(
            Uri.parse('${ApiConstants.baseUrl}${ApiConstants.notifications}'),
            headers: _authorizedHeaders(token!),
          )
          .timeout(const Duration(seconds: 6));

      if (response.statusCode == 200) {
        final payload = jsonDecode(response.body) as List<dynamic>;
        _notifications
          ..clear()
          ..addAll(payload.map((item) =>
              AppNotificationModel.fromJson(item as Map<String, dynamic>)));
        _loadError = null;
      } else {
        _loadError =
            'Không tải được thông báo (mã lỗi ${response.statusCode}).';
      }
    } catch (error) {
      _loadError = 'Không thể kết nối để tải thông báo: $error';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> markAsRead(String id) async {
    final index = _notifications.indexWhere((n) => n.id == id);
    if (index == -1 || _notifications[index].isRead) return true;

    if (!await _put('${ApiConstants.notifications}/$id/da-doc')) return false;
    _notifications[index].isRead = true;
    notifyListeners();
    return true;
  }

  Future<bool> markAllAsRead() async {
    if (!await _put('${ApiConstants.notifications}/da-doc')) return false;
    var changed = false;
    for (final item in _notifications) {
      if (!item.isRead) {
        item.isRead = true;
        changed = true;
      }
    }
    if (changed) notifyListeners();
    return true;
  }

  Future<bool> deleteNotification(String id) async {
    if (!_notifications.any((n) => n.id == id)) return true;
    if (!await _delete('${ApiConstants.notifications}/$id')) return false;
    _notifications.removeWhere((n) => n.id == id);
    notifyListeners();
    return true;
  }

  Future<void> removeCurrentDevice() async {
    final token = AuthProvider.instance.accessToken;
    if (!_isFirebaseAvailable ||
        _fcmToken == null ||
        !_hasServerSession(token)) {
      return;
    }

    try {
      await http
          .delete(
            Uri.parse(
                '${ApiConstants.baseUrl}${ApiConstants.notificationDevice}'),
            headers: _authorizedHeaders(token!),
            body: jsonEncode({'fcmToken': _fcmToken}),
          )
          .timeout(const Duration(seconds: 5));
    } catch (_) {}
  }

  // ── Private helpers ────────────────────────────────────────────────────────
  Future<void> _registerDevice(String token) async {
    final accessToken = AuthProvider.instance.accessToken;
    if (!_hasServerSession(accessToken)) return;

    try {
      await http
          .post(
            Uri.parse(
                '${ApiConstants.baseUrl}${ApiConstants.notificationDevice}'),
            headers: _authorizedHeaders(accessToken!),
            body: jsonEncode({
              'fcmToken': token,
              'platform': defaultTargetPlatform.name,
            }),
          )
          .timeout(const Duration(seconds: 5));
    } catch (_) {}
  }

  Future<bool> _put(String path) async {
    final token = AuthProvider.instance.accessToken;
    if (!_hasServerSession(token)) return false;
    try {
      final response = await http
          .put(
            Uri.parse('${ApiConstants.baseUrl}$path'),
            headers: _authorizedHeaders(token!),
          )
          .timeout(const Duration(seconds: 5));
      if (response.statusCode >= 200 && response.statusCode < 300) return true;
      _loadError =
          'Không lưu được thay đổi thông báo (mã lỗi ${response.statusCode}).';
    } catch (error) {
      _loadError = 'Không thể kết nối để cập nhật thông báo: $error';
    }
    notifyListeners();
    return false;
  }

  Future<bool> _delete(String path) async {
    final token = AuthProvider.instance.accessToken;
    if (!_hasServerSession(token)) return false;
    try {
      final response = await http
          .delete(
            Uri.parse('${ApiConstants.baseUrl}$path'),
            headers: _authorizedHeaders(token!),
          )
          .timeout(const Duration(seconds: 5));
      if (response.statusCode >= 200 && response.statusCode < 300) return true;
      _loadError = 'Không xóa được thông báo (mã lỗi ${response.statusCode}).';
    } catch (error) {
      _loadError = 'Không thể kết nối để xóa thông báo: $error';
    }
    notifyListeners();
    return false;
  }

  bool _hasServerSession(String? token) =>
      token != null && token.isNotEmpty;

  Map<String, String> _authorizedHeaders(String token) => {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      };

  @override
  void dispose() {
    _tokenRefreshSubscription?.cancel();
    _foregroundMessageSubscription?.cancel();
    _openedMessageSubscription?.cancel();
    _inAppStreamCtrl.close();
    _notificationOpenStreamCtrl.close();
    super.dispose();
  }
}
