import 'package:flutter/material.dart';

import '../../core/auth/auth_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/auth/user_model.dart';
import 'admin_curriculum_tab.dart';

class AdminDashboardTab extends StatelessWidget {
  final Function(int)? onNavigateTab;

  const AdminDashboardTab({super.key, this.onNavigateTab});

  @override
  Widget build(BuildContext context) {
    final user = AuthService().currentUser;
    final isLecturer = user?.role.name.contains('giangVien') ?? false;
    final canManageFees =
        user?.role == UserRole.admin || user?.role == UserRole.canBoQuanLy;

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 54, 20, 24),
          children: [
            Text(
              isLecturer ? 'Cổng Giảng Viên' : 'Hệ thống quản trị',
              style: const TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              user?.fullName.isNotEmpty == true
                  ? user!.fullName
                  : 'Tài khoản quản trị',
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFFFFBEB),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: const Color(0xFFFDE68A)),
              ),
              child: const Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline, color: Color(0xFFB45309)),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Tổng hợp dashboard chưa có dữ liệu. Các số liệu mẫu đã được gỡ để tránh hiển thị sai.',
                      style: TextStyle(
                        color: Color(0xFF92400E),
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            const Text(
              'Chức năng hiện có',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 10),
            _ActionCard(
              icon: Icons.people_outline,
              title: 'Danh sách sinh viên',
              subtitle: 'Tải dữ liệu từ máy chủ, tìm theo tên hoặc MSSV',
              onTap: () => onNavigateTab?.call(1),
            ),
            _ActionCard(
              icon: Icons.fact_check_outlined,
              title: 'Quản lý điểm danh',
              subtitle: 'Màn hình cần API chọn buổi học và danh sách lớp',
              onTap: () => onNavigateTab?.call(2),
            ),
            _ActionCard(
              icon: Icons.menu_book_outlined,
              title: 'Quản lý lớp học',
              subtitle: 'Tra cứu lớp học phần và danh sách môn',
              onTap: () => onNavigateTab?.call(3),
            ),
            if (canManageFees)
              _ActionCard(
                icon: Icons.account_tree_outlined,
                title: 'CTĐT & mở lớp học phần',
                subtitle: 'Nhập môn, tín chỉ, số nhóm và phân công giảng viên',
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => const AdminCurriculumTab(),
                  ),
                ),
              ),
            if (canManageFees)
              _ActionCard(
                icon: Icons.payments_outlined,
                title: 'Quản lý học phí',
                subtitle: 'Tra cứu và cập nhật khoản thu theo sinh viên',
                onTap: () => onNavigateTab?.call(4),
              ),
          ],
        ),
      ),
    );
  }
}

class _ActionCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _ActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) => Card(
        margin: const EdgeInsets.only(bottom: 10),
        color: Colors.white,
        child: ListTile(
          onTap: onTap,
          leading: Icon(icon, color: AppColors.primary),
          title: Text(
            title,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
          subtitle: Text(subtitle),
          trailing: const Icon(Icons.chevron_right),
        ),
      );
}
