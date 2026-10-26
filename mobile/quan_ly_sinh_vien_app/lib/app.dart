import 'package:flutter/material.dart';

import 'core/constants/role_names.dart';
import 'core/theme/app_colors.dart';
import 'presentation/auth/auth_controller.dart';
import 'presentation/auth/change_password_screen.dart';
import 'presentation/auth/login_screen.dart';
import 'presentation/shared/loading_screen.dart';
import 'screens/admin/admin_main_scaffold.dart';
import 'screens/student/student_face_scan_screen.dart';
import 'screens/student/student_grades_tab.dart';
import 'screens/student/student_main_scaffold.dart';

class QuanLySinhVienApp extends StatelessWidget {
  const QuanLySinhVienApp({
    required this.authController,
    super.key,
  });

  final AuthController authController;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'EduFace - Quản lý sinh viên',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      routes: {
        '/face-scan': (_) => const StudentFaceScanScreen(),
        '/grades': (_) => const StudentGradesTab(),
        '/student': (_) => const StudentMainScaffold(),
        '/admin': (_) => const AdminMainScaffold(),
      },
      home: AnimatedBuilder(
        animation: authController,
        builder: (context, _) {
          return switch (authController.status) {
            AuthStatus.initializing => const LoadingScreen(),
            AuthStatus.unauthenticated => LoginScreen(
                controller: authController,
              ),
            AuthStatus.authenticated => authController.mustChangePassword
                ? ChangePasswordScreen(
                    controller: authController,
                    isRequired: true,
                  )
                : _buildHomeForRoles(authController),
          };
        },
      ),
    );
  }

  Widget _buildHomeForRoles(AuthController controller) {
    final roles = controller.session?.roles ?? const [];
    final isAdminOrLecturer = roles.contains(RoleNames.admin) ||
        roles.contains(RoleNames.giangVien) ||
        roles.contains(RoleNames.canBoQuanLy);

    if (isAdminOrLecturer) {
      return const AdminMainScaffold();
    }
    return const StudentMainScaffold();
  }

  ThemeData _buildTheme() {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppColors.primary,
        primary: AppColors.primary,
        secondary: const Color(0xFF10B981),
        surface: Colors.white,
      ),
      scaffoldBackgroundColor: const Color(0xFFF8FAFC),
      appBarTheme: const AppBarTheme(
        backgroundColor: Colors.white,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
        centerTitle: false,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.8),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          minimumSize: const Size.fromHeight(52),
          elevation: 0,
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
      cardTheme: CardTheme(
        color: Colors.white,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFF1F5F9)),
        ),
      ),
    );
  }
}
