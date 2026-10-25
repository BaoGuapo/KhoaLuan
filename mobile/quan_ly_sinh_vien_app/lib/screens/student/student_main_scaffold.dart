import 'dart:async';

import 'package:flutter/material.dart';
import '../../core/notifications/fcm_notification_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/widgets/face_scan_icon.dart';
import 'student_face_scan_screen.dart';
import 'student_grades_tab.dart';
import 'student_home_tab.dart';
import 'notification_detail_screen.dart';
import 'student_notifications_tab.dart';
import 'student_profile_tab.dart';
import 'student_schedule_tab.dart';

class StudentMainScaffold extends StatefulWidget {
  const StudentMainScaffold({super.key, this.initialIndex = 0});

  final int initialIndex;

  @override
  State<StudentMainScaffold> createState() => _StudentMainScaffoldState();
}

class _StudentMainScaffoldState extends State<StudentMainScaffold> {
  late int _currentIndex;
  StreamSubscription<String>? _notificationOpenSubscription;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialIndex;
    final notificationService = FcmNotificationService();
    _notificationOpenSubscription =
        notificationService.notificationOpenStream.listen((id) {
      unawaited(_openNotification(id));
    });
    final pendingNotificationId =
        notificationService.takePendingNotificationId();
    if (pendingNotificationId != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        unawaited(_openNotification(pendingNotificationId));
      });
    }
  }

  @override
  void dispose() {
    _notificationOpenSubscription?.cancel();
    super.dispose();
  }

  Future<void> _openNotification(String id) async {
    final service = FcmNotificationService();
    var notification = service.notificationById(id);
    if (notification == null) {
      await service.loadNotifications();
      notification = service.notificationById(id);
    }
    if (!mounted || notification == null) return;

    setState(() => _currentIndex = 3);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => NotificationDetailScreen(item: notification!),
        ),
      );
    });
  }

  void _onTabTapped(int index) {
    if (index == 2) {
      // Tap on center Face Scan opens the Face Scan Attendance Screen
      Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => const StudentFaceScanScreen()),
      );
    } else {
      setState(() {
        _currentIndex = index;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final List<Widget> pages = [
      StudentHomeTab(onNavigateTab: (index) {
        if (index == 5) {
          // Open Grades Screen
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const StudentGradesTab()),
          );
        } else {
          setState(() {
            _currentIndex = index;
          });
        }
      }),
      const StudentScheduleTab(),
      const SizedBox.shrink(), // Center Face ID handled via modal/screen
      const StudentNotificationsTab(),
      const StudentProfileTab(),
    ];

    return Scaffold(
      body: IndexedStack(
        index: _currentIndex == 2 ? 0 : _currentIndex,
        children: pages,
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          color: Colors.white,
          border: Border(top: BorderSide(color: Color(0xFFE2E8F0), width: 1)),
        ),
        child: SafeArea(
          child: Padding(
            padding:
                const EdgeInsets.symmetric(horizontal: 10.0, vertical: 8.0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(0, Icons.home_outlined, Icons.home, 'Trang chủ'),
                _buildNavItem(1, Icons.calendar_today_outlined,
                    Icons.calendar_today, 'Lịch học'),
                _buildFaceScanNavItem(2, 'Điểm danh'),
                _buildNavItem(3, Icons.notifications_none_outlined,
                    Icons.notifications, 'Thông báo'),
                _buildNavItem(4, Icons.person_outline, Icons.person, 'Cá nhân'),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildNavItem(
      int index, IconData unselectedIcon, IconData selectedIcon, String label) {
    final isSelected = _currentIndex == index;
    final icon = Icon(
      isSelected ? selectedIcon : unselectedIcon,
      color: isSelected ? AppColors.primary : const Color(0xFF94A3B8),
      size: 24,
    );
    final notificationService = FcmNotificationService();

    return GestureDetector(
      onTap: () => _onTabTapped(index),
      behavior: HitTestBehavior.opaque,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (index == 3)
            AnimatedBuilder(
              animation: notificationService,
              builder: (context, _) => Stack(
                clipBehavior: Clip.none,
                children: [
                  icon,
                  if (notificationService.unreadCount > 0)
                    Positioned(
                      right: -8,
                      top: -5,
                      child: Container(
                        constraints:
                            const BoxConstraints(minWidth: 16, minHeight: 16),
                        padding: const EdgeInsets.symmetric(horizontal: 4),
                        decoration: const BoxDecoration(
                            color: AppColors.danger, shape: BoxShape.circle),
                        alignment: Alignment.center,
                        child: Text(
                          notificationService.unreadCount > 99
                              ? '99+'
                              : '${notificationService.unreadCount}',
                          style: const TextStyle(
                              color: Colors.white,
                              fontSize: 9,
                              fontWeight: FontWeight.w700),
                        ),
                      ),
                    ),
                ],
              ),
            )
          else
            icon,
          const SizedBox(height: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              color: isSelected ? AppColors.primary : const Color(0xFF94A3B8),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFaceScanNavItem(int index, String label) {
    final isSelected = _currentIndex == index;
    return GestureDetector(
      onTap: () => _onTabTapped(index),
      behavior: HitTestBehavior.opaque,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          FaceScanIcon(
            size: 24,
            color: isSelected ? AppColors.primary : const Color(0xFF94A3B8),
            strokeWidth: 2.2,
            cornerLength: 5,
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              color: isSelected ? AppColors.primary : const Color(0xFF94A3B8),
            ),
          ),
        ],
      ),
    );
  }
}
