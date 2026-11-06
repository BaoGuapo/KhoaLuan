import 'package:flutter/material.dart';

import '../../core/network/portal_api.dart';
import '../../screens/student/attendance_workflow_screen.dart';
import '../auth/auth_controller.dart';
import '../shared/account_tab.dart';
import '../shared/role_shell.dart';
import '../shared/ui.dart';
import 'results_view.dart';

/// Màn hình sinh viên: buổi học hôm nay, lịch học, quét QR điểm danh và lịch sử.
class StudentShell extends StatelessWidget {
  const StudentShell({required this.controller, super.key});

  final AuthController controller;

  @override
  Widget build(BuildContext context) {
    return RoleShell(
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => openQrAttendance(context),
        icon: const Icon(Icons.qr_code_scanner),
        label: const Text('Quét QR'),
      ),
      tabs: [
        const ShellTab(
          label: 'Hôm nay',
          icon: Icons.today_outlined,
          selectedIcon: Icons.today,
          page: TabPage(title: 'Buổi học hôm nay', body: _TodayView()),
        ),
        const ShellTab(
          label: 'Lịch học',
          icon: Icons.calendar_month_outlined,
          selectedIcon: Icons.calendar_month,
          page: TabPage(title: 'Lịch học', body: _ScheduleView()),
        ),
        const ShellTab(
          label: 'Điểm danh',
          icon: Icons.fact_check_outlined,
          selectedIcon: Icons.fact_check,
          page: TabPage(title: 'Lịch sử điểm danh', body: _HistoryView()),
        ),
        const ShellTab(
          label: 'Kết quả',
          icon: Icons.grading_outlined,
          selectedIcon: Icons.grading,
          page: TabPage(title: 'Kết quả học tập', body: ResultsView()),
        ),
        ShellTab(
          label: 'Tài khoản',
          icon: Icons.person_outline,
          selectedIcon: Icons.person,
          page: TabPage(
            title: 'Tài khoản',
            body: AccountTab(
              controller: controller,
              extraTiles: [
                ListTile(
                  leading: const Icon(Icons.face_retouching_natural),
                  title: const Text('Hồ sơ khuôn mặt'),
                  subtitle: const Text('Đăng ký hoặc cập nhật khuôn mặt để điểm danh'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(
                    MaterialPageRoute<void>(
                      builder: (_) => const AttendanceWorkflowScreen(enrollment: true),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

Future<void> openQrAttendance(BuildContext context) => Navigator.of(context).push(
      MaterialPageRoute<void>(builder: (_) => const AttendanceWorkflowScreen()),
    );

class _TodayView extends StatefulWidget {
  const _TodayView();

  @override
  State<_TodayView> createState() => _TodayViewState();
}

class _TodayViewState extends State<_TodayView> {
  final _api = PortalApi();

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AsyncPage<List<Map<String, dynamic>>>(
      load: () => _api.getList('api/v1/qr-diem-danh/buoi-hoc-cua-toi'),
      builder: (context, lessons, reload) {
        final now = DateTime.now();
        final done = lessons.where((x) => x['daDiemDanh'] == true).length;
        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            Card(
              color: Theme.of(context).colorScheme.primary,
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: Row(
                  children: [
                    const Icon(Icons.event_note, color: Colors.white, size: 36),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            '${dayOfWeekLabel(thuOf(now))}, ${dateFormat.format(now)}',
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 17,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            lessons.isEmpty
                                ? 'Hôm nay bạn không có buổi học.'
                                : 'Đã điểm danh $done/${lessons.length} buổi học',
                            style: const TextStyle(color: Colors.white70),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 8),
            if (lessons.isEmpty)
              const EmptyCard(
                message: 'Không có buổi học nào hôm nay.',
                icon: Icons.free_breakfast_outlined,
              )
            else
              for (final lesson in lessons)
                _LessonCard(
                  lesson: lesson,
                  onScan: () async {
                    await openQrAttendance(context);
                    await reload();
                  },
                ),
          ],
        );
      },
    );
  }
}

class _LessonCard extends StatelessWidget {
  const _LessonCard({required this.lesson, required this.onScan});

  final Map<String, dynamic> lesson;
  final VoidCallback onScan;

  @override
  Widget build(BuildContext context) {
    final attended = lesson['daDiemDanh'] == true;
    final checkIn = parseUtcToLocal(lesson['thoiGianDiemDanhUtc']);
    return Card(
      margin: const EdgeInsets.only(top: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    asText(lesson['tenMonHoc']),
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                ),
                Pill(
                  attended ? 'Đã điểm danh' : 'Chưa điểm danh',
                  color: attended ? AttendanceStatus.present : AttendanceStatus.late,
                ),
              ],
            ),
            const SizedBox(height: 8),
            _Line(Icons.schedule,
                periodRange(asInt(lesson['tietBatDau']), asInt(lesson['tietKetThuc']))),
            _Line(Icons.meeting_room_outlined, roomLabel(lesson['phong'])),
            _Line(Icons.person_outline, asText(lesson['giangVien'])),
            if (attended && checkIn != null)
              _Line(Icons.check_circle_outline, 'Điểm danh lúc ${timeFormat.format(checkIn)}'),
            if (!attended) ...[
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: onScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Quét QR điểm danh'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Line extends StatelessWidget {
  const _Line(this.icon, this.text);

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          Icon(icon, size: 16, color: const Color(0xFF5B6B7C)),
          const SizedBox(width: 6),
          Expanded(child: Text(text)),
        ],
      ),
    );
  }
}

class _ScheduleData {
  const _ScheduleData(this.semesters, this.entries);

  final List<Map<String, dynamic>> semesters;
  final List<Map<String, dynamic>> entries;
}

class _ScheduleView extends StatefulWidget {
  const _ScheduleView();

  @override
  State<_ScheduleView> createState() => _ScheduleViewState();
}

class _ScheduleViewState extends State<_ScheduleView> {
  final _api = PortalApi();
  String? _semester;

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  Future<_ScheduleData> _load() async {
    final results = await Future.wait([
      _api.getList('api/v1/hoc-ky'),
      _api.getList('api/v1/sinh-vien/me/lich-hoc'),
    ]);
    return _ScheduleData(results[0], results[1]);
  }

  /// Học kỳ đang diễn ra mà sinh viên có lịch; nếu không có thì học kỳ có lịch mới nhất.
  String? _defaultSemester(_ScheduleData data) {
    final withSchedule = data.entries.map((x) => asText(x['maHocKy'])).toSet();
    final today = DateTime.now();
    String? latest;
    for (final s in data.semesters) {
      final code = asText(s['maHocKy']);
      if (!withSchedule.contains(code)) continue;
      latest ??= code;
      final from = parseDate(s['ngayBatDau']);
      final to = parseDate(s['ngayKetThuc']);
      if (from != null && to != null && !today.isBefore(from) &&
          !today.isAfter(to.add(const Duration(days: 1)))) {
        return code;
      }
    }
    return latest ?? (withSchedule.isEmpty ? null : withSchedule.first);
  }

  @override
  Widget build(BuildContext context) {
    return AsyncPage<_ScheduleData>(
      load: _load,
      builder: (context, data, _) {
        final codes = data.entries.map((x) => asText(x['maHocKy'])).toSet();
        final semester = codes.contains(_semester) ? _semester : _defaultSemester(data);
        final names = {
          for (final s in data.semesters) asText(s['maHocKy']): asText(s['tenHocKy']),
        };
        final entries = data.entries.where((x) => asText(x['maHocKy']) == semester).toList();
        final today = thuOf(DateTime.now());

        final byDay = <int, List<Map<String, dynamic>>>{};
        for (final e in entries) {
          byDay.putIfAbsent(asInt(e['thuTrongTuan']), () => []).add(e);
        }
        final days = byDay.keys.toList()..sort();

        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            if (codes.length > 1)
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    for (final code in codes)
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: Text(names[code]?.isNotEmpty == true ? names[code]! : code),
                          selected: code == semester,
                          onSelected: (_) => setState(() => _semester = code),
                        ),
                      ),
                  ],
                ),
              )
            else if (semester != null)
              Padding(
                padding: const EdgeInsets.only(left: 4, bottom: 4),
                child: Text(
                  names[semester]?.isNotEmpty == true ? names[semester]! : semester,
                  style: const TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF5B6B7C)),
                ),
              ),
            if (entries.isEmpty)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: EmptyCard(
                  message: 'Bạn chưa có lịch học. Lịch sẽ hiển thị sau khi đăng ký học phần.',
                  icon: Icons.event_busy_outlined,
                ),
              ),
            for (final day in days) ...[
              SectionTitle(
                dayOfWeekLabel(day),
                trailing: day == today ? const Pill('Hôm nay', color: Color(0xFF0867B2)) : null,
              ),
              for (final e in byDay[day]!) _ScheduleCard(entry: e, isToday: day == today),
            ],
          ],
        );
      },
    );
  }
}

