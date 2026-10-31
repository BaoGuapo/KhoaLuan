import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/notifications/notification_model.dart';

void main() {
  group('AppNotificationModel.fromJson', () {
    test('parse thông báo lịch thi đầy đủ', () {
      final json = {
        'id': 'notif-001',
        'title': 'Lịch thi học kỳ 1',
        'body': 'Lịch thi môn Lập trình đã được cập nhật.',
        'category': 'lichThi',
        'detailContent': 'Phòng thi: E201, Ngày: 20/12/2026',
        'isRead': false,
        'createdAtUtc': '2026-10-25T08:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);

      expect(notif.id, 'notif-001');
      expect(notif.title, 'Lịch thi học kỳ 1');
      expect(notif.body, 'Lịch thi môn Lập trình đã được cập nhật.');
      expect(notif.category, NotificationCategory.lichThi);
      expect(notif.detailContent, 'Phòng thi: E201, Ngày: 20/12/2026');
      expect(notif.isRead, isFalse);
      expect(notif.timestamp, isNotNull);
    });

    test('isRead = true được parse đúng', () {
      final json = {
        'id': 'notif-002',
        'title': 'Đã đọc',
        'body': '...',
        'category': 'heThong',
        'isRead': true,
        'createdAtUtc': '2026-10-20T10:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.isRead, isTrue);
    });

    test('category không nhận ra → heThong', () {
      final json = {
        'id': 'notif-003',
        'title': 'Thông báo lạ',
        'body': '...',
        'category': 'loaiKhongTonTai',
        'isRead': false,
        'createdAtUtc': '2026-10-24T12:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.category, NotificationCategory.heThong);
    });

    test('detailContent null được parse an toàn', () {
      final json = {
        'id': 'notif-004',
        'title': 'Test',
        'body': 'Body',
        'category': 'canhBaoVang',
        'isRead': false,
        'createdAtUtc': '2026-10-24T00:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.detailContent, isNull);
      expect(notif.category, NotificationCategory.canhBaoVang);
    });

    test('parse lịch học', () {
      final json = {
        'id': 'n5',
        'title': 'Lịch học thay đổi',
        'body': '...',
        'category': 'lichHoc',
        'isRead': false,
        'createdAtUtc': '2026-10-23T00:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.category, NotificationCategory.lichHoc);
    });

    test('parse điểm danh', () {
      final json = {
        'id': 'n6',
        'title': 'Xác nhận điểm danh',
        'body': 'Bạn đã điểm danh thành công.',
        'category': 'diemDanh',
        'isRead': false,
        'createdAtUtc': '2026-10-25T07:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.category, NotificationCategory.diemDanh);
    });

    test('vangHoc → canhBaoVang (alias)', () {
      final json = {
        'id': 'n7',
        'title': 'Cảnh báo vắng học',
        'body': '...',
        'category': 'vangHoc',
        'isRead': false,
        'createdAtUtc': '2026-10-22T00:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.category, NotificationCategory.canhBaoVang);
    });

    test('title mặc định khi thiếu', () {
      final json = {
        'id': 'n8',
        'body': 'Nội dung',
        'category': 'heThong',
        'isRead': false,
        'createdAtUtc': '2026-10-25T00:00:00Z',
      };
      final notif = AppNotificationModel.fromJson(json);
      expect(notif.title, 'Thông báo mới');
    });

    test('createdAtUtc không hợp lệ → timestamp gần DateTime.now()', () {
      final before = DateTime.now();
      final json = {
        'id': 'n9',
        'title': 'Test',
        'body': '',
        'category': 'heThong',
        'isRead': false,
        'createdAtUtc': 'INVALID_DATE',
      };
      final notif = AppNotificationModel.fromJson(json);
      final after = DateTime.now();
      expect(
          notif.timestamp
              .isAfter(before.subtract(const Duration(seconds: 1))),
          isTrue);
      expect(notif.timestamp.isBefore(after.add(const Duration(seconds: 1))),
          isTrue);
    });
  });

  group('NotificationCategoryExtension.fromApiValue', () {
    test('ánh xạ đúng tất cả category hợp lệ', () {
      expect(NotificationCategoryExtension.fromApiValue('lichHoc'),
          NotificationCategory.lichHoc);
      expect(NotificationCategoryExtension.fromApiValue('lichThi'),
          NotificationCategory.lichThi);
      expect(NotificationCategoryExtension.fromApiValue('canhBaoVang'),
          NotificationCategory.canhBaoVang);
      expect(NotificationCategoryExtension.fromApiValue('vangHoc'),
          NotificationCategory.canhBaoVang);
      expect(NotificationCategoryExtension.fromApiValue('diemDanh'),
          NotificationCategory.diemDanh);
      expect(NotificationCategoryExtension.fromApiValue('heThong'),
          NotificationCategory.heThong);
    });

    test('giá trị không hợp lệ → heThong', () {
      expect(NotificationCategoryExtension.fromApiValue('unknown'),
          NotificationCategory.heThong);
      expect(
          NotificationCategoryExtension.fromApiValue(''), NotificationCategory.heThong);
      expect(NotificationCategoryExtension.fromApiValue(null),
          NotificationCategory.heThong);
    });
  });

  group('NotificationCategory.displayName', () {
    test('trả về tên hiển thị tiếng Việt', () {
      expect(NotificationCategory.lichHoc.displayName, 'Lịch học');
      expect(NotificationCategory.lichThi.displayName, 'Lịch thi');
      expect(NotificationCategory.canhBaoVang.displayName, 'Nhắc nhở vắng');
      expect(NotificationCategory.diemDanh.displayName, 'Điểm danh');
      expect(NotificationCategory.heThong.displayName, 'Hệ thống');
    });
  });

  group('AppNotificationModel.timeAgo', () {
    test('vừa xong khi < 1 phút', () {
      final notif = AppNotificationModel(
        id: '1',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: DateTime.now().subtract(const Duration(seconds: 30)),
      );
      expect(notif.timeAgo, 'Vừa xong');
    });

    test('X phút trước', () {
      final notif = AppNotificationModel(
        id: '2',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: DateTime.now().subtract(const Duration(minutes: 15)),
      );
      expect(notif.timeAgo, '15 phút trước');
    });

    test('X giờ trước', () {
      final notif = AppNotificationModel(
        id: '3',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: DateTime.now().subtract(const Duration(hours: 3)),
      );
      expect(notif.timeAgo, '3 giờ trước');
    });

    test('X ngày trước (< 7 ngày)', () {
      final notif = AppNotificationModel(
        id: '4',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: DateTime.now().subtract(const Duration(days: 2)),
      );
      expect(notif.timeAgo, '2 ngày trước');
    });

    test('>= 7 ngày hiển thị dạng dd/mm/yyyy', () {
      final timestamp = DateTime(2026, 9, 1);
      final notif = AppNotificationModel(
        id: '5',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: timestamp,
      );
      expect(notif.timeAgo, contains('1'));
      expect(notif.timeAgo, contains('9'));
      expect(notif.timeAgo, contains('2026'));
    });
  });

  group('AppNotificationModel.isRead mutation', () {
    test('có thể thay đổi isRead sau khi tạo', () {
      final notif = AppNotificationModel(
        id: '6',
        title: 'T',
        body: 'B',
        category: NotificationCategory.heThong,
        timestamp: DateTime.now(),
        isRead: false,
      );
      expect(notif.isRead, isFalse);
      notif.isRead = true;
      expect(notif.isRead, isTrue);
    });
  });
}
