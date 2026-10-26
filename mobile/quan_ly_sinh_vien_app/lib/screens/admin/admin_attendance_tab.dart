import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/theme/app_colors.dart';

class AdminAttendanceTab extends StatefulWidget {
  const AdminAttendanceTab({super.key});

  @override
  State<AdminAttendanceTab> createState() => _AdminAttendanceTabState();
}

class _AdminAttendanceTabState extends State<AdminAttendanceTab> {
  DateTime _selectedDate = DateTime.now();
  List<Map<String, dynamic>> _sessions = [];
  List<Map<String, dynamic>> _roster = [];
  String? _selectedSessionId;
  bool _isLoading = true;
  bool _isSaving = false;
  String? _error;
  String? _token;

  @override
  void initState() {
    super.initState();
    _loadSessions();
  }

  String _dateParameter(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  Future<void> _loadSessions() async {
    setState(() {
      _isLoading = true;
      _error = null;
      _sessions = [];
      _roster = [];
      _selectedSessionId = null;
    });
    try {
      _token = AuthProvider.instance.accessToken;
      if (_token == null || _token!.isEmpty) {
        throw const _AttendanceException(
          'Đăng nhập bằng tài khoản quản trị/giảng viên trên máy chủ.',
        );
      }
      final response = await http.get(
        Uri.parse('${ApiConstants.baseUrl}/api/v1/buoi-hoc').replace(
          queryParameters: {'ngay': _dateParameter(_selectedDate)},
        ),
        headers: {'Authorization': 'Bearer $_token'},
      ).timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) {
        throw _AttendanceException(
          _readMessage(response.body) ??
              'Không tải được buổi học (mã lỗi ${response.statusCode}).',
        );
      }
      final result = jsonDecode(response.body);
      if (result is! List) {
        throw const _AttendanceException('Danh sách buổi học không hợp lệ.');
      }
      final sessions = result.whereType<Map<String, dynamic>>().toList();
      if (!mounted) return;
      setState(() {
        _sessions = sessions;
        _isLoading = false;
      });
      if (sessions.isNotEmpty) {
        await _selectSession(sessions.first['maBuoiHoc'].toString());
      }
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _AttendanceException
            ? error.message
            : 'Không thể kết nối máy chủ điểm danh.';
        _isLoading = false;
      });
    }
  }

  Future<void> _selectSession(String id) async {
    setState(() {
      _selectedSessionId = id;
      _isLoading = true;
      _error = null;
      _roster = [];
    });
    try {
      final response = await http.get(
        Uri.parse(
          '${ApiConstants.baseUrl}/api/v1/buoi-hoc/$id/diem-danh',
        ),
        headers: {'Authorization': 'Bearer $_token'},
      ).timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) {
        throw _AttendanceException(
          _readMessage(response.body) ??
              'Không tải được danh sách điểm danh (mã lỗi ${response.statusCode}).',
        );
      }
      final result = jsonDecode(response.body);
      if (result is! List) {
        throw const _AttendanceException('Danh sách điểm danh không hợp lệ.');
      }
      if (!mounted) return;
      setState(() {
        _roster = result.whereType<Map<String, dynamic>>().toList();
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _AttendanceException
            ? error.message
            : 'Không thể tải danh sách điểm danh.';
        _isLoading = false;
      });
    }
  }

  Future<void> _setAttendance(Map<String, dynamic> row, String status) async {
    final sessionId = _selectedSessionId;
    final studentId = row['maSinhVien']?.toString();
    if (sessionId == null || studentId == null || _isSaving) return;
    setState(() => _isSaving = true);
    try {
      final response = await http
          .put(
            Uri.parse(
              '${ApiConstants.baseUrl}/api/v1/buoi-hoc/$sessionId/diem-danh/$studentId',
            ),
            headers: {
              'Authorization': 'Bearer $_token',
              'Content-Type': 'application/json',
            },
            body: jsonEncode({
              'trangThai': status,
              'thoiGianCheckInUtc': status == 'CoMat'
                  ? DateTime.now().toUtc().toIso8601String()
                  : null,
            }),
          )
          .timeout(const Duration(seconds: 10));
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw _AttendanceException(
          _readMessage(response.body) ??
              'Không lưu được điểm danh (mã lỗi ${response.statusCode}).',
        );
      }
      await _selectSession(sessionId);
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            error is _AttendanceException
                ? error.message
                : 'Không thể lưu trạng thái điểm danh.',
          ),
        ),
      );
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  String? _readMessage(String body) {
    try {
      final value = jsonDecode(body);
      return value is Map ? value['message']?.toString() : null;
    } on FormatException {
      return null;
    }
  }

  Future<void> _pickDate() async {
    final date = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );
    if (date == null || !mounted) return;
    setState(() => _selectedDate = date);
    await _loadSessions();
  }

  @override
  Widget build(BuildContext context) {
    final present = _roster.where((row) => row['trangThai'] == 'CoMat').length;
    final absent = _roster.where((row) => row['trangThai'] == 'Vang').length;
    final unchecked = _roster.length - present - absent;
    final selected =
        _sessions.where((row) => row['maBuoiHoc'] == _selectedSessionId);

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Quản lý điểm danh',
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
            onPressed: _isLoading ? null : _loadSessions,
            icon: const Icon(Icons.refresh, color: AppColors.textPrimary),
            tooltip: 'Tải lại',
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
              child: Row(
                children: [
                  OutlinedButton.icon(
                    onPressed: _pickDate,
                    icon: const Icon(Icons.calendar_month_outlined),
                    label: Text(_dateParameter(_selectedDate)),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      value: _selectedSessionId,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        labelText: 'Buổi học',
                        border: OutlineInputBorder(),
                        contentPadding:
                            EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                      ),
                      items: _sessions
                          .map(
                            (session) => DropdownMenuItem(
                              value: session['maBuoiHoc'].toString(),
                              child: Text(
                                '${session['monHoc'] ?? 'Môn học'} • ${session['maLopHocPhan'] ?? ''}',
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          )
                          .toList(),
                      onChanged: _isLoading
                          ? null
                          : (value) {
                              if (value != null) _selectSession(value);
                            },
                    ),
                  ),
                ],
              ),
            ),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  _error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: AppColors.danger),
                ),
              ),
            if (selected.isNotEmpty)
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    '${selected.first['monHoc']} • Tiết ${selected.first['tietBatDau']}-${selected.first['tietKetThuc']} • ${selected.first['phong']}',
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
              ),
            if (_roster.isNotEmpty)
              Padding(
                padding: const EdgeInsets.fromLTRB(12, 6, 12, 8),
                child: Row(
                  children: [
                    _SummaryChip(label: 'Có mặt', count: present),
                    _SummaryChip(label: 'Vắng', count: absent),
                    _SummaryChip(label: 'Chưa điểm danh', count: unchecked),
                  ],
                ),
              ),
            if (_isLoading) const LinearProgressIndicator(minHeight: 2),
            Expanded(
              child: _error != null
                  ? Center(
                      child: OutlinedButton.icon(
                        onPressed: _loadSessions,
                        icon: const Icon(Icons.refresh),
                        label: const Text('Thử lại'),
                      ),
                    )
                  : _isLoading
                      ? const SizedBox.shrink()
                      : _sessions.isEmpty
                          ? const Center(
                              child: Padding(
                                padding: EdgeInsets.all(24),
                                child: Text(
                                  'Không có buổi học trong ngày đã chọn.',
                                  textAlign: TextAlign.center,
                                ),
                              ),
                            )
                          : _roster.isEmpty
                              ? const Center(
                                  child: Text(
                                    'Lớp học phần chưa có sinh viên đăng ký.',
                                  ),
                                )
                              : RefreshIndicator(
                                  onRefresh: () => _selectSession(
                                    _selectedSessionId!,
                                  ),
                                  child: ListView.builder(
                                    padding: const EdgeInsets.all(12),
                                    itemCount: _roster.length,
                                    itemBuilder: (context, index) =>
                                        _buildStudentRow(_roster[index]),
                                  ),
                                ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStudentRow(Map<String, dynamic> row) {
    final status = row['trangThai']?.toString() ?? 'ChuaDiemDanh';
    final label = switch (status) {
      'CoMat' => 'Có mặt',
      'Vang' => 'Vắng',
      _ => 'Chưa điểm danh',
    };
    return Card(
      color: Colors.white,
      child: ListTile(
        title: Text(row['hoTen']?.toString() ?? 'Sinh viên'),
        subtitle: Text('${row['maSinhVien'] ?? ''} • $label'),
        trailing: _isSaving
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : PopupMenuButton<String>(
                enabled: !_isSaving,
                onSelected: (value) => _setAttendance(row, value),
                itemBuilder: (_) => const [
                  PopupMenuItem(value: 'CoMat', child: Text('Đánh dấu có mặt')),
                  PopupMenuItem(value: 'Vang', child: Text('Đánh dấu vắng')),
                ],
                icon: const Icon(Icons.more_vert),
              ),
      ),
    );
  }
}

class _SummaryChip extends StatelessWidget {
  final String label;
  final int count;

  const _SummaryChip({required this.label, required this.count});

  @override
  Widget build(BuildContext context) => Expanded(
        child: Column(
          children: [
            Text(
              '$count',
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.primary,
              ),
            ),
            Text(
              label,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 11),
            ),
          ],
        ),
      );
}

class _AttendanceException implements Exception {
  final String message;
  const _AttendanceException(this.message);
}
