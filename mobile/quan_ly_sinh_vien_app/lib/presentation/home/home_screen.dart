import 'package:flutter/material.dart';

import '../../core/constants/role_names.dart';
import '../auth/auth_controller.dart';
import '../lecturer/lecturer_shell.dart';
import '../manager/manager_shell.dart';
import '../shared/account_tab.dart';
import '../shared/role_shell.dart';
import '../shared/ui.dart';
import '../student/student_shell.dart';

/// Điều hướng sau đăng nhập theo vai trò; tài khoản nhiều vai trò dùng vai trò có quyền cao nhất.
class HomeScreen extends StatelessWidget {
  const HomeScreen({required this.controller, super.key});

  final AuthController controller;

  @override
  Widget build(BuildContext context) {
    final roles = controller.session?.roles ?? const <String>[];

    if (roles.contains(RoleNames.admin) || roles.contains(RoleNames.canBoQuanLy)) {
      return ManagerShell(controller: controller);
    }
    if (roles.contains(RoleNames.giangVien)) {
      return LecturerShell(controller: controller);
    }
    if (roles.contains(RoleNames.sinhVien)) {
      return StudentShell(controller: controller);
    }
    return TabPage(
      title: 'Tài khoản',
      body: Column(
        children: [
          const Padding(
            padding: EdgeInsets.only(top: 24),
            child: MessageView(message: 'Tài khoản chưa được gán vai trò phù hợp.'),
          ),
          Expanded(child: AccountTab(controller: controller)),
        ],
      ),
    );
  }
}
