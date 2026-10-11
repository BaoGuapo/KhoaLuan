import 'package:flutter/material.dart';

import '../../core/constants/role_names.dart';
import '../auth/auth_controller.dart';
import '../auth/change_password_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({required this.controller, super.key});

  final AuthController controller;

  @override
  Widget build(BuildContext context) {
    final session = controller.session!;
    final primaryRole = session.roles.isEmpty ? '' : session.roles.first;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Quản lý sinh viên'),
        actions: [
          IconButton(
            tooltip: 'Đổi mật khẩu',
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute<void>(
                builder: (_) => ChangePasswordScreen(
                  controller: controller,
                  isRequired: false,
                ),
              ),
            ),
            icon: const Icon(Icons.password),
          ),
          IconButton(
            tooltip: 'Đăng xuất',
            onPressed: controller.isBusy ? null : controller.logout,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 30,
                    backgroundColor: const Color(0xFFE3F2FD),
                    child: Text(
                      session.userName.substring(0, 1).toUpperCase(),
                      style: const TextStyle(
                        color: Color(0xFF0867B2),
                        fontSize: 24,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          session.userName,
                          style: Theme.of(context).textTheme.titleLarge?.copyWith(
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          session.roles.map(RoleNames.displayName).join(', '),
                          style: const TextStyle(color: Color(0xFF0B8F55)),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Chức năng tuần 6',
            style: Theme.of(context).textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
          ),
          const SizedBox(height: 12),
          _RoleDashboard(role: primaryRole),
        ],
      ),
    );
  }
}

class _RoleDashboard extends StatelessWidget {
  const _RoleDashboard({required this.role});

  final String role;

  @override
  Widget build(BuildContext context) {
    final data = switch (role) {
      RoleNames.admin => const (
          Icons.admin_panel_settings_outlined,
          'Khu vực quản trị',
          'Tài khoản có quyền quản trị hệ thống.',
        ),
      RoleNames.canBoQuanLy => const (
          Icons.manage_accounts_outlined,
          'Khu vực cán bộ quản lý',
          'Tài khoản có quyền quản lý hồ sơ và cấp tài khoản.',
        ),
      RoleNames.giangVien => const (
          Icons.co_present_outlined,
          'Khu vực giảng viên',
          'Các nghiệp vụ lớp học và điểm danh sẽ được triển khai ở tuần tiếp theo.',
        ),
      RoleNames.sinhVien => const (
          Icons.school_outlined,
          'Khu vực sinh viên',
          'Các nghiệp vụ lịch học và chuyên cần sẽ được triển khai ở tuần tiếp theo.',
        ),
      _ => const (
          Icons.person_outline,
          'Tài khoản người dùng',
          'Tài khoản chưa có vai trò phù hợp.',
        ),
    };

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(data.$1, size: 38, color: const Color(0xFF0867B2)),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    data.$2,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Text(data.$3),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
