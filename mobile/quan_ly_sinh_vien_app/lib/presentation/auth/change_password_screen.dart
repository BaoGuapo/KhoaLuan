import 'package:flutter/material.dart';

import 'auth_controller.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({
    required this.controller,
    required this.isRequired,
    super.key,
  });

  final AuthController controller;
  final bool isRequired;

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscureCurrent = true;
  bool _obscureNew = true;

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;

    final changed = await widget.controller.changePassword(
      currentPassword: _currentPasswordController.text,
      newPassword: _newPasswordController.text,
    );
    if (changed && !widget.isRequired && mounted) {
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Đổi mật khẩu'),
        automaticallyImplyLeading: !widget.isRequired,
        actions: [
          TextButton.icon(
            onPressed: controller.isBusy ? null : controller.logout,
            style: TextButton.styleFrom(foregroundColor: Colors.white),
            icon: const Icon(Icons.logout),
            label: const Text('Đăng xuất'),
          ),
        ],
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 520),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Icon(
                        Icons.password_rounded,
                        size: 56,
                        color: Color(0xFF0867B2),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        widget.isRequired
                            ? 'Thiết lập mật khẩu mới'
                            : 'Đổi mật khẩu',
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                              fontWeight: FontWeight.w700,
                            ),
                      ),
                      if (widget.isRequired) ...[
                        const SizedBox(height: 12),
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: const Color(0xFFE8F4FD),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: Text(
                            controller.noticeMessage ??
                                'Bạn phải đổi mật khẩu tạm trước khi sử dụng ứng dụng.',
                          ),
                        ),
                      ],
                      if (controller.errorMessage != null) ...[
                        const SizedBox(height: 12),
                        Text(
                          controller.errorMessage!,
                          style: TextStyle(color: Colors.red.shade700),
                        ),
                      ],
                      const SizedBox(height: 24),
                      TextFormField(
                        controller: _currentPasswordController,
                        enabled: !controller.isBusy,
                        obscureText: _obscureCurrent,
                        autofillHints: const [AutofillHints.password],
                        decoration: InputDecoration(
                          labelText: 'Mật khẩu hiện tại',
                          prefixIcon: const Icon(Icons.lock_outline),
                          suffixIcon: IconButton(
                            onPressed: () => setState(
                              () => _obscureCurrent = !_obscureCurrent,
                            ),
                            icon: Icon(
                              _obscureCurrent
                                  ? Icons.visibility_outlined
                                  : Icons.visibility_off_outlined,
                            ),
                          ),
                        ),
                        validator: (value) => value == null || value.isEmpty
                            ? 'Vui lòng nhập mật khẩu hiện tại.'
                            : null,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _newPasswordController,
                        enabled: !controller.isBusy,
                        obscureText: _obscureNew,
                        autofillHints: const [AutofillHints.newPassword],
                        decoration: InputDecoration(
                          labelText: 'Mật khẩu mới',
                          helperText: 'Tối thiểu 8 ký tự.',
                          prefixIcon: const Icon(Icons.key_outlined),
                          suffixIcon: IconButton(
                            onPressed: () => setState(
                              () => _obscureNew = !_obscureNew,
                            ),
                            icon: Icon(
                              _obscureNew
                                  ? Icons.visibility_outlined
                                  : Icons.visibility_off_outlined,
                            ),
                          ),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Vui lòng nhập mật khẩu mới.';
                          }
                          if (value.length < 8) {
                            return 'Mật khẩu mới phải có ít nhất 8 ký tự.';
                          }
                          if (value == _currentPasswordController.text) {
                            return 'Mật khẩu mới phải khác mật khẩu hiện tại.';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _confirmPasswordController,
                        enabled: !controller.isBusy,
                        obscureText: true,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.newPassword],
                        onFieldSubmitted: (_) => _submit(),
                        decoration: const InputDecoration(
                          labelText: 'Xác nhận mật khẩu mới',
                          prefixIcon: Icon(Icons.verified_user_outlined),
                        ),
                        validator: (value) => value != _newPasswordController.text
                            ? 'Mật khẩu xác nhận không khớp.'
                            : null,
                      ),
                      const SizedBox(height: 24),
                      ElevatedButton.icon(
                        onPressed: controller.isBusy ? null : _submit,
                        icon: controller.isBusy
                            ? const SizedBox.square(
                                dimension: 20,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.save_outlined),
                        label: Text(
                          controller.isBusy ? 'Đang lưu...' : 'Lưu mật khẩu mới',
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
