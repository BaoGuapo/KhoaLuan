import 'package:flutter/material.dart';

import '../../core/student/student_portal_service.dart';
import '../../core/theme/app_colors.dart';

class StudentContactScreen extends StatefulWidget {
  const StudentContactScreen({super.key});

  @override
  State<StudentContactScreen> createState() => _StudentContactScreenState();
}

class _StudentContactScreenState extends State<StudentContactScreen> {
  final _service = StudentPortalService();
  final _phoneController = TextEditingController();
  final _addressController = TextEditingController();
  final _emergencyPhoneController = TextEditingController();
  StudentContact? _profile;
  String? _error;
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _phoneController.dispose();
    _addressController.dispose();
    _emergencyPhoneController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final profile = await _service.getProfile();
      if (!mounted) return;
      _profile = profile;
      _phoneController.text = profile.phone;
      _addressController.text = profile.address;
      _emergencyPhoneController.text = profile.emergencyPhone;
      setState(() => _loading = false);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is StudentPortalException
            ? error.message
            : 'Không tải được hồ sơ sinh viên.';
        _loading = false;
      });
    }
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      await _service.updateContact(
        phone: _phoneController.text,
        address: _addressController.text,
        emergencyPhone: _emergencyPhoneController.text,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đã lưu thông tin liên hệ.')),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            error is StudentPortalException
                ? error.message
                : 'Không lưu được thông tin liên hệ.',
          ),
        ),
      );
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final profile = _profile;
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Thông tin liên hệ chi tiết',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        actions: [
          IconButton(
            tooltip: 'Làm mới',
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh, color: AppColors.primary),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_error!, textAlign: TextAlign.center),
                        TextButton(
                            onPressed: _load, child: const Text('Thử lại')),
                      ],
                    ),
                  ),
                )
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(18),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _SectionCard(
                        title: 'Hồ sơ sinh viên',
                        children: [
                          _ReadOnlyLine('Mã sinh viên', profile?.studentCode),
                          _ReadOnlyLine('Họ và tên', profile?.fullName),
                          _ReadOnlyLine('Email trường', profile?.email),
                          _ReadOnlyLine('Ngày sinh', _birthDate(profile)),
                          _ReadOnlyLine('Lớp sinh hoạt', profile?.className),
                          _ReadOnlyLine('Ngành học', profile?.major),
                          _ReadOnlyLine('Khóa học', profile?.cohort),
                        ],
                      ),
                      const SizedBox(height: 14),
                      _SectionCard(
                        title: 'Thông tin có thể cập nhật',
                        children: [
                          _EditableField(
                            label: 'Số điện thoại',
                            controller: _phoneController,
                            keyboardType: TextInputType.phone,
                          ),
                          _EditableField(
                            label: 'Địa chỉ liên hệ',
                            controller: _addressController,
                            maxLines: 2,
                          ),
                          _EditableField(
                            label: 'Điện thoại liên hệ khẩn cấp',
                            controller: _emergencyPhoneController,
                            keyboardType: TextInputType.phone,
                          ),
                          const Text(
                            'Mã sinh viên, họ tên và email trường do nhà trường quản lý; liên hệ Phòng Đào tạo để điều chỉnh.',
                            style: TextStyle(
                              color: AppColors.textSecondary,
                              fontSize: 12,
                              height: 1.4,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 18),
                      SizedBox(
                        height: 50,
                        child: ElevatedButton(
                          onPressed: _saving ? null : _save,
                          child: _saving
                              ? const SizedBox(
                                  height: 20,
                                  width: 20,
                                  child:
                                      CircularProgressIndicator(strokeWidth: 2),
                                )
                              : const Text('Lưu thông tin liên hệ'),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }

  String _birthDate(StudentContact? profile) {
    final date = profile?.birthDate;
    if (date == null) return 'Chưa cập nhật';
    return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
  }
}

class _SectionCard extends StatelessWidget {
  const _SectionCard({required this.title, required this.children});
  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(title,
                style:
                    const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
            const Divider(height: 24),
            ...children,
          ],
        ),
      );
}

class _ReadOnlyLine extends StatelessWidget {
  const _ReadOnlyLine(this.label, this.value);
  final String label;
  final String? value;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 13),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 118,
              child: Text(label,
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.textSecondary)),
            ),
            Expanded(
              child: Text(
                value?.isNotEmpty == true ? value! : 'Chưa cập nhật',
                style: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textPrimary,
                    fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      );
}

class _EditableField extends StatelessWidget {
  const _EditableField({
    required this.label,
    required this.controller,
    this.keyboardType,
    this.maxLines = 1,
  });

  final String label;
  final TextEditingController controller;
  final TextInputType? keyboardType;
  final int maxLines;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 14),
        child: TextField(
          controller: controller,
          keyboardType: keyboardType,
          maxLines: maxLines,
          decoration: InputDecoration(
            labelText: label,
            border: const OutlineInputBorder(),
            isDense: true,
          ),
        ),
      );
}
