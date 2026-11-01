import 'package:flutter/material.dart';

import '../../core/notifications/fcm_notification_service.dart';
import '../../core/notifications/notification_model.dart';
import '../../core/theme/app_colors.dart';
import 'notification_detail_screen.dart';

class StudentNotificationsTab extends StatefulWidget {
  const StudentNotificationsTab({super.key});

  @override
  State<StudentNotificationsTab> createState() =>
      _StudentNotificationsTabState();
}

class _StudentNotificationsTabState extends State<StudentNotificationsTab>
    with SingleTickerProviderStateMixin {
  NotificationCategory? _selectedCategory;
  late final AnimationController _fabAnimCtrl;

  @override
  void initState() {
    super.initState();
    _fabAnimCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 300),
    );
    FcmNotificationService().loadNotifications();
  }

  @override
  void dispose() {
    _fabAnimCtrl.dispose();
    super.dispose();
  }

  // ─── Build ──────────────────────────────────────────────────────────────────
  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: FcmNotificationService(),
      builder: (context, _) {
        final service = FcmNotificationService();
        final all = service.notifications;
        final filtered = _selectedCategory == null
            ? all
            : all.where((n) => n.category == _selectedCategory).toList();

        return Scaffold(
          backgroundColor: const Color(0xFFF8FAFC),
          appBar: _buildAppBar(service),
          body: Column(
            children: [
              _FilterBar(
                selectedCategory: _selectedCategory,
                onChanged: (cat) => setState(() => _selectedCategory = cat),
                allCount: all.length,
                unreadCount: service.unreadCount,
              ),
              if (service.isLoading)
                const LinearProgressIndicator(
                    minHeight: 2, color: AppColors.primary),
              if (service.loadError != null && !service.isLoading)
                MaterialBanner(
                  content: Text(service.loadError!),
                  leading: const Icon(Icons.cloud_off),
                  actions: [
                    TextButton(
                      onPressed: service.loadNotifications,
                      child: const Text('Thử lại'),
                    ),
                  ],
                ),
              Expanded(
                child: RefreshIndicator(
                  onRefresh: service.loadNotifications,
                  color: AppColors.primary,
                  child: filtered.isEmpty
                      ? _buildEmpty(
                          service.isLoading,
                          service.loadError,
                        )
                      : _buildList(filtered),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  // ─── AppBar ─────────────────────────────────────────────────────────────────
  PreferredSizeWidget _buildAppBar(FcmNotificationService service) {
    return AppBar(
      backgroundColor: Colors.white,
      elevation: 0,
      titleSpacing: 20,
      title: Row(
        children: [
          const Text(
            'Thông báo',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
          if (service.unreadCount > 0) ...[
            const SizedBox(width: 8),
            _UnreadBadge(count: service.unreadCount),
          ],
        ],
      ),
      actions: [
        // Refresh
        IconButton(
          icon:
              const Icon(Icons.refresh_rounded, color: AppColors.textSecondary),
          tooltip: 'Làm mới',
          onPressed: service.loadNotifications,
        ),
        // Mark all read
        IconButton(
          icon: Icon(
            Icons.done_all_rounded,
            color: service.unreadCount == 0
                ? AppColors.textMuted
                : AppColors.primary,
          ),
          tooltip: 'Đánh dấu đã đọc tất cả',
          onPressed: service.unreadCount == 0
              ? null
              : () async {
                  final succeeded = await service.markAllAsRead();
                  if (!mounted) return;
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(
                        succeeded
                            ? 'Đã đánh dấu tất cả là đã đọc'
                            : 'Không lưu được trạng thái đã đọc',
                      ),
                      behavior: SnackBarBehavior.floating,
                      duration: const Duration(seconds: 2),
                    ),
                  );
                },
        ),
        const SizedBox(width: 4),
      ],
    );
  }

  // ─── List ────────────────────────────────────────────────────────────────────
  Widget _buildList(List<AppNotificationModel> items) {
    // Group by date
    final groups = _groupByDate(items);
    final keys = groups.keys.toList();

    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
      itemCount: keys.length,
      itemBuilder: (context, i) {
        final label = keys[i];
        final groupItems = groups[label]!;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _DateSeparator(label: label),
            ...groupItems.asMap().entries.map((entry) {
              final item = entry.value;
              return Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: _NotificationCard(
                  item: item,
                  onTap: () => _openDetail(item),
                  onDelete: () {
                    FcmNotificationService().deleteNotification(item.id);
                  },
                ),
              );
            }),
          ],
        );
      },
    );
  }

  // ─── Empty ───────────────────────────────────────────────────────────────────
  Widget _buildEmpty(bool isLoading, String? loadError) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        SizedBox(
          height: MediaQuery.sizeOf(context).height * 0.6,
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 80,
                  height: 80,
                  decoration: BoxDecoration(
                    color: const Color(0xFFF1F5F9),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.notifications_off_outlined,
                    size: 36,
                    color: Color(0xFFCBD5E1),
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  isLoading
                      ? 'Đang tải thông báo…'
                      : loadError != null
                          ? 'Không tải được thông báo'
                          : 'Chưa có thông báo',
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textMuted,
                  ),
                ),
                if (!isLoading && loadError == null) ...[
                  const SizedBox(height: 6),
                  const Text(
                    'Kéo xuống để làm mới',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textMuted,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }

  // ─── Navigation ──────────────────────────────────────────────────────────────
  void _openDetail(AppNotificationModel item) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NotificationDetailScreen(item: item),
      ),
    );
  }

  // ─── Group by date ────────────────────────────────────────────────────────────
  Map<String, List<AppNotificationModel>> _groupByDate(
      List<AppNotificationModel> items) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final yesterday = today.subtract(const Duration(days: 1));

    final result = <String, List<AppNotificationModel>>{};
    for (final item in items) {
      final d = DateTime(
          item.timestamp.year, item.timestamp.month, item.timestamp.day);
      String label;
      if (d == today) {
        label = 'Hôm nay';
      } else if (d == yesterday) {
        label = 'Hôm qua';
      } else {
        label =
            '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
      }
      (result[label] ??= []).add(item);
    }
    return result;
  }
}

