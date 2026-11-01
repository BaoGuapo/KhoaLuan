import 'package:flutter/material.dart';

import '../../core/student/student_portal_service.dart';
import '../../core/theme/app_colors.dart';

class StudentGradesTab extends StatefulWidget {
  const StudentGradesTab({super.key});

  @override
  State<StudentGradesTab> createState() => _StudentGradesTabState();
}

class _StudentGradesTabState extends State<StudentGradesTab> {
  final _service = StudentPortalService();
  AcademicResults? _results;
  String? _error;
  bool _loading = true;
  String? _semesterCode;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await _service.getGrades();
      if (!mounted) return;
      setState(() {
        _results = results;
        _semesterCode ??=
            results.semesters.isNotEmpty ? results.semesters.last.code : null;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is StudentPortalException
            ? error.message
            : 'Không tải được kết quả học tập.';
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final results = _results;
    SemesterGrades? selected;
    if (results != null) {
      for (final semester in results.semesters) {
        if (semester.code == _semesterCode) selected = semester;
      }
    }

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Kết quả học tập',
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
            tooltip: 'Làm mới',
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh, color: AppColors.primary),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _MessageState(message: _error!, onRetry: _load)
              : results == null
                  ? const _MessageState(message: 'Chưa có dữ liệu học tập.')
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: ListView(
                        padding: const EdgeInsets.all(16),
                        children: [
                          if (results.isDemo) const _DemoBanner(),
                          _SummaryCard(
                            gpa: results.cumulativeGpa,
                            credits: results.gradedCredits,
                          ),
                          const SizedBox(height: 16),
                          if (results.semesters.isNotEmpty)
                            DropdownButtonFormField<String>(
                              value: _semesterCode,
                              decoration: const InputDecoration(
                                labelText: 'Học kỳ',
                                border: OutlineInputBorder(),
                                filled: true,
                                fillColor: Colors.white,
                              ),
                              items: results.semesters
                                  .map(
                                    (semester) => DropdownMenuItem(
                                      value: semester.code,
                                      child: Text(semester.name),
                                    ),
                                  )
                                  .toList(),
                              onChanged: (value) =>
                                  setState(() => _semesterCode = value),
                            ),
                          const SizedBox(height: 14),
                          if (selected != null)
                            _SemesterOverview(semester: selected),
                          const SizedBox(height: 12),
                          if (selected == null || selected.courses.isEmpty)
                            const _MessageState(
                              message:
                                  'Học kỳ này chưa có môn học được đăng ký.',
                            )
                          else
                            ...selected.courses.map(_CourseGradeCard.new),
                          const SizedBox(height: 20),
                        ],
                      ),
                    ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.gpa, required this.credits});

  final double? gpa;
  final int credits;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: Row(
          children: [
            Expanded(
              child: _SummaryValue(
                label: 'GPA tích lũy (hệ 4)',
                value: gpa?.toStringAsFixed(2) ?? '—',
                icon: Icons.school_outlined,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _SummaryValue(
                label: 'Tín chỉ đã có điểm',
                value: '$credits',
                icon: Icons.menu_book_outlined,
              ),
            ),
          ],
        ),
      );
}

class _SummaryValue extends StatelessWidget {
  const _SummaryValue({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: AppColors.primary),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(
              fontSize: 23,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          Text(
            label,
            style:
                const TextStyle(fontSize: 12, color: AppColors.textSecondary),
          ),
        ],
      );
}

class _SemesterOverview extends StatelessWidget {
  const _SemesterOverview({required this.semester});
  final SemesterGrades semester;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Row(
          children: [
            Text(
              '${semester.courseCount} môn',
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            const SizedBox(width: 14),
            Text('${semester.creditsWithGrades} tín chỉ có điểm'),
            const Spacer(),
            Text(
              semester.gpa == null
                  ? 'Đang học'
                  : 'GPA ${semester.gpa!.toStringAsFixed(2)}',
              style: const TextStyle(
                fontWeight: FontWeight.w700,
                color: AppColors.primary,
              ),
            ),
          ],
        ),
      );
}

class _CourseGradeCard extends StatelessWidget {
  const _CourseGradeCard(this.course);
  final Map<String, dynamic> course;

  @override
  Widget build(BuildContext context) {
    final total = course['diemTongKet'];
    final inProgress = total == null;
    final passed = !inProgress && (double.tryParse('$total') ?? 0) >= 4;
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${course['tenMonHoc'] ?? 'Môn học'}',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        '${course['maMonHoc'] ?? ''} · ${course['tinChi'] ?? 0} tín chỉ',
                        style: const TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: inProgress
                        ? const Color(0xFFEFF6FF)
                        : passed
                            ? const Color(0xFFECFDF5)
                            : const Color(0xFFFEF2F2),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    inProgress
                        ? 'Đang học'
                        : passed
                            ? '${course['diemChu'] ?? ''} · Đạt'
                            : '${course['diemChu'] ?? ''} · Học lại',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: inProgress
                          ? AppColors.primary
                          : passed
                              ? AppColors.success
                              : AppColors.danger,
                    ),
                  ),
                ),
              ],
            ),
            const Divider(height: 22),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _ScoreValue(label: 'Quá trình', value: course['diemQuaTrinh']),
                _ScoreValue(label: 'Thi', value: course['diemThi']),
                _ScoreValue(label: 'Tổng kết', value: total, strong: true),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ScoreValue extends StatelessWidget {
  const _ScoreValue({
    required this.label,
    required this.value,
    this.strong = false,
  });

  final String label;
  final Object? value;
  final bool strong;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: const TextStyle(
                  fontSize: 11, color: AppColors.textSecondary)),
          const SizedBox(height: 4),
          Text(
            value == null ? '—' : double.parse('$value').toStringAsFixed(2),
            style: TextStyle(
              fontSize: 15,
              fontWeight: strong ? FontWeight.w800 : FontWeight.w600,
              color: strong ? AppColors.primary : AppColors.textPrimary,
            ),
          ),
        ],
      );
}

class _DemoBanner extends StatelessWidget {
  const _DemoBanner();

  @override
  Widget build(BuildContext context) => Container(
        margin: const EdgeInsets.only(bottom: 14),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: const Color(0xFFFFFBEB),
          border: Border.all(color: const Color(0xFFFDE68A)),
          borderRadius: BorderRadius.circular(12),
        ),
        child: const Text(
          'DỮ LIỆU DEMO: các môn và điểm bên dưới dùng để minh họa ứng dụng, không phải kết quả học tập chính thức.',
          style: TextStyle(color: Color(0xFF92400E), height: 1.35),
        ),
      );
}

class _MessageState extends StatelessWidget {
  const _MessageState({required this.message, this.onRetry});
  final String message;
  final Future<void> Function()? onRetry;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.school_outlined,
                  size: 48, color: AppColors.textMuted),
              const SizedBox(height: 12),
              Text(message, textAlign: TextAlign.center),
              if (onRetry != null) ...[
                const SizedBox(height: 12),
                TextButton(onPressed: onRetry, child: const Text('Thử lại')),
              ],
            ],
          ),
        ),
      );
}
