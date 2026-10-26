import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/theme/app_colors.dart';

class AdminStudentTab extends StatefulWidget {
  const AdminStudentTab({super.key});

  @override
  State<AdminStudentTab> createState() => _AdminStudentTabState();
}

class _AdminStudentTabState extends State<AdminStudentTab> {
  final _searchController = TextEditingController();
  List<Map<String, dynamic>> _students = [];
  bool _isLoading = true;
  String? _error;
  final Set<String> _deletingFaceProfiles = {};

  @override
  void initState() {
    super.initState();
    _loadStudents();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadStudents() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final token = AuthProvider.instance.accessToken;
      if (token == null || token.isEmpty) {
        throw const _StudentLoadException(
          'Đăng nhập bằng tài khoản quản trị máy chủ để xem danh sách.',
        );
      }
      final query = _searchController.text.trim();
      final uri = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.managedFaceEnrollments}',
      ).replace(queryParameters: query.isEmpty ? null : {'search': query});
      final response = await http.get(uri, headers: {
        'Authorization': 'Bearer $token'
      }).timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) {
        final decoded = _tryDecode(response.body);
        throw _StudentLoadException(
          decoded?['message']?.toString() ??
              'Không tải được danh sách sinh viên (mã lỗi ${response.statusCode}).',
        );
      }
      final decoded = jsonDecode(response.body);
      if (decoded is! List) {
        throw const _StudentLoadException(
          'Dữ liệu sinh viên từ máy chủ không hợp lệ.',
        );
      }
      if (!mounted) return;
      setState(() {
        _students = decoded.whereType<Map<String, dynamic>>().toList();
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _StudentLoadException
            ? error.message
            : 'Không thể kết nối để tải danh sách sinh viên.';
        _students = [];
        _isLoading = false;
      });
    }
  }

  Map<String, dynamic>? _tryDecode(String body) {
    try {
      final decoded = jsonDecode(body);
      return decoded is Map<String, dynamic> ? decoded : null;
    } on FormatException {
      return null;
    }
  }

  Future<void> _search(String _) => _loadStudents();

  Future<void> _deleteFaceEnrollment(Map<String, dynamic> student) async {
    final code = student['maSinhVien']?.toString();
    if (code == null || code.isEmpty || _deletingFaceProfiles.contains(code)) {
      return;
    }

    final name = student['hoTen']?.toString() ?? code;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa dữ liệu FaceID?'),
        content: Text(
          'Xóa ảnh mẫu và vector khuôn mặt của $name ($code). '
          'Sinh viên cần đăng ký FaceID lại trước khi điểm danh. '
          'Lịch sử điểm danh hiện có không bị xóa.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            child: const Text('Xóa FaceID'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _deletingFaceProfiles.add(code));
    try {
      final token = AuthProvider.instance.accessToken;
      if (token == null || token.isEmpty) {
        throw const _StudentLoadException(
          'Đăng nhập bằng tài khoản Admin để xóa dữ liệu FaceID.',
        );
      }
      final response = await http.delete(
        Uri.parse(
          '${ApiConstants.baseUrl}'
          '${ApiConstants.deleteStudentFaceEnrollment(code)}',
        ),
        headers: {'Authorization': 'Bearer $token'},
      ).timeout(const Duration(seconds: 10));

      if (response.statusCode != 204) {
        final decoded = _tryDecode(response.body);
        throw _StudentLoadException(
          decoded?['message']?.toString() ??
              'Không xóa được FaceID (mã lỗi ${response.statusCode}).',
        );
      }

      if (!mounted) return;
      setState(() {
        _students = _students.map((item) {
          if (item['maSinhVien']?.toString() != code) return item;
          return {
            ...item,
            'daDangKyKhuonMat': false,
            'thoiGianDangKyKhuonMat': null,
          };
        }).toList();
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Đã xóa dữ liệu FaceID của $name.')),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            error is _StudentLoadException
                ? error.message
                : 'Không thể kết nối để xóa dữ liệu FaceID.',
          ),
          backgroundColor: AppColors.danger,
        ),
      );
    } finally {
      if (mounted) setState(() => _deletingFaceProfiles.remove(code));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Quản lý sinh viên',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        actions: [
          IconButton(
            onPressed: _isLoading ? null : _loadStudents,
            icon: const Icon(Icons.refresh, color: AppColors.textPrimary),
            tooltip: 'Tải lại',
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
              child: TextField(
                controller: _searchController,
                textInputAction: TextInputAction.search,
                onSubmitted: _search,
                decoration: InputDecoration(
                  hintText: 'Tìm kiếm sinh viên, MSSV...',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: IconButton(
                    onPressed: _loadStudents,
                    icon: const Icon(Icons.arrow_forward),
                  ),
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
                ),
              ),
            ),
            Expanded(
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? _buildError()
                      : _students.isEmpty
                          ? const Center(
                              child: Text('Không tìm thấy sinh viên.'),
                            )
                          : RefreshIndicator(
                              onRefresh: _loadStudents,
                              child: ListView.builder(
                                padding:
                                    const EdgeInsets.fromLTRB(20, 4, 20, 20),
                                itemCount: _students.length,
                                itemBuilder: (context, index) =>
                                    _buildStudentCard(_students[index]),
                              ),
                            ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildError() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.cloud_off, size: 44, color: AppColors.textMuted),
              const SizedBox(height: 12),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 14),
              OutlinedButton.icon(
                onPressed: _loadStudents,
                icon: const Icon(Icons.refresh),
                label: const Text('Thử lại'),
              ),
            ],
          ),
        ),
      );

  Widget _buildStudentCard(Map<String, dynamic> student) {
    final name = student['hoTen']?.toString() ?? 'Chưa cập nhật họ tên';
    final code = student['maSinhVien']?.toString() ?? '—';
    final email = student['email']?.toString() ?? 'Chưa có email';
    final active = student['trangThaiTaiKhoan'] == true;
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Row(
        children: [
          CircleAvatar(
            backgroundColor: const Color(0xFFEFF6FF),
            child: Text(
              name.isEmpty ? '?' : name.substring(0, 1).toUpperCase(),
              style: const TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  name,
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 4),
                Text('MSSV: $code'),
                Text(email, maxLines: 1, overflow: TextOverflow.ellipsis),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                active ? 'Hoạt động' : 'Khóa',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: active ? AppColors.success : AppColors.danger,
                ),
              ),
              const SizedBox(height: 6),
              _buildFaceEnrollmentStatus(student),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildFaceEnrollmentStatus(Map<String, dynamic> student) {
    final enrolled = student['daDangKyKhuonMat'] == true;
    final code = student['maSinhVien']?.toString() ?? '';
    final deleting = _deletingFaceProfiles.contains(code);
    final registeredAt =
        DateTime.tryParse(student['thoiGianDangKyKhuonMat']?.toString() ?? '');
    final dateLabel = registeredAt == null
        ? null
        : '${registeredAt.day.toString().padLeft(2, '0')}/'
            '${registeredAt.month.toString().padLeft(2, '0')}/'
            '${registeredAt.year}';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: enrolled ? const Color(0xFFD1FAE5) : const Color(0xFFF1F5F9),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Text(
            enrolled ? 'Đã đăng ký FaceID' : 'Chưa đăng ký FaceID',
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w700,
              color: enrolled ? AppColors.success : AppColors.textSecondary,
            ),
          ),
        ),
        if (dateLabel != null) ...[
          const SizedBox(height: 3),
          Text(
            dateLabel,
            style: const TextStyle(
              fontSize: 10,
              color: AppColors.textSecondary,
            ),
          ),
        ],
        if (enrolled)
          SizedBox(
            height: 32,
            child: TextButton(
              onPressed: deleting ? null : () => _deleteFaceEnrollment(student),
              style: TextButton.styleFrom(
                foregroundColor: AppColors.danger,
                padding: const EdgeInsets.symmetric(horizontal: 4),
                minimumSize: Size.zero,
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
              ),
              child: deleting
                  ? const SizedBox(
                      width: 14,
                      height: 14,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text(
                      'Xóa FaceID',
                      style: TextStyle(fontSize: 11),
                    ),
            ),
          ),
      ],
    );
  }
}

class _StudentLoadException implements Exception {
  final String message;
  const _StudentLoadException(this.message);
}
