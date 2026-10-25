import 'package:flutter/material.dart';
import '../../core/auth/auth_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/widgets/custom_text_field.dart';
import '../../core/widgets/primary_button.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _handleChangePassword() async {
    if (!_formKey.currentState!.validate()) return;

    if (_newPasswordController.text != _confirmPasswordController.text) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mật khẩu mới không trùng khớp')),
      );
      return;
    }

    setState(() {
      _isLoading = true;
    });

    final success = await AuthService().changePassword(
      _currentPasswordController.text,
      _newPasswordController.text,
    );

    if (!mounted) return;

    setState(() {
      _isLoading = false;
    });

    if (success) {
      final authService = AuthService();
      final mustChangePassword = authService.mustChangePassword;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: AppColors.success,
          content: Text('Đổi mật khẩu thành công!'),
        ),
      );
      if (mustChangePassword) {
        final homeRoute = authService.isLecturerOrAdmin ? '/admin' : '/student';
        Navigator.pushNamedAndRemoveUntil(
          context,
          homeRoute,
          (route) => false,
        );
      } else {
        Navigator.pop(context);
      }
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppColors.danger,
          content: Text(
            AuthService().errorMessage ??
                'Đổi mật khẩu thất bại. Vui lòng kiểm tra kết nối và thử lại.',
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textPrimary),
          onPressed: AuthService().mustChangePassword
              ? null
              : () => Navigator.pop(context),
        ),
        title: const Text(
          'Thay đổi mật khẩu',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 18,
            fontWeight: FontWeight.w700,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CustomTextField(
                  label: 'Mật khẩu hiện tại',
                  hint: 'Nhập mật khẩu hiện tại',
                  controller: _currentPasswordController,
                  isPassword: true,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Vui lòng nhập mật khẩu hiện tại'
                      : null,
                ),
                const SizedBox(height: 18),
                CustomTextField(
                  label: 'Mật khẩu mới',
                  hint: 'Nhập mật khẩu mới (tối thiểu 8 ký tự)',
                  controller: _newPasswordController,
                  isPassword: true,
                  validator: (val) => val == null || val.length < 8
                      ? 'Mật khẩu mới tối thiểu 8 ký tự'
                      : null,
                ),
                const SizedBox(height: 18),
                CustomTextField(
                  label: 'Xác nhận mật khẩu mới',
                  hint: 'Nhập lại mật khẩu mới',
                  controller: _confirmPasswordController,
                  isPassword: true,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Vui lòng xác nhận mật khẩu mới'
                      : null,
                ),
                const SizedBox(height: 32),
                PrimaryButton(
                  text: 'Cập nhật mật khẩu',
                  isLoading: _isLoading,
                  onPressed: _handleChangePassword,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
