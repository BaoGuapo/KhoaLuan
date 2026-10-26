import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/theme/app_colors.dart';
import '../../core/widgets/face_scan_icon.dart';
import 'student_face_scan_screen.dart';

class StudentScheduleTab extends StatefulWidget {
  const StudentScheduleTab({super.key});

  @override
  State<StudentScheduleTab> createState() => _StudentScheduleTabState();
}

class _StudentScheduleTabState extends State<StudentScheduleTab> {
  final List<Map<String, dynamic>> _schedule = [];
  int _selectedDayIndex = 2; // Thứ 4 mặc định theo mockup (T4, ngày 17)
  int _weekOffset = 0;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadSchedule();
  }

  Future<void> _loadSchedule() async {
    setState(() {
      _isLoading = true;
    });

    try {
      final auth = AuthProvider.instance;
      final userName = auth.userName;
      final token = auth.accessToken;

      if (userName.isNotEmpty && token != null && token.isNotEmpty) {
        final response = await http.get(
          Uri.parse(
            '${ApiConstants.baseUrl}/api/v1/sinh-vien/${Uri.encodeComponent(userName)}/lich-hoc',
          ),
          headers: {'Authorization': 'Bearer $token'},
        ).timeout(const Duration(seconds: 4));

        if (response.statusCode == 200) {
          final decoded = jsonDecode(response.body);
          if (decoded is List && decoded.isNotEmpty) {
            final rows = decoded.whereType<Map<String, dynamic>>().toList();
            if (mounted) {
              setState(() {
                _schedule
                  ..clear()
                  ..addAll(rows);
                _isLoading = false;
              });
              return;
            }
          }
        }
      }
    } catch (_) {
      // Dùng dữ liệu thời khóa biểu chuẩn theo mockup
    }

    if (mounted) {
      _loadMockSchedule();
    }
  }

  void _loadMockSchedule() {
    setState(() {
      _schedule
        ..clear()
        ..addAll([
          {
            'thuTrongTuan': 4,
            'thoiGian': '07:00 - 09:15',
            'phong': 'Phòng B201',
            'monHoc': 'Toán cao cấp 2',
            'giangVien': 'TS. Nguyễn Thị Minh',
            'isActive': false,
          },
          {
            'thuTrongTuan': 4,
            'thoiGian': '09:30 - 11:45',
            'phong': 'Phòng A301',
            'monHoc': 'Lập trình ứng dụng Web',
            'giangVien': 'ThS. Trần Tuấn Anh',
            'isActive': true,
          },
          {
            'thuTrongTuan': 4,
            'thoiGian': '13:00 - 15:15',
            'phong': 'Phòng Lab 4',
            'monHoc': 'Cơ sở dữ liệu',
            'giangVien': 'TS. Lê Hoàng Nam',
            'isActive': false,
          },
          // Lịch các ngày khác
          {
            'thuTrongTuan': 2,
            'thoiGian': '07:00 - 09:15',
            'phong': 'Phòng C102',
            'monHoc': 'Nhập môn Hệ điều hành',
            'giangVien': 'ThS. Hoàng Văn Tuấn',
            'isActive': false,
          },
          {
            'thuTrongTuan': 5,
            'thoiGian': '09:30 - 11:45',
            'phong': 'Phòng B302',
            'monHoc': 'Toán học rời rạc',
            'giangVien': 'PGS.TS. Trần Quốc Đạt',
            'isActive': false,
          },
        ]);
      _isLoading = false;
    });
  }

  int get _selectedApiWeekday => _selectedDayIndex + 2;

  @override
  Widget build(BuildContext context) {
    final lessons = _schedule
        .where((row) => (row['thuTrongTuan'] as int?) == _selectedApiWeekday)
        .toList();

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Lịch học & Thời khóa biểu',
          style: TextStyle(
            color: Color(0xFF0F172A),
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        elevation: 0,
        backgroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: _loadSchedule,
            tooltip: 'Tải lại',
            icon: const Icon(Icons.refresh, color: AppColors.textPrimary),
          ),
        ],
      ),
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _loadSchedule,
                child: ListView(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 20,
                    vertical: 16,
                  ),
                  children: [
                    // ── Selector Tuần ─────────────────────────────────────────
                    _buildWeekSelector(),
                    const SizedBox(height: 16),

                    // ── Selector Ngày trong tuần (T2 - CN) ───────────────────
                    _buildDaySelector(),
                    const SizedBox(height: 22),

                    // ── Danh sách ca học trong ngày ──────────────────────────
                    if (lessons.isEmpty)
                      _buildEmptyDay()
                    else
                      ...lessons.map(_buildClassCard),

                    const SizedBox(height: 20),
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildWeekSelector() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.02),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left_rounded, color: AppColors.textPrimary),
            onPressed: () => setState(() => _weekOffset--),
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
          Text(
            'Tuần 5: 15/09 - 21/09',
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w800,
              color: Color(0xFF0F172A),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right_rounded, color: AppColors.textPrimary),
            onPressed: () => setState(() => _weekOffset++),
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
        ],
      ),
    );
  }

  Widget _buildDaySelector() {
    final days = [
      {'label': 'T2', 'day': '15'},
      {'label': 'T3', 'day': '16'},
      {'label': 'T4', 'day': '17'},
      {'label': 'T5', 'day': '18'},
      {'label': 'T6', 'day': '19'},
      {'label': 'T7', 'day': '20'},
      {'label': 'CN', 'day': '21'},
    ];

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: List.generate(days.length, (index) {
        final item = days[index];
        final selected = index == _selectedDayIndex;

        return InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: () => setState(() => _selectedDayIndex = index),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 200),
            width: 44,
            padding: const EdgeInsets.symmetric(vertical: 10),
            decoration: BoxDecoration(
              color: selected ? AppColors.primary : Colors.transparent,
              borderRadius: BorderRadius.circular(14),
              boxShadow: selected
                  ? [
                      BoxShadow(
                        color: AppColors.primary.withOpacity(0.35),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ]
                  : null,
            ),
            child: Column(
              children: [
                Text(
                  item['label']!,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    color: selected ? Colors.white : AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  item['day']!,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    color: selected ? Colors.white : const Color(0xFF0F172A),
                  ),
                ),
              ],
            ),
          ),
        );
      }),
    );
  }

  Widget _buildEmptyDay() {
    return Container(
      padding: const EdgeInsets.all(28),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        children: [
          const Icon(Icons.event_busy_rounded, size: 40, color: AppColors.textMuted),
          const SizedBox(height: 10),
          const Text(
            'Hôm nay không có lịch học',
            style: TextStyle(
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
              fontSize: 15,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'Bạn có thể nghỉ ngơi hoặc ôn luyện thêm kiến thức nhé!',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
          ),
        ],
      ),
    );
  }

  Widget _buildClassCard(Map<String, dynamic> item) {
    final bool isActive = item['isActive'] == true;
    final time = item['thoiGian'] ?? '09:30 - 11:45';
    final room = item['phong'] ?? 'Phòng A301';
    final course = item['monHoc'] ?? 'Lập trình ứng dụng Web';
    final lecturer = item['giangVien'] ?? 'ThS. Trần Tuấn Anh';

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: isActive ? const Color(0xFFEFF6FF) : Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: isActive ? AppColors.primary : const Color(0xFFE2E8F0),
          width: isActive ? 1.8 : 1.0,
        ),
        boxShadow: [
          BoxShadow(
            color: isActive ? AppColors.primary.withOpacity(0.08) : Colors.black.withOpacity(0.02),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                time,
                style: TextStyle(
                  color: isActive ? AppColors.primary : AppColors.textSecondary,
                  fontWeight: FontWeight.w700,
                  fontSize: 13,
                ),
              ),
              Text(
                room,
                style: TextStyle(
                  color: isActive ? AppColors.primary : const Color(0xFF0F172A),
                  fontWeight: FontWeight.w800,
                  fontSize: 13,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            course,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w800,
              color: Color(0xFF0F172A),
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Giảng viên: $lecturer',
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
            ),
          ),

          // Nút điểm danh ngay bằng khuôn mặt (nếu là lớp học hiện tại)
          if (isActive) ...[
            const SizedBox(height: 14),
            ElevatedButton(
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => StudentFaceScanScreen(
                      maBuoiHoc: 'WEB301-B05',
                      tenMonHoc: course,
                      phongHoc: room,
                      thoiGian: time,
                    ),
                  ),
                );
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                elevation: 0,
                minimumSize: const Size.fromHeight(46),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  FaceScanIcon(size: 20, color: Colors.white, strokeWidth: 2.2, cornerLength: 5),
                  SizedBox(width: 8),
                  Text(
                    'Điểm danh ngay bằng khuôn mặt',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
