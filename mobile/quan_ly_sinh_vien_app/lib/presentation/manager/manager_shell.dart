import 'package:flutter/material.dart';

import '../../core/network/portal_api.dart';
import '../auth/auth_controller.dart';
import '../shared/account_tab.dart';
import '../shared/class_detail_screen.dart';
import '../shared/role_shell.dart';
import '../shared/ui.dart';

/// Màn hình Admin và Cán bộ quản lý: xem báo cáo chuyên cần theo học kỳ.
class ManagerShell extends StatefulWidget {
  const ManagerShell({required this.controller, super.key});

  final AuthController controller;

  @override
  State<ManagerShell> createState() => _ManagerShellState();
}

class _ManagerShellState extends State<ManagerShell> {
  final _api = PortalApi();
  List<Map<String, dynamic>> _semesters = const [];
  String? _semester;
  Map<String, dynamic>? _report;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadSemesters();
  }

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  Future<void> _loadSemesters() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final semesters = await _api.getList('api/v1/hoc-ky');
      if (!mounted) return;
      _semesters = semesters;
      _semester = _currentSemester(semesters);
      await _loadReport();
    } catch (error) {
      if (mounted) {
        setState(() {
          _error = error;
          _loading = false;
        });
      }
    }
  }

  /// Ưu tiên học kỳ đang diễn ra; nếu không có thì lấy học kỳ mới nhất.
  static String? _currentSemester(List<Map<String, dynamic>> semesters) {
    final today = DateTime.now();
    for (final s in semesters) {
      final from = parseDate(s['ngayBatDau']);
      final to = parseDate(s['ngayKetThuc']);
      if (from != null && to != null && !today.isBefore(from) &&
          !today.isAfter(to.add(const Duration(days: 1)))) {
        return asText(s['maHocKy']);
      }
    }
    return semesters.isEmpty ? null : asText(semesters.first['maHocKy']);
  }

  Future<void> _loadReport() async {
    final semester = _semester;
    if (semester == null) {
      setState(() {
        _report = null;
        _loading = false;
      });
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final report = await _api.getObject(
        'api/v1/bao-cao/thong-ke-diem-danh',
        query: {'maHocKy': semester},
      );
      if (!mounted || semester != _semester) return;
      setState(() {
        _report = report;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error;
        _loading = false;
      });
    }
  }

  void _selectSemester(String? value) {
    if (value == null || value == _semester) return;
    setState(() => _semester = value);
    _loadReport();
  }

  Widget _content(Widget Function(Map<String, dynamic> report) builder) {
    final report = _report;
    if (report == null) {
      if (_loading) return const Center(child: CircularProgressIndicator());
      if (_semesters.isEmpty && _error == null) {
        return const MessageView(message: 'Chưa có học kỳ nào trong hệ thống.');
      }
      return MessageView(
        icon: Icons.cloud_off_outlined,
        message: _error?.toString() ?? 'Không có dữ liệu.',
        actionLabel: 'Thử lại',
        onAction: _semesters.isEmpty ? _loadSemesters : _loadReport,
      );
    }
    return Stack(
      children: [
        RefreshIndicator(onRefresh: _loadReport, child: builder(report)),
        if (_loading)
          const Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: LinearProgressIndicator(minHeight: 2),
          ),
      ],
    );
  }

  Widget _semesterPicker() {
    // Hai tab dùng chung học kỳ đang chọn; key theo học kỳ để tab còn lại cập nhật theo.
    return DropdownButtonFormField<String>(
      key: ValueKey(_semester),
      initialValue: _semester,
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Học kỳ',
        prefixIcon: Icon(Icons.date_range),
        isDense: true,
      ),
      items: [
        for (final s in _semesters)
          DropdownMenuItem(
            value: asText(s['maHocKy']),
            child: Text(asText(s['tenHocKy'], asText(s['maHocKy']))),
          ),
      ],
      onChanged: _loading ? null : _selectSemester,
    );
  }

  @override
  Widget build(BuildContext context) {
    return RoleShell(
      tabs: [
        ShellTab(
          label: 'Tổng quan',
          icon: Icons.insights_outlined,
          selectedIcon: Icons.insights,
          page: TabPage(
            title: 'Báo cáo chuyên cần',
            body: _content((report) => _OverviewView(
                  report: report,
                  header: _semesterPicker(),
                )),
          ),
        ),
        ShellTab(
          label: 'Lớp học phần',
          icon: Icons.class_outlined,
          selectedIcon: Icons.class_,
          page: TabPage(
            title: 'Lớp học phần',
            body: _content((report) => _ClassesView(
                  report: report,
                  header: _semesterPicker(),
                )),
          ),
        ),
        ShellTab(
          label: 'Tài khoản',
          icon: Icons.person_outline,
          selectedIcon: Icons.person,
          page: TabPage(
            title: 'Tài khoản',
            body: AccountTab(controller: widget.controller),
          ),
        ),
      ],
    );
  }
}

