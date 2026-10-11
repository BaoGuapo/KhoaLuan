import 'package:flutter/material.dart';

import 'presentation/auth/auth_controller.dart';
import 'presentation/auth/change_password_screen.dart';
import 'presentation/auth/login_screen.dart';
import 'presentation/home/home_screen.dart';
import 'presentation/shared/loading_screen.dart';

class QuanLySinhVienApp extends StatelessWidget {
  const QuanLySinhVienApp({
    required this.authController,
    super.key,
  });

  final AuthController authController;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Quản lý sinh viên HUIT',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
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
                : HomeScreen(controller: authController),
          };
        },
      ),
    );
  }

  ThemeData _buildTheme() {
    const primary = Color(0xFF0867B2);
    const secondary = Color(0xFF0B8F55);

    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: primary,
        primary: primary,
        secondary: secondary,
        surface: Colors.white,
      ),
      scaffoldBackgroundColor: const Color(0xFFF3F7FB),
      appBarTheme: const AppBarTheme(
        backgroundColor: primary,
        foregroundColor: Colors.white,
        centerTitle: false,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFD5E1EC)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFD5E1EC)),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 1,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
      ),
    );
  }
}
