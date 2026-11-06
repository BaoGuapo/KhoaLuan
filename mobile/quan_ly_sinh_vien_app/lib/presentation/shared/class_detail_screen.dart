import 'package:flutter/material.dart';

import '../../core/network/portal_api.dart';
import 'ui.dart';

/// Thống kê chuyên cần của một lớp học phần: theo sinh viên và theo từng buổi.
/// Dùng chung cho Admin, Cán bộ quản lý và giảng viên phụ trách lớp.
class ClassDetailScreen extends StatefulWidget {
  const ClassDetailScreen({required this.maLopHocPhan, this.title, super.key});

  final String maLopHocPhan;
  final String? title;

  @override
  State<ClassDetailScreen> createState() => _ClassDetailScreenState();
}

class _ClassData {
  const _ClassData(this.workspace, this.history);

  final Map<String, dynamic> workspace;
  final Map<String, dynamic> history;
}

class _ClassDetailScreenState extends State<ClassDetailScreen> {
  final _api = PortalApi();

  Future<_ClassData> _load() async {
    final code = Uri.encodeComponent(widget.maLopHocPhan);
    final results = await Future.wait([
      _api.getObject('api/v1/giang-day/lop/$code'),
      _api.getObject('api/v1/giang-day/lop/$code/lich-su'),
    ]);
    return _ClassData(results[0], results[1]);
  }

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(widget.title ?? widget.maLopHocPhan),
          bottom: const TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            tabs: [Tab(text: 'Sinh viên'), Tab(text: 'Buổi học')],
          ),
        ),
        body: AsyncPage<_ClassData>(
          load: _load,
          builder: (context, data, _) => TabBarView(
            children: [
              _StudentsView(data: data),
              _SessionsView(history: data.history),
            ],
          ),
        ),
      ),
    );
  }
}

class _ClassHeader extends StatelessWidget {
  const _ClassHeader({required this.lop});

  final Map<String, dynamic> lop;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              asText(lop['tenMonHoc']),
              style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 4),
            Text('${asText(lop['maLopHocPhan'])} · ${asText(lop['tenHocKy'])}'),
            const SizedBox(height: 8),
            _IconLine(Icons.person_outline, asText(lop['hoTenGiangVien'])),
            _IconLine(
              Icons.schedule,
              '${dayOfWeekLabel(asInt(lop['thuTrongTuan']))}, '
              '${periodRange(asInt(lop['tietBatDau']), asInt(lop['tietKetThuc']))}',
            ),
            _IconLine(
              Icons.meeting_room_outlined,
              roomLabel(asText(lop['tenPhong'], asText(lop['maPhong']))),
            ),
            _IconLine(
              Icons.groups_outlined,
              'Sĩ số ${asInt(lop['siSoHienTai'])}/${asInt(lop['siSoToiDa'])}',
            ),
          ],
        ),
      ),
    );
  }
}

class _IconLine extends StatelessWidget {
  const _IconLine(this.icon, this.text);

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
          Expanded(child: Text(text, style: const TextStyle(color: Color(0xFF3B4A5A)))),
        ],
      ),
    );
  }
}

class _StudentsView extends StatefulWidget {
  const _StudentsView({required this.data});

  final _ClassData data;

  @override
  State<_StudentsView> createState() => _StudentsViewState();
}

class _StudentsViewState extends State<_StudentsView> {
  bool _onlyFlagged = false;
  String _query = '';

