import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/notifications/notification_model.dart';
import '../../core/theme/app_colors.dart';

/// Widget wrapper bao quanh toàn bộ app. Lắng nghe stream thông báo mới
/// và hiển thị banner in-app khi nhận push lúc foreground.
///
/// Sử dụng:
/// ```dart
/// InAppNotificationOverlay(
///   stream: notificationStream,
///   child: MaterialApp(...),
/// )
/// ```
class InAppNotificationOverlay extends StatefulWidget {
  const InAppNotificationOverlay({
    super.key,
    required this.child,
    required this.stream,
  });

  final Widget child;

  /// Stream phát ra [AppNotificationModel] mỗi khi có push notification mới
  /// trong foreground.
  final Stream<AppNotificationModel> stream;

  @override
  State<InAppNotificationOverlay> createState() =>
      _InAppNotificationOverlayState();
}

class _InAppNotificationOverlayState extends State<InAppNotificationOverlay> {
  final List<_BannerEntry> _queue = [];
  bool _isShowing = false;
  StreamSubscription<AppNotificationModel>? _sub;

  @override
  void initState() {
    super.initState();
    _sub = widget.stream.listen(_enqueue);
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  void _enqueue(AppNotificationModel item) {
    _queue.add(_BannerEntry(item: item));
    if (!_isShowing) _showNext();
  }

  void _showNext() {
    if (_queue.isEmpty || !mounted) return;
    _isShowing = true;
    final entry = _queue.removeAt(0);
    entry.completer.complete(_showBanner(entry.item));
    entry.completer.future.then((_) {
      _isShowing = false;
      _showNext();
    });
  }

  Future<void> _showBanner(AppNotificationModel item) {
    final completer = Completer<void>();

    final overlayEntry = OverlayEntry(
      builder: (context) => _NotificationBanner(
        item: item,
        onDismiss: () => completer.complete(),
      ),
    );

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        Overlay.of(context).insert(overlayEntry);
        // Auto-dismiss after 4 s
        Future.delayed(const Duration(seconds: 4), () {
          if (!completer.isCompleted) {
            overlayEntry.remove();
            completer.complete();
          }
        });
      } else {
        completer.complete();
      }
    });

    completer.future.then((_) {
      if (overlayEntry.mounted) overlayEntry.remove();
    });

    return completer.future;
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

class _BannerEntry {
  _BannerEntry({required this.item});
  final AppNotificationModel item;
  final Completer<void> completer = Completer<void>();
}

// ── Actual banner widget ───────────────────────────────────────────────────────

class _NotificationBanner extends StatefulWidget {
  const _NotificationBanner({required this.item, required this.onDismiss});

  final AppNotificationModel item;
  final VoidCallback onDismiss;

  @override
  State<_NotificationBanner> createState() => _NotificationBannerState();
}

class _NotificationBannerState extends State<_NotificationBanner>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<Offset> _slideAnim;
  late final Animation<double> _fadeAnim;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 350),
    );
    _slideAnim = Tween<Offset>(
      begin: const Offset(0, -1),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _ctrl, curve: Curves.easeOutBack));
    _fadeAnim = Tween<double>(begin: 0, end: 1)
        .animate(CurvedAnimation(parent: _ctrl, curve: Curves.easeIn));
    _ctrl.forward();
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  Future<void> _dismiss() async {
    await _ctrl.reverse();
    widget.onDismiss();
  }

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.of(context).padding.top;
    return Positioned(
      top: topPadding + 8,
      left: 16,
      right: 16,
      child: Material(
        color: Colors.transparent,
        child: SlideTransition(
          position: _slideAnim,
          child: FadeTransition(
            opacity: _fadeAnim,
            child: GestureDetector(
              onTap: _dismiss,
              onVerticalDragUpdate: (details) {
                if (details.primaryDelta! < -4) _dismiss();
              },
              child: Container(
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.15),
                      blurRadius: 20,
                      offset: const Offset(0, 6),
                    ),
                  ],
                  border: Border(
                    left: BorderSide(
                      color: widget.item.category.bannerColor,
                      width: 4,
                    ),
                  ),
                ),
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Icon circle
                      Container(
                        width: 38,
                        height: 38,
                        decoration: BoxDecoration(
                          color: widget.item.category.iconBgColor,
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          widget.item.category.icon,
                          color: widget.item.category.iconColor,
                          size: 19,
                        ),
                      ),
                      const SizedBox(width: 10),
                      // Text
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    widget.item.title,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      fontSize: 14,
                                      fontWeight: FontWeight.w800,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  widget.item.timeAgo,
                                  style: const TextStyle(
                                      fontSize: 10, color: AppColors.textMuted),
                                ),
                              ],
                            ),
                            const SizedBox(height: 3),
                            Text(
                              widget.item.body,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                fontSize: 12,
                                height: 1.4,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 4),
                      // Close
                      GestureDetector(
                        onTap: _dismiss,
                        child: const Padding(
                          padding: EdgeInsets.only(top: 2),
                          child: Icon(Icons.close_rounded,
                              size: 16, color: AppColors.textMuted),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