List<Map<String, dynamic>> _rows(Object? value) => (value as List? ?? const [])
    .whereType<Map>()
    .map(Map<String, dynamic>.from)
    .toList();

void _openClass(BuildContext context, Map<String, dynamic> row) {
  Navigator.of(context).push(MaterialPageRoute<void>(
    builder: (_) => ClassDetailScreen(
      maLopHocPhan: asText(row['maLopHocPhan']),
      title: asText(row['tenMonHoc'], asText(row['maLopHocPhan'])),
    ),
  ));
}

class _OverviewView extends StatelessWidget {
  const _OverviewView({required this.report, required this.header});

  final Map<String, dynamic> report;
  final Widget header;

  @override
  Widget build(BuildContext context) {
    final overview = Map<String, dynamic>.from(report['tongQuan'] as Map? ?? {});
    final subjects = _rows(report['theoMonHoc']);
    final classes = _rows(report['theoLopHocPhan']);
    final days = _rows(report['theoNgay'])
        .where((x) => asInt(x['soLuotDuKien']) > 0)
        .map((x) => (parseDate(x['ngay']), asDouble(x['tyLeChuyenCan'])))
        .where((x) => x.$1 != null)
        .map((x) => (x.$1!, x.$2))
        .toList()
      ..sort((a, b) => a.$1.compareTo(b.$1));

    final recorded = asInt(overview['coMat']) +
        asInt(overview['diTre']) +
        asInt(overview['coPhep']) +
        asInt(overview['vang']);
    final rate = asDouble(overview['tyLeChuyenCan']);
    final flaggedStudents =
        classes.fold<int>(0, (sum, x) => sum + asInt(x['soSinhVienCanhBao']));
    // Backend đã sắp lớp có tỷ lệ thấp lên đầu; bỏ các lớp chưa có số liệu.
    final weakest = classes.where((x) => asInt(x['soLuotDuKien']) > 0).take(5).toList();

    final from = parseDate(report['ngayBatDau']);
    final to = parseDate(report['ngayKetThuc']);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        header,
        if (from != null && to != null)
          Padding(
            padding: const EdgeInsets.only(top: 6, left: 4),
            child: Text(
              '${dateFormat.format(from)} – ${dateFormat.format(to)}',
              style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
            ),
          ),
        const SizedBox(height: 14),
        StatGrid(children: [
          StatTile(
            label: 'Tỷ lệ chuyên cần',
            value: recorded == 0 ? '—' : formatRate(rate),
            icon: Icons.insights,
            color: recorded == 0 ? AttendanceStatus.pending : rateColor(rate),
          ),
          StatTile(
            label: 'Buổi đã diễn ra',
            value: '${asInt(overview['soBuoiDaDienRa'])}/${asInt(overview['tongSoBuoi'])}',
            icon: Icons.event_available,
          ),
          StatTile(
            label: 'Sinh viên',
            value: '${asInt(overview['soSinhVien'])}',
            icon: Icons.groups_outlined,
          ),
          StatTile(
            label: 'Lớp học phần',
            value: '${asInt(overview['soLopHocPhan'])}',
            icon: Icons.class_outlined,
          ),
          StatTile(
            label: 'Môn học',
            value: '${asInt(overview['soMonHoc'])}',
            icon: Icons.menu_book_outlined,
          ),
          StatTile(
            label: 'Lượt SV bị cảnh báo',
            value: '$flaggedStudents',
            icon: Icons.warning_amber_rounded,
            color: flaggedStudents == 0
                ? AttendanceStatus.present
                : AttendanceStatus.absent,
          ),
        ]),
        const SectionTitle('Phân bố điểm danh'),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: AttendanceBreakdown(
              coMat: asInt(overview['coMat']),
              diTre: asInt(overview['diTre']),
              coPhep: asInt(overview['coPhep']),
              vang: asInt(overview['vang']),
              chuaDiemDanh: asInt(overview['chuaDiemDanh']),
            ),
          ),
        ),
        const SectionTitle('Chuyên cần theo ngày'),
        Card(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 16, 12, 12),
            child: RateBarChart(points: days),
          ),
        ),
        const SectionTitle('Lớp cần chú ý'),
        if (weakest.isEmpty)
          const EmptyCard(message: 'Chưa có lớp nào có số liệu điểm danh.')
        else
          for (final row in weakest) _ClassCard(row: row),
        const SectionTitle('Theo môn học'),
        if (subjects.isEmpty)
          const EmptyCard(message: 'Học kỳ chưa có môn học.')
        else
          Card(
            child: Column(
              children: [
                for (final (i, s) in subjects.indexed) ...[
                  if (i > 0) const Divider(height: 1),
                  _SubjectRow(row: s),
                ],
              ],
            ),
          ),
        const SizedBox(height: 16),
      ],
    );
  }
}

class _SubjectRow extends StatelessWidget {
  const _SubjectRow({required this.row});

  final Map<String, dynamic> row;

