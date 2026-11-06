import 'package:flutter/material.dart';

import '../../core/constants/role_names.dart';
import '../../core/network/portal_api.dart';
import '../../screens/admin/attendance_manager_screen.dart';
import '../auth/auth_controller.dart';
import '../shared/account_tab.dart';
import '../shared/class_detail_screen.dart';
import '../shared/role_shell.dart';
import '../shared/ui.dart';

/// Màn hình giảng viên: thống kê lớp đang dạy và mở phiên điểm danh QR.
class LecturerShell extends StatelessWidget {
  const LecturerShell({required this.controller, super.key});

  final AuthController controller;

  @override
  Widget build(BuildContext context) {
    return RoleShell(
      tabs: [
        const ShellTab(
          label: 'Lớp của tôi',
          icon: Icons.class_outlined,
          selectedIcon: Icons.class_,
          page: TabPage(title: 'Lớp của tôi', body: _MyClassesView()),
        ),
        const ShellTab(
          label: 'Điểm danh QR',
          icon: Icons.qr_code_2_outlined,
          selectedIcon: Icons.qr_code_2,
          page: AttendanceManagerScreen(roles: [RoleNames.giangVien]),
        ),
        ShellTab(
          label: 'Tài khoản',
          icon: Icons.person_outline,
          selectedIcon: Icons.person,
          page: TabPage(
            title: 'Tài khoản',
            body: AccountTab(controller: controller),
          ),
        ),
      ],
    );
  }
}

/// Lớp đang dạy kèm số liệu tổng hợp từ các buổi đã chốt.
class _ClassSummary {
  _ClassSummary(this.lop, Map<String, dynamic>? history) {
    final sessions = (history?['buoiHoc'] as List? ?? const []).whereType<Map>();
    totalSessions = sessions.length;
    for (final s in sessions) {
      if (s['daChot'] != true) continue;
      heldSessions++;
      coMat += asInt(s['coMat']);
      diTre += asInt(s['diTre']);
      coPhep += asInt(s['coPhep']);
      vang += asInt(s['vang']);
    }
  }

  final Map<String, dynamic> lop;
  int totalSessions = 0;
  int heldSessions = 0;
  int coMat = 0, diTre = 0, coPhep = 0, vang = 0;

  int get recorded => coMat + diTre + coPhep + vang;
  double get rate => recorded == 0 ? 0 : (coMat + diTre) / recorded;
}

class _MyClassesView extends StatefulWidget {
  const _MyClassesView();

  @override
  State<_MyClassesView> createState() => _MyClassesViewState();
}

class _MyClassesViewState extends State<_MyClassesView> {
  final _api = PortalApi();

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  Future<List<_ClassSummary>> _load() async {
    final classes = await _api.getList('api/v1/giang-day/cua-toi');
    // Mỗi lớp lấy lịch sử buổi học song song; lỗi một lớp không làm hỏng cả danh sách.
    final histories = await Future.wait(classes.map((lop) async {
      try {
        final code = Uri.encodeComponent(asText(lop['maLopHocPhan']));
        return await _api.getObject('api/v1/giang-day/lop/$code/lich-su');
      } catch (_) {
        return null;
      }
    }));
    return [
      for (var i = 0; i < classes.length; i++) _ClassSummary(classes[i], histories[i]),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return AsyncPage<List<_ClassSummary>>(
      load: _load,
      builder: (context, classes, _) {
        if (classes.isEmpty) {
          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: const [
              EmptyCard(message: 'Bạn chưa được phân công lớp học phần nào.'),
            ],
          );
        }

        final today = thuOf(DateTime.now());
        final todayClasses = classes
            .where((x) => asInt(x.lop['thuTrongTuan']) == today)
            .toList()
          ..sort((a, b) => asInt(a.lop['tietBatDau']).compareTo(asInt(b.lop['tietBatDau'])));
        var coMat = 0, diTre = 0, coPhep = 0, vang = 0, held = 0, students = 0;
        for (final c in classes) {
          coMat += c.coMat;
          diTre += c.diTre;
          coPhep += c.coPhep;
          vang += c.vang;
          held += c.heldSessions;
          students += asInt(c.lop['siSoHienTai']);
        }
        final recorded = coMat + diTre + coPhep + vang;
        final rate = recorded == 0 ? 0.0 : (coMat + diTre) / recorded;

        // Nhóm theo học kỳ, giữ thứ tự backend trả về.
        final bySemester = <String, List<_ClassSummary>>{};
        for (final c in classes) {
          bySemester.putIfAbsent(asText(c.lop['tenHocKy'], asText(c.lop['maHocKy'])), () => []).add(c);
        }

        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            StatGrid(children: [
              StatTile(
                label: 'Tỷ lệ chuyên cần',
                value: recorded == 0 ? '—' : formatRate(rate),
                icon: Icons.insights,
                color: recorded == 0 ? AttendanceStatus.pending : rateColor(rate),
              ),
              StatTile(
                label: 'Lớp đang dạy',
                value: '${classes.length}',
                icon: Icons.class_outlined,
              ),
              StatTile(
                label: 'Sinh viên',
                value: '$students',
                icon: Icons.groups_outlined,
              ),
              StatTile(
                label: 'Buổi đã chốt',
                value: '$held',
                icon: Icons.event_available,
              ),
            ]),
            const SizedBox(height: 12),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: AttendanceBreakdown(
                  coMat: coMat,
                  diTre: diTre,
                  coPhep: coPhep,
                  vang: vang,
                ),
              ),
            ),
            SectionTitle('Lịch dạy hôm nay (${dayOfWeekLabel(today)})'),
            if (todayClasses.isEmpty)
              const EmptyCard(message: 'Hôm nay bạn không có lịch dạy.', icon: Icons.free_breakfast_outlined)
            else
              for (final c in todayClasses) _TodayClassCard(summary: c),
            for (final entry in bySemester.entries) ...[
              SectionTitle(entry.key),
              for (final c in entry.value) _LecturerClassCard(summary: c),
            ],
            const SizedBox(height: 16),
          ],
        );
      },
    );
  }
}

