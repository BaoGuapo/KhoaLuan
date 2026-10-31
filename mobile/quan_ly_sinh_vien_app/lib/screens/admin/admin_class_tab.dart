import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/token_storage.dart';
import '../../core/theme/app_colors.dart';

class AdminClassTab extends StatefulWidget {
  const AdminClassTab({super.key});

  @override
  State<AdminClassTab> createState() => _AdminClassTabState();
}

class _AdminClassTabState extends State<AdminClassTab> {
  final _searchController = TextEditingController();
  List<Map<String, dynamic>> _classes = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadClasses();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadClasses() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final token = await TokenStorage.getAccessToken();
      if (token == null || token.isEmpty || token.startsWith('mock_')) {
        throw const _ClassException('Đăng nhập máy chủ để xem lớp học phần.');
      }
      final response = await http.get(
        Uri.parse('${ApiConstants.baseUrl}/api/v1/lop-hoc-phan'),
        headers: {'Authorization': 'Bearer $token'},
      ).timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) {
        throw _ClassException(
          'Không tải được danh sách lớp (mã lỗi ${response.statusCode}).',
        );
      }
      final result = jsonDecode(response.body);
      if (result is! List) {
        throw const _ClassException('Dữ liệu lớp học phần không hợp lệ.');
      }
      if (!mounted) return;
      setState(() {
        _classes = result.whereType<Map<String, dynamic>>().toList();
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _ClassException
            ? error.message
            : 'Không thể kết nối để tải danh sách lớp.';
        _classes = [];
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final query = _searchController.text.trim().toLowerCase();
    final filtered = _classes.where((item) {
      return item['maLopHocPhan']?.toString().toLowerCase().contains(query) ==
              true ||
          item['monHoc']?.toString().toLowerCase().contains(query) == true ||
          item['giangVien']?.toString().toLowerCase().contains(query) == true;
    }).toList();

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Quản lý lớp học',
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
            onPressed: _isLoading ? null : _loadClasses,
            icon: const Icon(Icons.refresh, color: AppColors.textPrimary),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _searchController,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                hintText: 'Tìm mã lớp, môn học, giảng viên',
                prefixIcon: const Icon(Icons.search),
                filled: true,
                fillColor: Colors.white,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(14),
                  borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
                ),
              ),
            ),
          ),
          if (_isLoading) const LinearProgressIndicator(minHeight: 2),
          Expanded(
            child: _error != null
                ? _errorState()
                : _isLoading
                    ? const SizedBox.shrink()
                    : filtered.isEmpty
                        ? const Center(child: Text('Không có lớp học phần.'))
                        : RefreshIndicator(
                            onRefresh: _loadClasses,
                            child: ListView.builder(
                              padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
                              itemCount: filtered.length,
                              itemBuilder: (_, index) =>
                                  _classCard(filtered[index]),
                            ),
                          ),
          ),
        ],
      ),
    );
  }

  Widget _errorState() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: _loadClasses,
                icon: const Icon(Icons.refresh),
                label: const Text('Thử lại'),
              ),
            ],
          ),
        ),
      );

  Widget _classCard(Map<String, dynamic> item) {
    final enrolled = item['siSoHienTai'] ?? 0;
    final maximum = item['siSoToiDa'] ?? '—';
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  item['maLopHocPhan']?.toString() ?? '—',
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text('HK: ${item['maHocKy'] ?? '—'}'),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              item['monHoc']?.toString() ?? 'Môn học',
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 4),
            Text('Giảng viên: ${item['giangVien'] ?? 'Chưa cập nhật'}'),
            const SizedBox(height: 4),
            Text('Sĩ số: $enrolled / $maximum'),
            if (item['trangThai'] != null)
              Text('Trạng thái: ${item['trangThai']}'),
          ],
        ),
      ),
    );
  }
}

class _ClassException implements Exception {
  final String message;
  const _ClassException(this.message);
}
