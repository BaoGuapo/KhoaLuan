import 'package:flutter/material.dart';

enum NotificationCategory {
  lichHoc,
  lichThi,
  canhBaoVang,
  diemDanh,
  heThong,
}

extension NotificationCategoryExtension on NotificationCategory {
  static NotificationCategory fromApiValue(String? value) {
    switch (value) {
      case 'lichHoc':
        return NotificationCategory.lichHoc;
      case 'lichThi':
        return NotificationCategory.lichThi;
      case 'canhBaoVang':
      case 'vangHoc':
        return NotificationCategory.canhBaoVang;
      case 'diemDanh':
        return NotificationCategory.diemDanh;
      default:
        return NotificationCategory.heThong;
    }
  }

  String get displayName {
    switch (this) {
      case NotificationCategory.lichHoc:
        return 'Lịch học';
      case NotificationCategory.lichThi:
        return 'Lịch thi';
      case NotificationCategory.canhBaoVang:
        return 'Nhắc nhở vắng';
      case NotificationCategory.diemDanh:
        return 'Điểm danh';
      case NotificationCategory.heThong:
        return 'Hệ thống';
    }
  }

  IconData get icon {
    switch (this) {
      case NotificationCategory.lichHoc:
        return Icons.calendar_today_outlined;
      case NotificationCategory.lichThi:
        return Icons.assignment_outlined;
      case NotificationCategory.canhBaoVang:
        return Icons.warning_amber_rounded;
      case NotificationCategory.diemDanh:
        return Icons.check_circle_outline;
      case NotificationCategory.heThong:
        return Icons.notifications_none_outlined;
    }
  }

  Color get iconColor {
    switch (this) {
      case NotificationCategory.lichHoc:
        return const Color(0xFF3B82F6); // Blue
      case NotificationCategory.lichThi:
        return const Color(0xFF8B5CF6); // Purple
      case NotificationCategory.canhBaoVang:
        return const Color(0xFFEF4444); // Red
      case NotificationCategory.diemDanh:
        return const Color(0xFF10B981); // Green
      case NotificationCategory.heThong:
        return const Color(0xFFF59E0B); // Amber
    }
  }

  Color get iconBgColor {
    switch (this) {
      case NotificationCategory.lichHoc:
        return const Color(0xFFEFF6FF);
      case NotificationCategory.lichThi:
        return const Color(0xFFF5F3FF);
      case NotificationCategory.canhBaoVang:
        return const Color(0xFFFEE2E2);
      case NotificationCategory.diemDanh:
        return const Color(0xFFD1FAE5);
      case NotificationCategory.heThong:
        return const Color(0xFFFEF3C7);
    }
  }

  Color get bannerColor {
    switch (this) {
      case NotificationCategory.lichHoc:
        return const Color(0xFF2563EB);
      case NotificationCategory.lichThi:
        return const Color(0xFF7C3AED);
      case NotificationCategory.canhBaoVang:
        return const Color(0xFFDC2626);
      case NotificationCategory.diemDanh:
        return const Color(0xFF059669);
      case NotificationCategory.heThong:
        return const Color(0xFFD97706);
    }
  }
}

class AppNotificationModel {
  final String id;
  final String title;
  final String body;
  final NotificationCategory category;
  final DateTime timestamp;
  bool isRead;
  final String? detailContent;
  final Map<String, dynamic>? data;

  AppNotificationModel({
    required this.id,
    required this.title,
    required this.body,
    required this.category,
    required this.timestamp,
    this.isRead = false,
    this.detailContent,
    this.data,
  });

  factory AppNotificationModel.fromJson(Map<String, dynamic> json) {
    return AppNotificationModel(
      id: json['id']?.toString() ?? '',
      title: json['title']?.toString() ?? 'Thông báo mới',
      body: json['body']?.toString() ?? '',
      category: NotificationCategoryExtension.fromApiValue(
          json['category']?.toString()),
      timestamp: DateTime.tryParse(json['createdAtUtc']?.toString() ?? '')
              ?.toLocal() ??
          DateTime.now(),
      isRead: json['isRead'] == true,
      detailContent: json['detailContent']?.toString(),
    );
  }

  String get timeAgo {
    final now = DateTime.now();
    final difference = now.difference(timestamp);

    if (difference.inMinutes < 1) {
      return 'Vừa xong';
    } else if (difference.inMinutes < 60) {
      return '${difference.inMinutes} phút trước';
    } else if (difference.inHours < 24) {
      return '${difference.inHours} giờ trước';
    } else if (difference.inDays < 7) {
      return '${difference.inDays} ngày trước';
    } else {
      return '${timestamp.day}/${timestamp.month}/${timestamp.year}';
    }
  }
}