void _openClass(BuildContext context, Map<String, dynamic> lop) {
  Navigator.of(context).push(MaterialPageRoute<void>(
    builder: (_) => ClassDetailScreen(
      maLopHocPhan: asText(lop['maLopHocPhan']),
      title: asText(lop['tenMonHoc'], asText(lop['maLopHocPhan'])),
    ),
  ));
}

class _TodayClassCard extends StatelessWidget {
  const _TodayClassCard({required this.summary});

  final _ClassSummary summary;

  @override
  Widget build(BuildContext context) {
    final lop = summary.lop;
    final primary = Theme.of(context).colorScheme.primary;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      color: const Color(0xFFE8F2FB),
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        leading: Icon(Icons.schedule, color: primary),
        title: Text(
          asText(lop['tenMonHoc']),
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        subtitle: Text(
          '${periodRange(asInt(lop['tietBatDau']), asInt(lop['tietKetThuc']))}\n'
          '${roomLabel(asText(lop['tenPhong'], asText(lop['maPhong'])))} ·${asText(lop['maLopHocPhan'])}',
        ),
        isThreeLine: true,
        trailing: const Icon(Icons.chevron_right),
        onTap: () => _openClass(context, lop),
      ),
    );
  }
}

class _LecturerClassCard extends StatelessWidget {
  const _LecturerClassCard({required this.summary});

  final _ClassSummary summary;

  @override
  Widget build(BuildContext context) {
    final lop = summary.lop;
    final hasData = summary.recorded > 0;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => _openClass(context, lop),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      asText(lop['tenMonHoc']),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                  Pill(
                    hasData ? formatRate(summary.rate) : 'Chưa có số liệu',
                    color: hasData ? rateColor(summary.rate) : AttendanceStatus.pending,
                  ),
                ],
              ),
              const SizedBox(height: 2),
              Text(
                '${asText(lop['maLopHocPhan'])} · ${dayOfWeekLabel(asInt(lop['thuTrongTuan']))}, '
                'tiết ${asInt(lop['tietBatDau'])}–${asInt(lop['tietKetThuc'])}',
                style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
              ),
              const SizedBox(height: 8),
              AttendanceBreakdown(
                coMat: summary.coMat,
                diTre: summary.diTre,
                coPhep: summary.coPhep,
                vang: summary.vang,
                showLegend: false,
                height: 6,
              ),
              const SizedBox(height: 8),
              Text(
                '${asInt(lop['siSoHienTai'])} sinh viên · '
                '${summary.heldSessions}/${summary.totalSessions} buổi đã chốt',
                style: const TextStyle(fontSize: 12),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
