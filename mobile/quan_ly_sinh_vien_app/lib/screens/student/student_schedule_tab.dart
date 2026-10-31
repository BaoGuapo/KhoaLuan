import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/token_storage.dart';
import '../../core/theme/app_colors.dart';

class StudentScheduleTab extends StatefulWidget {
  const StudentScheduleTab({super.key});

  @override
  State<StudentScheduleTab> createState() => _StudentScheduleTabState();
}

class _StudentScheduleTabState extends State<StudentScheduleTab> {
  static const _dayNames = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  final List<Map<String, dynamic>> _schedule = [];
  int _selectedDayIndex = (DateTime.now().weekday + 5) % 7;
  int _weekOffset = 0;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadSchedule();
  }

  Future<void> _loadSchedule() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final user = await TokenStorage.getUser();
      final token = await TokenStorage.getAccessToken();
      if (user == null ||
          token == null ||
          token.isEmpty ||
          token.startsWith('mock_')) {
        throw const _ScheduleException(
          'Đăng nhập bằng tài khoản máy chủ để tải lịch học.',
        );
      }
      final response = await http.get(
        Uri.parse(
          '${ApiConstants.baseUrl}/api/v1/sinh-vien/${Uri.encodeComponent(user.userName)}/lich-hoc',
        ),
        headers: {'Authorization': 'Bearer $token'},
      ).timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) {
        throw _ScheduleException(
          'Không tải được lịch học (mã lỗi ${response.statusCode}).',
        );
      }
      final decoded = jsonDecode(response.body);
      if (decoded is! List) {
        throw const _ScheduleException(
            'Dữ liệu lịch học từ máy chủ không hợp lệ.');
      }
      final rows = decoded.whereType<Map<String, dynamic>>().toList()
        ..sort((a, b) {
          final day = _asInt(a['thuTrongTuan']).compareTo(
            _asInt(b['thuTrongTuan']),
          );
          return day != 0
              ? day
              : _asInt(a['tietBatDau']).compareTo(_asInt(b['tietBatDau']));
        });
      if (!mounted) return;
      setState(() {
        _schedule
          ..clear()
          ..addAll(rows);
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _ScheduleException
            ? error.message
            : 'Không thể kết nối để tải lịch học.';
        _isLoading = false;
      });
    }
  }

  static int _asInt(Object? value) =>
      value is num ? value.toInt() : int.tryParse('$value') ?? 0;

  DateTime get _monday {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    return today
        .subtract(Duration(days: now.weekday - 1))
        .add(Duration(days: _weekOffset * 7));
  }

  String _formatDate(DateTime date) =>
      '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}';

  int get _selectedApiWeekday =>
      _selectedDayIndex == 6 ? 8 : _selectedDayIndex + 2;

  @override
  Widget build(BuildContext context) {
    final monday = _monday;
    final selectedDate = monday.add(Duration(days: _selectedDayIndex));
    final lessons = _schedule
        .where((row) => _asInt(row['thuTrongTuan']) == _selectedApiWeekday)
        .toList();

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Lịch học & Thời khóa biểu',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        elevation: 0,
        backgroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: _isLoading ? null : _loadSchedule,
            tooltip: 'Tải lại',
            icon: const Icon(Icons.refresh, color: AppColors.textPrimary),
          ),
        ],
      ),
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _buildError()
                : RefreshIndicator(
                    onRefresh: _loadSchedule,
                    child: ListView(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 20,
                        vertical: 16,
                      ),
                      children: [
                        _buildWeekSelector(monday),
                        const SizedBox(height: 16),
                        _buildDaySelector(monday),
                        const SizedBox(height: 22),
                        Text(
                          '${_dayNames[_selectedDayIndex]}, ${_formatDate(selectedDate)}',
                          style: const TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w800,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 12),
                        if (lessons.isEmpty)
                          _buildEmptyDay()
                        else
                          ...lessons.map(_buildClassCard),
                        const SizedBox(height: 18),
                        const Text(
                          'Lịch hiển thị theo thời khóa biểu tuần. Thông tin buổi học cụ thể và chức năng điểm danh cần được backend cung cấp.',
                          style: TextStyle(
                            fontSize: 12,
                            color: AppColors.textMuted,
                          ),
                        ),
                      ],
                    ),
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
                onPressed: _loadSchedule,
                icon: const Icon(Icons.refresh),
                label: const Text('Thử lại'),
              ),
            ],
          ),
        ),
      );

  Widget _buildWeekSelector(DateTime monday) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            IconButton(
              onPressed: () => setState(() => _weekOffset--),
              icon: const Icon(Icons.chevron_left),
            ),
            Text(
              'Tuần ${_formatDate(monday)} - ${_formatDate(monday.add(const Duration(days: 6)))}',
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            IconButton(
              onPressed:
                  _weekOffset == 0 ? null : () => setState(() => _weekOffset++),
              icon: const Icon(Icons.chevron_right),
            ),
          ],
        ),
      );

  Widget _buildDaySelector(DateTime monday) => Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: List.generate(7, (index) {
          final date = monday.add(Duration(days: index));
          final selected = index == _selectedDayIndex;
          return InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: () => setState(() => _selectedDayIndex = index),
            child: Container(
              width: 42,
              padding: const EdgeInsets.symmetric(vertical: 9),
              decoration: BoxDecoration(
                color: selected ? AppColors.primary : Colors.transparent,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  Text(
                    _dayNames[index],
                    style: TextStyle(
                      fontSize: 11,
                      color: selected ? Colors.white : AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${date.day}',
                    style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w800,
                      color: selected ? Colors.white : AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
          );
        }),
      );

  Widget _buildEmptyDay() => Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: const Text(
          'Không có lịch học được ghi nhận trong ngày này.',
          textAlign: TextAlign.center,
          style: TextStyle(color: AppColors.textSecondary),
        ),
      );

  Widget _buildClassCard(Map<String, dynamic> item) {
    final start = _asInt(item['tietBatDau']);
    final end = _asInt(item['tietKetThuc']);
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                start > 0 ? 'Tiết $start - $end' : 'Chưa có tiết học',
                style: const TextStyle(
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                item['phong']?.toString() ?? 'Chưa xếp phòng',
                style: const TextStyle(color: AppColors.textSecondary),
              ),
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
          Text(
            'Giảng viên: ${item['giangVien'] ?? 'Chưa cập nhật'}',
            style: const TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 4),
          Text(
            'Lớp học phần: ${item['maLopHocPhan'] ?? '—'}',
            style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
        ],
      ),
    );
  }
}

class _ScheduleException implements Exception {
  final String message;
  const _ScheduleException(this.message);
}