// ─── Sub-widgets ──────────────────────────────────────────────────────────────

class _UnreadBadge extends StatelessWidget {
  const _UnreadBadge({required this.count});
  final int count;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minWidth: 22, minHeight: 22),
      alignment: Alignment.center,
      padding: const EdgeInsets.symmetric(horizontal: 6),
      decoration:
          const BoxDecoration(color: AppColors.danger, shape: BoxShape.circle),
      child: Text(
        count > 99 ? '99+' : '$count',
        style: const TextStyle(
            color: Colors.white, fontSize: 11, fontWeight: FontWeight.w700),
      ),
    );
  }
}

// ─── Filter chip bar ──────────────────────────────────────────────────────────

class _FilterBar extends StatelessWidget {
  const _FilterBar({
    required this.selectedCategory,
    required this.onChanged,
    required this.allCount,
    required this.unreadCount,
  });

  final NotificationCategory? selectedCategory;
  final ValueChanged<NotificationCategory?> onChanged;
  final int allCount;
  final int unreadCount;

  static const _filters =
      <({NotificationCategory? category, String label, IconData icon})>[
    (category: null, label: 'Tất cả', icon: Icons.inbox_outlined),
    (
      category: NotificationCategory.lichHoc,
      label: 'Lịch học',
      icon: Icons.calendar_today_outlined
    ),
    (
      category: NotificationCategory.lichThi,
      label: 'Lịch thi',
      icon: Icons.assignment_outlined
    ),
    (
      category: NotificationCategory.canhBaoVang,
      label: 'Vắng học',
      icon: Icons.warning_amber_rounded
    ),
    (
      category: NotificationCategory.diemDanh,
      label: 'Điểm danh',
      icon: Icons.check_circle_outline
    ),
    (
      category: NotificationCategory.heThong,
      label: 'Hệ thống',
      icon: Icons.settings_outlined
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: _filters.map((f) {
            final isSelected = selectedCategory == f.category;
            final color = f.category?.iconColor ?? AppColors.primary;
            return Padding(
              padding: const EdgeInsets.only(right: 8),
              child: FilterChip(
                avatar: Icon(
                  f.icon,
                  size: 14,
                  color: isSelected ? Colors.white : color,
                ),
                label: Text(f.label),
                selected: isSelected,
                onSelected: (_) => onChanged(f.category),
                selectedColor: color,
                backgroundColor: Colors.white,
                side: BorderSide(
                  color: isSelected ? color : AppColors.border,
                ),
                labelStyle: TextStyle(
                  fontSize: 12,
                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                  color: isSelected ? Colors.white : AppColors.textSecondary,
                ),
                visualDensity: VisualDensity.compact,
                showCheckmark: false,
                padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 0),
              ),
            );
          }).toList(),
        ),
      ),
    );
  }
}

