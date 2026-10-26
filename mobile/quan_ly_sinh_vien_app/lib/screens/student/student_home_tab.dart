import 'package:flutter/material.dart';
import '../../core/auth/auth_provider.dart';
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
    final auth = AuthProvider.instance;
    final fullName = auth.userName.isNotEmpty ? auth.userName : 'Nguyễn Văn An';

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // ── Header: Lời chào & Avatar ──────────────────────────────────
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
                          fontSize: 22,
                          fontWeight: FontWeight.w800,
                          color: Color(0xFF0F172A),
                        ),
                      ),
                    ],
                  ),
                  Container(
                    width: 50,
                    height: 50,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: AppColors.primaryLight, width: 2),
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.primary.withOpacity(0.15),
                          blurRadius: 10,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: CircleAvatar(
                      backgroundColor: AppColors.primary.withOpacity(0.1),
                      child: const Icon(Icons.person, color: AppColors.primary, size: 28),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 20),

              // ── 3 Stat Cards Row (GPA, Tín chỉ, Chuyên cần) ────────────────
              FutureBuilder<AcademicResults>(
                future: _gradesFuture,
                builder: (context, snapshot) {
                  final results = snapshot.data;
                  final gpaVal = results?.cumulativeGpa?.toStringAsFixed(2) ?? '3.45';
                  final creditsVal = results?.gradedCredits != null ? '${results!.gradedCredits}' : '98';

                  return Row(
                    children: [
                      // GPA Tích lũy
                      Expanded(
                        child: _buildStatCard(
                          title: 'Điểm GPA tích lũy',
                          valueRich: RichText(
                            text: TextSpan(
                              children: [
                                TextSpan(
                                  text: gpaVal,
                                  style: const TextStyle(
                                    fontSize: 19,
                                    fontWeight: FontWeight.w900,
                                    color: AppColors.primary,
                                  ),
                                ),
                                const TextSpan(
                                  text: ' /4.0',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),

                      // Tín chỉ hoàn thành
                      Expanded(
                        child: _buildStatCard(
                          title: 'Tín chỉ hoàn thành',
                          valueRich: RichText(
                            text: TextSpan(
                              children: [
                                TextSpan(
                                  text: creditsVal,
                                  style: const TextStyle(
                                    fontSize: 19,
                                    fontWeight: FontWeight.w900,
                                    color: Color(0xFF0F172A),
                                  ),
                                ),
                                const TextSpan(
                                  text: ' /130',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),

                      // Chuyên cần
                      Expanded(
                        child: _buildStatCard(
                          title: 'Chuyên cần',
                          valueRich: const Text(
                            '95%',
                            style: TextStyle(
                              fontSize: 19,
                              fontWeight: FontWeight.w900,
                              color: Color(0xFF10B981),
                            ),
                          ),
                        ),
                      ),
                    ],
                  );
                },
              ),

              const SizedBox(height: 24),

              // ── Section: Phím tắt nhanh ────────────────────────────────────
              const Text(
                'Phím tắt nhanh',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF0F172A),
                ),
              ),

              const SizedBox(height: 14),

              // Row 1 phím tắt
              Row(
                children: [
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.calendar_today_rounded,
                      iconColor: const Color(0xFF3B82F6),
                      bgColor: const Color(0xFFEFF6FF),
                      label: 'Lịch học',
                      onTap: () => widget.onNavigateTab?.call(1),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.military_tech_rounded,
                      iconColor: const Color(0xFF10B981),
                      bgColor: const Color(0xFFECFDF5),
                      label: 'Điểm số',
                      onTap: () => widget.onNavigateTab?.call(5),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildShortcutItem(
                      icon: Icons.notifications_none_rounded,
                      iconColor: const Color(0xFFF59E0B),
                      bgColor: const Color(0xFFFFFBEB),
                      label: 'Thông báo',
                      onTap: () => widget.onNavigateTab?.call(3),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Row 2 phím tắt
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
                      icon: Icons.credit_card_rounded,
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
                      icon: Icons.menu_book_rounded,
                      iconColor: const Color(0xFF059669),
                      bgColor: const Color(0xFFECFDF5),
                      label: 'Đăng ký môn',
                      onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => const StudentCourseRegistrationScreen(),
                        ),
                      ),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 26),

              // ── Section: Lớp học tiếp theo (Card Navy Gradient) ────────────
              const Text(
                'Lớp học tiếp theo',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF0F172A),
                ),
              ),

              const SizedBox(height: 12),

              GestureDetector(
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => const StudentFaceScanScreen(
                        maBuoiHoc: 'WEB301-B05',
                        tenMonHoc: 'Lập trình ứng dụng Web',
                        phongHoc: 'Phòng A301',
                        thoiGian: '09:30 - 11:30 (Ca 2)',
                      ),
                    ),
                  );
                },
                child: Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [
                        Color(0xFF282566), // Deep indigo
                        Color(0xFF1E1B4B), // Dark navy
                      ],
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                    borderRadius: BorderRadius.circular(22),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFF1E1B4B).withOpacity(0.35),
                        blurRadius: 18,
                        offset: const Offset(0, 8),
                      ),
                    ],
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Badge SẮP DIỄN RA & Phòng học
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: const Color(0xFFEF4444),
                              borderRadius: BorderRadius.circular(20),
                            ),
                            child: const Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  '● SẮP DIỄN RA',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 0.3,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const Text(
                            'Phòng A301',
                            style: TextStyle(
                              color: Color(0xFFCBD5E1),
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),

                      const SizedBox(height: 14),

                      // Tên môn học
                      const Text(
                        'Lập trình ứng dụng Web',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 19,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 0.2,
                        ),
                      ),
                      const SizedBox(height: 4),

                      // Giảng viên
                      const Text(
                        'Giáo viên: ThS. Trần Tuấn Anh',
                        style: TextStyle(
                          color: Color(0xFF94A3B8),
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                        ),
                      ),

                      const SizedBox(height: 16),
                      Divider(color: Colors.white.withOpacity(0.12), height: 1),
                      const SizedBox(height: 14),

                      // Thời gian & Nút Điểm danh nhanh
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Row(
                            children: [
                              Icon(
                                Icons.access_time_rounded,
                                size: 16,
                                color: Color(0xFF93C5FD),
                              ),
                              SizedBox(width: 6),
                              Text(
                                'Hôm nay, 09:30 - 11:30 (Ca 2)',
                                style: TextStyle(
                                  color: Color(0xFFE2E8F0),
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                            decoration: BoxDecoration(
                              color: Colors.white.withOpacity(0.15),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Row(
                              children: [
                                FaceScanIcon(size: 14, color: Colors.white, cornerLength: 4),
                                SizedBox(width: 5),
                                Text(
                                  'Điểm danh',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
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
    required Widget valueRich,
  }) {
    return Container(
      height: 98,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFE2E8F0)),
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
              fontWeight: FontWeight.w600,
              color: AppColors.textSecondary,
              height: 1.2,
            ),
          ),
          valueRich,
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
        height: 84,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: const Color(0xFFE2E8F0)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.02),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: bgColor,
                borderRadius: BorderRadius.circular(12),
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
                fontWeight: FontWeight.w700,
                color: Color(0xFF0F172A),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