  @override
  Widget build(BuildContext context) {
    final hasData = asInt(row['soLuotDuKien']) > 0;
    final rate = asDouble(row['tyLeChuyenCan']);
    return Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  asText(row['tenMonHoc']),
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ),
              Pill(
                hasData ? formatRate(rate) : 'Chưa có số liệu',
                color: hasData ? rateColor(rate) : AttendanceStatus.pending,
              ),
            ],
          ),
          const SizedBox(height: 2),
          Text(
            '${asText(row['maMonHoc'])} · ${asInt(row['soLop'])} lớp · '
            '${asInt(row['soSinhVien'])} sinh viên',
            style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
          ),
          const SizedBox(height: 8),
          AttendanceBreakdown(
            coMat: asInt(row['coMat']),
            diTre: asInt(row['diTre']),
            coPhep: asInt(row['coPhep']),
            vang: asInt(row['vang']),
            showLegend: false,
            height: 6,
          ),
        ],
      ),
    );
  }
}

class _ClassCard extends StatelessWidget {
  const _ClassCard({required this.row});

  final Map<String, dynamic> row;

  @override
  Widget build(BuildContext context) {
    final hasData = asInt(row['soLuotDuKien']) > 0;
    final rate = asDouble(row['tyLeChuyenCan']);
    final flagged = asInt(row['soSinhVienCanhBao']);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => _openClass(context, row),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      asText(row['tenMonHoc']),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                  Pill(
                    hasData ? formatRate(rate) : 'Chưa có số liệu',
                    color: hasData ? rateColor(rate) : AttendanceStatus.pending,
                  ),
                ],
              ),
              const SizedBox(height: 2),
              Text(
                '${asText(row['maLopHocPhan'])} · ${asText(row['hoTenGiangVien'])}',
                style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
              ),
              const SizedBox(height: 8),
              AttendanceBreakdown(
                coMat: asInt(row['coMat']),
                diTre: asInt(row['diTre']),
                coPhep: asInt(row['coPhep']),
                vang: asInt(row['vang']),
                showLegend: false,
                height: 6,
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.groups_outlined, size: 15, color: Color(0xFF5B6B7C)),
                  const SizedBox(width: 4),
                  Text('${asInt(row['siSo'])} SV', style: const TextStyle(fontSize: 12)),
                  const SizedBox(width: 12),
                  const Icon(Icons.event_available, size: 15, color: Color(0xFF5B6B7C)),
                  const SizedBox(width: 4),
                  Text('${asInt(row['soBuoiDaDienRa'])} buổi',
                      style: const TextStyle(fontSize: 12)),
                  const Spacer(),
                  if (flagged > 0)
                    Pill('$flagged cảnh báo', color: AttendanceStatus.absent),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

enum _ClassSort { lowestRate, mostFlagged, code }

class _ClassesView extends StatefulWidget {
  const _ClassesView({required this.report, required this.header});

  final Map<String, dynamic> report;
  final Widget header;

  @override
  State<_ClassesView> createState() => _ClassesViewState();
}

class _ClassesViewState extends State<_ClassesView> {
  String _query = '';
  _ClassSort _sort = _ClassSort.lowestRate;

  @override
  Widget build(BuildContext context) {
    final query = _query.trim().toLowerCase();
    final classes = _rows(widget.report['theoLopHocPhan']).where((row) {
      if (query.isEmpty) return true;
      return [row['maLopHocPhan'], row['tenMonHoc'], row['hoTenGiangVien'], row['maMonHoc']]
          .any((x) => asText(x).toLowerCase().contains(query));
    }).toList();

    switch (_sort) {
      case _ClassSort.lowestRate:
        break; // Thứ tự mặc định của backend.
      case _ClassSort.mostFlagged:
        classes.sort((a, b) =>
            asInt(b['soSinhVienCanhBao']).compareTo(asInt(a['soSinhVienCanhBao'])));
      case _ClassSort.code:
        classes.sort((a, b) =>
            asText(a['maLopHocPhan']).compareTo(asText(b['maLopHocPhan'])));
    }

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        widget.header,
        const SizedBox(height: 10),
        TextField(
          decoration: const InputDecoration(
            prefixIcon: Icon(Icons.search),
            hintText: 'Tìm lớp, môn học, giảng viên',
            isDense: true,
          ),
          onChanged: (value) => setState(() => _query = value),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          children: [
            for (final (sort, label) in const [
              (_ClassSort.lowestRate, 'Chuyên cần thấp'),
              (_ClassSort.mostFlagged, 'Nhiều cảnh báo'),
              (_ClassSort.code, 'Mã lớp'),
            ])
              ChoiceChip(
                label: Text(label),
                selected: _sort == sort,
                onSelected: (_) => setState(() => _sort = sort),
              ),
          ],
        ),
        const SizedBox(height: 8),
        if (classes.isEmpty)
          const EmptyCard(message: 'Không có lớp học phần phù hợp.')
        else
          for (final row in classes) _ClassCard(row: row),
      ],
    );
  }
}
