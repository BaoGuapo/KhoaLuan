import 'package:flutter/material.dart';

import '../../core/network/portal_api.dart';
import '../shared/ui.dart';

/// Bảng điểm gọn: điểm chữ đã công bố theo học kỳ, GPA và chuyên cần từng môn.
class ResultsView extends StatefulWidget {
  const ResultsView({super.key});

  @override
  State<ResultsView> createState() => _ResultsViewState();
}

class _ResultsViewState extends State<ResultsView> {
  final _api = PortalApi();

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AsyncPage<Map<String, dynamic>>(
      load: () => _api.getObject('api/v1/me/ket-qua-hoc-tap'),
      builder: (context, data, _) {
        final semesters = (data['hocKy'] as List? ?? const [])
            .whereType<Map>()
            .map(Map<String, dynamic>.from)
            .toList();

        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            StatGrid(children: [
              StatTile(
                label: 'GPA tích lũy (hệ 4)',
                value: _gpa(data['gpaTichLuy']),
                icon: Icons.school_outlined,
                color: _gpaColor(data['gpaTichLuy']),
              ),
              StatTile(
                label: 'Tín chỉ tích lũy',
                value: '${asInt(data['tinChiTichLuy'])}',
                icon: Icons.workspace_premium_outlined,
              ),
            ]),
            if (asInt(data['tinChiChuaCoDiem']) > 0)
              Padding(
                padding: const EdgeInsets.only(top: 8, left: 4),
                child: Text(
                  '${asInt(data['tinChiChuaCoDiem'])} tín chỉ đang học hoặc chưa công bố điểm',
                  style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                ),
              ),
            if (semesters.isEmpty)
              const Padding(
                padding: EdgeInsets.only(top: 12),
                child: EmptyCard(
                  message: 'Chưa có kết quả học tập. Kết quả hiển thị sau khi đăng ký học phần.',
                  icon: Icons.menu_book_outlined,
                ),
              ),
            for (final hk in semesters) _SemesterCard(semester: hk),
          ],
        );
      },
    );
  }
}

String _gpa(Object? value) => value is num ? value.toStringAsFixed(2) : '—';

Color _gpaColor(Object? value) => value is! num
    ? AttendanceStatus.pending
    : value >= 3.2
        ? AttendanceStatus.present
        : value >= 2
            ? AttendanceStatus.excused
            : AttendanceStatus.absent;

class _SemesterCard extends StatelessWidget {
  const _SemesterCard({required this.semester});

  final Map<String, dynamic> semester;

  @override
  Widget build(BuildContext context) {
    final courses = (semester['monHoc'] as List? ?? const [])
        .whereType<Map>()
        .map(Map<String, dynamic>.from)
        .toList();
    final credits = courses.fold<int>(0, (sum, x) => sum + asInt(x['soTinChi']));

    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Card(
        margin: EdgeInsets.zero,
        clipBehavior: Clip.antiAlias,
        child: ExpansionTile(
          initiallyExpanded: true,
          shape: const Border(),
          title: Text(
            asText(semester['tenHocKy'], asText(semester['maHocKy'])),
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
          subtitle: Text(
            '${courses.length} học phần · $credits tín chỉ · '
            'GPA kỳ ${_gpa(semester['gpaHocKy'])}',
            style: const TextStyle(fontSize: 12),
          ),
          children: [
            for (final (i, mon) in courses.indexed) ...[
              if (i > 0) const Divider(height: 1, indent: 16, endIndent: 16),
              _CourseRow(course: mon),
            ],
            Container(
              width: double.infinity,
              color: const Color(0xFFF3F7FB),
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              child: Text(
                'Tín chỉ đạt ${asInt(semester['tinChiDat'])} · '
                'GPA tích lũy ${_gpa(semester['gpaTichLuy'])} · '
                'Tín chỉ tích lũy ${asInt(semester['tinChiTichLuy'])}',
                style: const TextStyle(fontSize: 12, color: Color(0xFF3B4A5A)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CourseRow extends StatelessWidget {
  const _CourseRow({required this.course});

  final Map<String, dynamic> course;

  @override
  Widget build(BuildContext context) {
    final status = asText(course['trangThai']);
    final grade = course['diemChu'] as String?;
    final tally = course['chuyenCan'] is Map
        ? Map<String, dynamic>.from(course['chuyenCan'] as Map)
        : null;
    final gradeColor = switch (status) {
      'Dat' => AttendanceStatus.present,
      'KhongDat' => AttendanceStatus.absent,
      _ => AttendanceStatus.pending,
    };

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
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
                      asText(course['tenMonHoc']),
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${asText(course['maMonHoc'])} · ${asInt(course['soTinChi'])} tín chỉ',
                      style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Container(
                width: 52,
                padding: const EdgeInsets.symmetric(vertical: 6),
                decoration: BoxDecoration(
                  color: gradeColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Column(
                  children: [
                    Text(
                      grade ?? '—',
                      style: TextStyle(
                        color: gradeColor,
                        fontSize: 17,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      // Ưu tiên điểm thang 10 giảng viên nhập; kết quả học vụ chỉ có điểm chữ thì hiện thang 4.
                      course['diemTongKet'] is num
                          ? (course['diemTongKet'] as num).toStringAsFixed(1)
                          : course['diem4'] is num
                              ? 'Hệ 4: ${(course['diem4'] as num).toStringAsFixed(1)}'
                              : 'Chưa có',
                      style: TextStyle(color: gradeColor, fontSize: 10),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (tally != null) ...[
            const SizedBox(height: 8),
            AttendanceBreakdown(
              coMat: asInt(tally['coMat']),
              diTre: asInt(tally['diTre']),
              coPhep: asInt(tally['coPhep']),
              vang: asInt(tally['vang']),
              showLegend: false,
              height: 6,
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Có mặt ${asInt(tally['coMat']) + asInt(tally['diTre'])}/${asInt(tally['tongSoBuoi'])}'
                    ' · Vắng ${asInt(tally['vang'])}',
                    style: const TextStyle(fontSize: 12, color: Color(0xFF3B4A5A)),
                  ),
                ),
                Pill(
                  WarningLevel.label(asText(tally['mucCanhBao'])),
                  color: WarningLevel.color(asText(tally['mucCanhBao'])),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