  @override
  Widget build(BuildContext context) {
    final lop = Map<String, dynamic>.from(widget.data.workspace['lopHocPhan'] as Map? ?? {});
    final all = (widget.data.workspace['sinhVien'] as List? ?? const [])
        .whereType<Map>()
        .map(Map<String, dynamic>.from)
        .toList()
      ..sort((a, b) {
        final byAbsent = asInt(b['vang']).compareTo(asInt(a['vang']));
        return byAbsent != 0
            ? byAbsent
            : asText(a['maSinhVien']).compareTo(asText(b['maSinhVien']));
      });

    final sessions = (widget.data.history['buoiHoc'] as List? ?? const [])
        .whereType<Map>()
        .toList();
    final held = sessions.where((x) => x['daChot'] == true).length;
    final flagged = all.where((x) => WarningLevel.isFlagged(asText(x['trangThaiCanhBao']))).length;
    var coMat = 0, diTre = 0, coPhep = 0, vang = 0;
    for (final s in all) {
      coMat += asInt(s['coMat']);
      diTre += asInt(s['diTre']);
      coPhep += asInt(s['coPhep']);
      vang += asInt(s['vang']);
    }
    final recorded = coMat + diTre + coPhep + vang;
    final rate = recorded == 0 ? 0.0 : (coMat + diTre) / recorded;

    final query = _query.trim().toLowerCase();
    final visible = all.where((s) {
      if (_onlyFlagged && !WarningLevel.isFlagged(asText(s['trangThaiCanhBao']))) {
        return false;
      }
      if (query.isEmpty) return true;
      return asText(s['maSinhVien']).toLowerCase().contains(query) ||
          asText(s['hoTen']).toLowerCase().contains(query);
    }).toList();

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        _ClassHeader(lop: lop),
        const SizedBox(height: 12),
        StatGrid(children: [
          StatTile(
            label: 'Tỷ lệ chuyên cần',
            value: recorded == 0 ? '—' : formatRate(rate),
            icon: Icons.insights,
            color: recorded == 0 ? AttendanceStatus.pending : rateColor(rate),
          ),
          StatTile(
            label: 'Buổi đã chốt / tổng',
            value: '$held/${sessions.length}',
            icon: Icons.event_available,
          ),
          StatTile(
            label: 'Sinh viên',
            value: '${all.length}',
            icon: Icons.groups_outlined,
          ),
          StatTile(
            label: 'Cảnh báo / cấm thi',
            value: '$flagged',
            icon: Icons.warning_amber_rounded,
            color: flagged == 0 ? AttendanceStatus.present : AttendanceStatus.absent,
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
        const SizedBox(height: 12),
        TextField(
          decoration: const InputDecoration(
            prefixIcon: Icon(Icons.search),
            hintText: 'Tìm theo mã hoặc tên sinh viên',
            isDense: true,
          ),
          onChanged: (value) => setState(() => _query = value),
        ),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerLeft,
          child: FilterChip(
            label: Text('Chỉ sinh viên bị cảnh báo ($flagged)'),
            selected: _onlyFlagged,
            onSelected: (value) => setState(() => _onlyFlagged = value),
          ),
        ),
        const SizedBox(height: 8),
        if (visible.isEmpty)
          const EmptyCard(message: 'Không có sinh viên phù hợp.')
        else
          for (final s in visible) _StudentCard(student: s),
      ],
    );
  }
}

class _StudentCard extends StatelessWidget {
  const _StudentCard({required this.student});

  final Map<String, dynamic> student;

  @override
  Widget build(BuildContext context) {
    final level = asText(student['trangThaiCanhBao']);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        asText(student['hoTen']),
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      Text(
                        asText(student['maSinhVien']),
                        style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                      ),
                    ],
                  ),
                ),
                Pill(WarningLevel.label(level), color: WarningLevel.color(level)),
              ],
            ),
            const SizedBox(height: 10),
            AttendanceBreakdown(
              coMat: asInt(student['coMat']),
              diTre: asInt(student['diTre']),
              coPhep: asInt(student['coPhep']),
              vang: asInt(student['vang']),
              height: 8,
            ),
          ],
        ),
      ),
    );
  }
}

class _SessionsView extends StatelessWidget {
  const _SessionsView({required this.history});

  final Map<String, dynamic> history;

  @override
  Widget build(BuildContext context) {
    final sessions = (history['buoiHoc'] as List? ?? const [])
        .whereType<Map>()
        .map(Map<String, dynamic>.from)
        .toList()
      ..sort((a, b) => asText(b['ngayHocCuThe']).compareTo(asText(a['ngayHocCuThe'])));

    if (sessions.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: const [EmptyCard(message: 'Lớp chưa có buổi học nào.')],
      );
    }

    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      itemCount: sessions.length,
      itemBuilder: (context, index) {
        final s = sessions[index];
        final date = parseDate(s['ngayHocCuThe']);
        final daChot = s['daChot'] == true;
        final laHomNay = s['laHomNay'] == true;
        final siSo = asInt(s['siSoLop']);
        final present = asInt(s['coMat']) + asInt(s['diTre']);
        final outside = asInt(s['ngoaiKhuonVien']);
        final edits = asInt(s['soLanDieuChinh']);
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        date == null
                            ? asText(s['maBuoiHoc'])
                            : '${dayOfWeekLabel(thuOf(date))}, ${dateFormat.format(date)}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                    if (laHomNay)
                      const Padding(
                        padding: EdgeInsets.only(right: 6),
                        child: Pill('Hôm nay', color: Color(0xFF0867B2)),
                      ),
                    Pill(
                      daChot ? 'Đã chốt' : 'Chưa chốt',
                      color: daChot ? AttendanceStatus.present : AttendanceStatus.pending,
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  'Có mặt $present/$siSo',
                  style: const TextStyle(fontSize: 13, color: Color(0xFF3B4A5A)),
                ),
                const SizedBox(height: 10),
                AttendanceBreakdown(
                  coMat: asInt(s['coMat']),
                  diTre: asInt(s['diTre']),
                  coPhep: asInt(s['coPhep']),
                  vang: asInt(s['vang']),
                  chuaDiemDanh: asInt(s['chuaDiemDanh']),
                  height: 8,
                ),
                if (outside > 0 || edits > 0) ...[
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      if (outside > 0)
                        Pill('$outside ngoài khuôn viên', color: AttendanceStatus.late),
                      if (edits > 0)
                        Pill('$edits lần sửa tay', color: const Color(0xFF6B5BD2)),
                    ],
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}