// ─── Date separator ───────────────────────────────────────────────────────────

class _DateSeparator extends StatelessWidget {
  const _DateSeparator({required this.label});
  final String label;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 12, bottom: 8),
      child: Row(
        children: [
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: AppColors.textMuted,
              letterSpacing: 0.4,
            ),
          ),
          const SizedBox(width: 8),
          const Expanded(child: Divider(color: AppColors.border, height: 1)),
        ],
      ),
    );
  }
}

// ─── Notification card ────────────────────────────────────────────────────────

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({
    required this.item,
    required this.onTap,
    required this.onDelete,
  });

  final AppNotificationModel item;
  final VoidCallback onTap;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Dismissible(
      key: ValueKey(item.id),
      direction: DismissDirection.endToStart,
      onDismissed: (_) => onDelete(),
      confirmDismiss: (_) async {
        return await showDialog<bool>(
              context: context,
              builder: (ctx) => AlertDialog(
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(18)),
                title: const Text('Xóa thông báo?',
                    style:
                        TextStyle(fontWeight: FontWeight.w700, fontSize: 17)),
                content: const Text(
                  'Thông báo này sẽ bị xóa vĩnh viễn.',
                  style: TextStyle(color: AppColors.textSecondary, height: 1.4),
                ),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(ctx, false),
                    child: const Text('Hủy'),
                  ),
                  ElevatedButton(
                    onPressed: () => Navigator.pop(ctx, true),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.danger,
                      foregroundColor: Colors.white,
                      elevation: 0,
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10)),
                    ),
                    child: const Text('Xóa'),
                  ),
                ],
              ),
            ) ??
            false;
      },
      background: Container(
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 20),
        decoration: BoxDecoration(
          color: AppColors.danger,
          borderRadius: BorderRadius.circular(16),
        ),
        child: const Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.delete_outline_rounded, color: Colors.white, size: 24),
            SizedBox(height: 4),
            Text('Xóa',
                style: TextStyle(
                    color: Colors.white,
                    fontSize: 11,
                    fontWeight: FontWeight.w700)),
          ],
        ),
      ),
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            color: item.isRead ? Colors.white : const Color(0xFFF0F7FF),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: item.isRead
                  ? AppColors.border
                  : AppColors.primary.withOpacity(0.25),
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.03),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Category icon
                _CategoryIcon(category: item.category, size: 44),
                const SizedBox(width: 12),
                // Content
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Title row
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Text(
                              item.title,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: item.isRead
                                    ? FontWeight.w600
                                    : FontWeight.w800,
                                color: AppColors.textPrimary,
                                height: 1.3,
                              ),
                            ),
                          ),
                          if (!item.isRead) ...[
                            const SizedBox(width: 8),
                            const Icon(Icons.circle,
                                color: AppColors.primary, size: 8),
                          ],
                        ],
                      ),
                      const SizedBox(height: 5),
                      // Body
                      Text(
                        item.body,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 13,
                          height: 1.4,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 8),
                      // Footer: category chip + time
                      Row(
                        children: [
                          _MiniCategoryChip(category: item.category),
                          const Spacer(),
                          Text(
                            item.timeAgo,
                            style: const TextStyle(
                                fontSize: 11, color: AppColors.textMuted),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ─── Category icon circle ─────────────────────────────────────────────────────

class _CategoryIcon extends StatelessWidget {
  const _CategoryIcon({required this.category, required this.size});

  final NotificationCategory category;
  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: category.iconBgColor,
        shape: BoxShape.circle,
      ),
      child: Icon(category.icon, color: category.iconColor, size: size / 2),
    );
  }
}

// ─── Mini category chip (inside card footer) ──────────────────────────────────

class _MiniCategoryChip extends StatelessWidget {
  const _MiniCategoryChip({required this.category});
  final NotificationCategory category;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: category.iconBgColor,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        category.displayName,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w700,
          color: category.iconColor,
        ),
      ),
    );
  }
}
