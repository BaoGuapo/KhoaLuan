import 'package:flutter/material.dart';
import '../../core/auth/auth_service.dart';
import '../../core/student/student_portal_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/widgets/face_scan_icon.dart';
import 'student_face_scan_screen.dart';
import 'student_tuition_screen.dart';
import 'student_course_registration_screen.dart';

class StudentHomeTab extends StatefulWidget {
  final Function(int)? onNavigateTab;

  const StudentHomeTab({super.key, this.onNavigateTab});

  @override
  State<StudentHomeTab> createState() => _StudentHomeTabState();
}

class _StudentHomeTabState extends State<StudentHomeTab> {
  late final Future<AcademicResults> _gradesFuture =
      StudentPortalService().getGrades();

  @override
  Widget build(BuildContext context) {
    final user = AuthService().currentUser;
    final fullName = user?.fullName ?? 'Sinh viên';

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header Row: Greeting & Avatar
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Xin chào 👋',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        fullName,
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border:
                          Border.all(color: AppColors.primaryLight, width: 2),
                    ),
                    child: const Icon(Icons.person, color: AppColors.primary),
                  ),
                ],
              ),

              const SizedBox(height: 20),

              // 3 Stat Cards Row
              FutureBuilder<AcademicResults>(
                future: _gradesFuture,
                builder: (context, snapshot) {
                  final results = snapshot.data;
                  return Column(
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: _buildStatCard(
                              title: 'GPA tích lũy · hệ 4',
                              valueWidget: Text(
                                results?.cumulativeGpa?.toStringAsFixed(2) ??
                                    '—',
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w800,
                                  color: AppColors.primary,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: _buildStatCard(
                              title: 'Tín chỉ đã có điểm',
                              valueWidget: Text(
                                results?.gradedCredits.toString() ?? '—',
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF1E293B),
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: _buildStatCard(
                              title: 'Chuyên cần',
                              valueWidget: Text(
                                user?.attendanceRate == null
                                    ? '—'
                                    : '${user!.attendanceRate}%',
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w800,
                                  color: AppColors.success,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      if (snapshot.hasError)
                        const Padding(
                          padding: EdgeInsets.only(top: 6),
                          child: Text(
                            'Không tải được dữ liệu học tập từ máy chủ.',
                            style: TextStyle(
                              color: AppColors.danger,
                              fontSize: 12,
                            ),
                          ),
                        ),
                    ],
                  );
                },
              ),

              const SizedBox(height: 24),

              // Section: Phím tắt nhanh
              const Text(
                'Phím tắt nhanh',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),

              const SizedBox(height: 14),

              // 2x3 Shortcuts Grid
              Row(
                children: [
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.calendar_today_outlined,
                      iconColor: const Color(0xFF3B82F6),
                      bgColor: const Color(0xFFEFF6FF),
                      label: 'Lịch học',
                      onTap: () => widget.onNavigateTab?.call(1),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.military_tech_outlined,
                      iconColor: const Color(0xFF10B981),
                      bgColor: const Color(0xFFECFDF5),
                      label: 'Điểm số',
                      onTap: () => widget.onNavigateTab?.call(5),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.notifications_none_outlined,
                      iconColor: const Color(0xFFF59E0B),
                      bgColor: const Color(0xFFFFFBEB),
                      label: 'Thông báo',
                      onTap: () => widget.onNavigateTab?.call(3),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: _buildShortcutItem(
                      customIcon: const FaceScanIcon(
                        size: 22,
                        color: Color(0xFF8B5CF6),
                        strokeWidth: 2.2,
                        cornerLength: 5,
                      ),
                      bgColor: const Color(0xFFF5F3FF),
                      label: 'Điểm danh',
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => const StudentFaceScanScreen(),
                          ),
                        );
                      },
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.credit_card_outlined,
                      iconColor: const Color(0xFFEC4899),
                      bgColor: const Color(0xFFFDF2F8),
                      label: 'Học phí',
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => const StudentTuitionScreen(),
                          ),
                        );
                      },
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.menu_book_outlined,
                      iconColor: const Color(0xFF059669),
                      bgColor: const Color(0xFFECFDF5),
                      label: 'Đăng ký học phần',
                      onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) =>
                              const StudentCourseRegistrationScreen(),
                        ),
                      ),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 24),

              // Section: Lớp học tiếp theo
              const Text(
                'Lớp học tiếp theo',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),

              const SizedBox(height: 12),

              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(
                    Icons.calendar_month_outlined,
                    color: AppColors.primary,
                  ),
                  title: const Text(
                    'Thời khóa biểu cá nhân',
                    style: TextStyle(fontWeight: FontWeight.w700),
                  ),
                  subtitle: const Text(
                    'Mở tab Lịch học để tải lịch từ máy chủ.',
                  ),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => widget.onNavigateTab?.call(1),
                ),
              ),

              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatCard({
    required String title,
    required Widget valueWidget,
  }) {
    return Container(
      height: 96,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFF1F5F9)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.02),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            title,
            maxLines: 2,
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w500,
              color: AppColors.textSecondary,
              height: 1.2,
            ),
          ),
          valueWidget,
        ],
      ),
    );
  }

  Widget _buildShortcutItem({
    IconData? icon,
    Widget? customIcon,
    Color? iconColor,
    required Color bgColor,
    required String label,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        height: 82,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFF1F5F9)),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: bgColor,
                borderRadius: BorderRadius.circular(10),
              ),
              alignment: Alignment.center,
              child: customIcon ??
                  Icon(
                    icon,
                    color: iconColor,
                    size: 20,
                  ),
            ),
            const SizedBox(height: 6),
            Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.textPrimary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
