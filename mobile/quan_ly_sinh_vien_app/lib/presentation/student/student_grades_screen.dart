import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

class StudentGradesScreen extends StatefulWidget {
  const StudentGradesScreen({super.key});

  @override
  State<StudentGradesScreen> createState() => _StudentGradesScreenState();
}

class _StudentGradesScreenState extends State<StudentGradesScreen> {
  String _selectedHocKy = 'Học kỳ 1 - Năm học 2025-2026';
  int _selectedIndex = 2;

  final List<String> _subjects = [
    'Lập trình ứng dụng Web',
    'Cơ sở dữ liệu',
    'Toán học rời rạc',
    'Nhập môn Hệ điều hành',
    'Đối tượng và tiền xử lý',
  ];

  final Map<String, Map<String, dynamic>> _grades = {
    'Lập trình ứng dụng Web': {
      'soTinChi': '3 Tín chỉ',
      'giaiDoanKy': 'Gía kỳ: 8.5',
      'cuoiKy': 'Cuối kỳ: 9.0',
      'grade': 'A',
      'gradeColor': Color(0xFF10B981),
    },
    'Cơ sở dữ liệu': {
      'soTinChi': '3 Tín chỉ',
      'giaiDoanKy': 'Gía kỳ: 7.0',
      'cuoiKy': 'Cuối kỳ: 8.0',
      'grade': 'B+',
      'gradeColor': Color(0xFF3B82F6),
    },
    'Toán học rời rạc': {
      'soTinChi': '3 Tín chỉ',
      'giaiDoanKy': 'Gía kỳ: 6.5',
      'cuoiKy': 'Cuối kỳ: 7.0',
      'grade': 'B',
      'gradeColor': Color(0xFF3B82F6),
    },
    'Nhập môn Hệ điều hành': {
      'soTinChi': '3 Tín chỉ',
      'giaiDoanKy': 'Gía kỳ: 9.0',
      'cuoiKy': 'Cuối kỳ: 8.5',
      'grade': 'A',
      'gradeColor': Color(0xFF10B981),
    },
    'Đối tượng và tiền xử lý': {
      'soTinChi': '4 Tín chỉ',
      'giaiDoanKy': 'Gía kỳ: 5.5',
      'cuoiKy': 'Cuối kỳ: 6.0',
      'grade': 'C+',
      'gradeColor': Color(0xFFF59E0B),
    },
  };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Kết quả học tập',
          style: TextStyle(
            color: Color(0xFF0F172A),
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        centerTitle: true,
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Học kỳ selector
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              color: Colors.white,
              child: Row(
                children: [
                  const Icon(Icons.calendar_today_outlined,
                      color: AppColors.textMuted, size: 20),
                  const SizedBox(width: 12),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      value: _selectedHocKy,
                      decoration: const InputDecoration(
                        isDense: true,
                        border: OutlineInputBorder(borderSide: BorderSide.none),
                        focusedBorder: OutlineInputBorder(
                          borderSide: BorderSide.none,
                        ),
                        enabledBorder: OutlineInputBorder(
                          borderSide: BorderSide.none,
                        ),
                        contentPadding: EdgeInsets.zero,
                      ),
                      items: const [
                        'Học kỳ 1 - Năm học 2025-2026',
                        'Học kỳ 2 - Năm học 2024-2025',
                        'Học kỳ 1 - Năm học 2024-2025',
                      ]
                          .map((e) => DropdownMenuItem(
                                value: e,
                                child: Text(e,
                                    style: const TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.w600)),
                              ))
                          .toList(),
                      onChanged: (v) => setState(() => _selectedHocKy = v!),
                    ),
                  ),
                ],
              ),
            ),

            Expanded(
              child: ListView.builder(
                padding: const EdgeInsets.all(20),
                itemCount: _subjects.length,
                itemBuilder: (context, index) {
                  final subject = _subjects[index];
                  final grade = _grades[subject]!;
                  return _GradeItem(
                    subject: subject,
                    soTinChi: grade['soTinChi'] as String,
                    giaiDoanKy: grade['giaiDoanKy'] as String,
                    cuoiKy: grade['cuoiKy'] as String,
                    grade: grade['grade'] as String,
                    gradeColor: grade['gradeColor'] as Color,
                  );
                },
              ),
            ),

            // GPA Summary
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(20),
              color: const Color(0xFF1E3A5F),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Điểm trung bình học kỳ:',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 14,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '3.45 / 4.0',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 28,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Điểm trung bình tích lũy:',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 14,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '3.52 / 4.0',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: _buildBottomNav(),
    );
  }

  Widget _buildBottomNav() {
    final items = [
      {'icon': Icons.home, 'label': 'Trang chủ', 'index': 0},
      {'icon': Icons.calendar_today, 'label': 'Lịch học', 'index': 1},
      {'icon': Icons.face, 'label': 'Điểm danh', 'index': 2},
      {'icon': Icons.notifications, 'label': 'Thông báo', 'index': 3},
      {'icon': Icons.person, 'label': 'Cá nhân', 'index': 4},
    ];

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.borderLight)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 8,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        child: SizedBox(
          height: 64,
          child: Row(
            children: items.map((item) {
              final isSelected = _selectedIndex == item['index'] as int;
              return Expanded(
                child: GestureDetector(
                  onTap: () => setState(() => _selectedIndex = item['index'] as int),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        item['icon'] as IconData,
                        color: isSelected
                            ? AppColors.primary
                            : AppColors.textMuted,
                        size: 22,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        item['label'] as String,
                        style: TextStyle(
                          color: isSelected
                              ? AppColors.primary
                              : AppColors.textMuted,
                          fontSize: 10,
                          fontWeight: isSelected
                              ? FontWeight.w800
                              : FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }).toList(),
          ),
        ),
      ),
    );
  }
}

class _GradeItem extends StatelessWidget {
  final String subject;
  final String soTinChi;
  final String giaiDoanKy;
  final String cuoiKy;
  final String grade;
  final Color gradeColor;

  const _GradeItem({
    required this.subject,
    required this.soTinChi,
    required this.giaiDoanKy,
    required this.cuoiKy,
    required this.grade,
    required this.gradeColor,
  });

  @override
  Widget build(BuildContext context) {
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
              Expanded(
                child: Text(
                  subject,
                  style: const TextStyle(
                    color: Color(0xFF0F172A),
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: gradeColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  grade,
                  style: TextStyle(
                    color: gradeColor,
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _GradeStat(
                  label: soTinChi,
                  icon: Icons.school_outlined,
                ),
              ),
              Expanded(
                child: _GradeStat(
                  label: giaiDoanKy,
                  icon: Icons.circle_outlined,
                ),
              ),
              Expanded(
                child: _GradeStat(
                  label: cuoiKy,
                  icon: Icons.circle_outlined,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _GradeStat extends StatelessWidget {
  final String label;
  final IconData icon;

  const _GradeStat({required this.label, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 14, color: AppColors.textMuted),
        const SizedBox(width: 6),
        Text(
          label,
          style: const TextStyle(
            color: Color(0xFF64748B),
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}