class _ScheduleCard extends StatelessWidget {
  const _ScheduleCard({required this.entry, required this.isToday});

  final Map<String, dynamic> entry;
  final bool isToday;

  @override
  Widget build(BuildContext context) {
    final from = asInt(entry['tietBatDau']);
    final to = asInt(entry['tietKetThuc']);
    final primary = Theme.of(context).colorScheme.primary;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: isToday ? BorderSide(color: primary, width: 1.2) : BorderSide.none,
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            Container(
              width: 56,
              padding: const EdgeInsets.symmetric(vertical: 10),
              decoration: BoxDecoration(
                color: primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  Text('Tiết', style: TextStyle(fontSize: 11, color: primary)),
                  Text(
                    '$from–$to',
                    style: TextStyle(fontWeight: FontWeight.w800, color: primary),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    asText(entry['monHoc']),
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    periodRange(from, to).split(' · ').last,
                    style: const TextStyle(fontSize: 13),
                  ),
                  Text(
                    '${roomLabel(entry['phong'])} ·${asText(entry['giangVien'])}',
                    style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HistoryView extends StatefulWidget {
  const _HistoryView();

  @override
  State<_HistoryView> createState() => _HistoryViewState();
}

class _HistoryViewState extends State<_HistoryView> {
  final _api = PortalApi();

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AsyncPage<List<Map<String, dynamic>>>(
      load: () => _api.getList('api/v1/qr-diem-danh/ket-qua-cua-toi'),
      builder: (context, rows, _) {
        final counts = <String, int>{};
        for (final r in rows) {
          final status = asText(r['trangThai']);
          counts[status] = (counts[status] ?? 0) + 1;
        }
        final recorded = (counts['CoMat'] ?? 0) +
            (counts['DiTre'] ?? 0) +
            (counts['CoPhep'] ?? 0) +
            (counts['Vang'] ?? 0);
        final rate = recorded == 0
            ? 0.0
            : ((counts['CoMat'] ?? 0) + (counts['DiTre'] ?? 0)) / recorded;

        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Expanded(
                          child: Text(
                            'Chuyên cần của bạn',
                            style: TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                        Pill(
                          recorded == 0 ? 'Chưa có số liệu' : formatRate(rate),
                          color: recorded == 0 ? AttendanceStatus.pending : rateColor(rate),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    AttendanceBreakdown(
                      coMat: counts['CoMat'] ?? 0,
                      diTre: counts['DiTre'] ?? 0,
                      coPhep: counts['CoPhep'] ?? 0,
                      vang: counts['Vang'] ?? 0,
                    ),
                  ],
                ),
              ),
            ),
            const SectionTitle('Các buổi gần đây'),
            if (rows.isEmpty)
              const EmptyCard(message: 'Chưa có kết quả điểm danh nào.')
            else
              Card(
                child: Column(
                  children: [
                    for (final (i, r) in rows.indexed) ...[
                      if (i > 0) const Divider(height: 1),
                      _HistoryRow(row: r),
                    ],
                  ],
                ),
              ),
          ],
        );
      },
    );
  }
}

class _HistoryRow extends StatelessWidget {
  const _HistoryRow({required this.row});

  final Map<String, dynamic> row;

  @override
  Widget build(BuildContext context) {
    final status = asText(row['trangThai']);
    final date = parseDate(row['ngayHoc']);
    final checkIn = parseUtcToLocal(row['thoiGianCheckInUtc']);
    return ListTile(
      leading: CircleAvatar(
        radius: 6,
        backgroundColor: AttendanceStatus.color(status),
      ),
      minLeadingWidth: 12,
      title: Text(asText(row['tenMonHoc'])),
      subtitle: Text([
        if (date != null) '${dayOfWeekLabel(thuOf(date))}, ${dateFormat.format(date)}',
        if (checkIn != null) 'lúc ${timeFormat.format(checkIn)}',
      ].join(' ')),
      trailing: Pill(AttendanceStatus.label(status), color: AttendanceStatus.color(status)),
    );
  }
}
