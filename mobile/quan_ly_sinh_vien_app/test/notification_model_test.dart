import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/notifications/notification_model.dart';

void main() {
  group('AppNotificationModel', () {
    test('parses persisted notification fields and category', () {
      final notification = AppNotificationModel.fromJson({
        'id': 'notification-1',
        'title': 'Lịch thi',
        'body': 'Thi lúc 08:30',
        'category': 'lichThi',
        'detailContent': 'Phòng E301',
        'isRead': false,
        'createdAtUtc': '2026-09-27T10:00:00Z',
      });

      expect(notification.id, 'notification-1');
      expect(notification.category, NotificationCategory.lichThi);
      expect(notification.detailContent, 'Phòng E301');
      expect(notification.isRead, isFalse);
      expect(notification.timestamp.isUtc, isFalse);
    });

    test('maps absence reminders and unknown categories safely', () {
      expect(
        NotificationCategoryExtension.fromApiValue('canhBaoVang'),
        NotificationCategory.canhBaoVang,
      );
      expect(
        NotificationCategoryExtension.fromApiValue('unknown'),
        NotificationCategory.heThong,
      );
    });
  });
}
