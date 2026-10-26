import 'package:flutter/material.dart';

import '../../core/student/student_registration_service.dart';
import '../../core/theme/app_colors.dart';

class StudentCourseRegistrationScreen extends StatefulWidget {
  const StudentCourseRegistrationScreen({super.key});

  @override
  State<StudentCourseRegistrationScreen> createState() =>
      _StudentCourseRegistrationScreenState();
}

class _StudentCourseRegistrationScreenState
    extends State<StudentCourseRegistrationScreen>
    with SingleTickerProviderStateMixin {
  final _service = StudentRegistrationService();
  late final TabController _tabs;
  Map<String, dynamic>? _profile;
  Map<String, dynamic>? _selected;
  List<Map<String, dynamic>> _subjects = [];
  bool _loading = true;
  String? _error;
  final Set<String> _busySections = {};

  static const _types = ['moi', 'vuot', 'lai'];
  static const _labels = ['Theo CTĐT', 'Học vượt', 'Học lại'];

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: _types.length, vsync: this)
      ..addListener(_loadSubjects);
    _loadAll();
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  Future<void> _loadAll() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        _service.getStudentProfile(),
        _service.getSelectedSections(),
        _service.getSubjects(_types[_tabs.index]),
      ]);
      if (!mounted) return;
      setState(() {
        _profile = results[0] as Map<String, dynamic>;
        _selected = results[1] as Map<String, dynamic>;
        _subjects = results[2] as List<Map<String, dynamic>>;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = _message(error);
        _loading = false;
      });
    }
  }

  Future<void> _loadSubjects() async {
    if (_tabs.indexIsChanging) return;
    try {
      final subjects = await _service.getSubjects(_types[_tabs.index]);
      if (mounted) setState(() => _subjects = subjects);
    } catch (error) {
      if (mounted) _showMessage(_message(error));
    }
  }

  Future<void> _register(Map<String, dynamic> section) async {
    final code = '${section['maLopHocPhan'] ?? ''}';
    if (code.isEmpty || _busySections.contains(code)) return;
    setState(() => _busySections.add(code));
    try {
      await _service.register(code, _types[_tabs.index]);
      await _loadAll();
      if (mounted) _showMessage('Đăng ký nhóm học phần thành công.');
    } catch (error) {
      if (mounted) _showMessage(_message(error));
    } finally {
      if (mounted) setState(() => _busySections.remove(code));
    }
  }

  Future<void> _unregister(Map<String, dynamic> section) async {
    final code = '${section['maLopHocPhan'] ?? ''}';
    if (code.isEmpty || _busySections.contains(code)) return;
    setState(() => _busySections.add(code));
    try {
      await _service.unregister(code, _types[_tabs.index]);
      await _loadAll();
      if (mounted) _showMessage('Đã hủy đăng ký nhóm học phần.');
    } catch (error) {
      if (mounted) _showMessage(_message(error));
    } finally {
      if (mounted) setState(() => _busySections.remove(code));
    }
  }

  String _message(Object error) => error is StudentRegistrationException
      ? error.message
      : 'Không thể tải thông tin đăng ký học phần.';

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  int _int(Object? value) =>
      value is num ? value.toInt() : int.tryParse('$value') ?? 0;

  @override
  Widget build(BuildContext context) {
    final profile = _profile;
    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FB),
      appBar: AppBar(
        title: const Text('Đăng ký học phần'),
        backgroundColor: Colors.white,
        foregroundColor: AppColors.textPrimary,
        bottom: TabBar(
          controller: _tabs,
          isScrollable: true,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textSecondary,
          tabs: _labels.map((label) => Tab(text: label)).toList(),
        ),
        actions: [
          IconButton(
            tooltip: 'Tải lại',
            onPressed: _loading ? null : _loadAll,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _errorView()
              : RefreshIndicator(
                  onRefresh: _loadAll,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 28),
                    children: [
                      if (profile != null) _studentSummary(profile),
                      const SizedBox(height: 12),
                      if (_selected != null) _creditSummary(_selected!),
                      const SizedBox(height: 18),
                      Text(
                        _tabs.index == 2
                            ? 'Môn chưa đạt · chọn nhóm đang mở trong học kỳ hiện tại'
                            : 'Môn học đang mở trong học kỳ',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 10),
                      if (_subjects.isEmpty)
                        _emptyState()
                      else
                        ..._subjects.map(_subjectCard),
                      if (_tabs.index == 2)
                        const Padding(
                          padding: EdgeInsets.only(top: 4, bottom: 8),
                          child: Text(
                            'Học lại dùng nhóm mới đang mở của học kỳ hiện tại; không đăng ký lại nhóm thuộc học kỳ cũ.',
                            style: TextStyle(
                                fontSize: 12, color: AppColors.textSecondary),
                          ),
                        ),
                      const SizedBox(height: 16),
                      _selectedCourses(),
                    ],
                  ),
                ),
    );
  }

  Widget _studentSummary(Map<String, dynamic> profile) => Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('HỒ SƠ ĐĂNG KÝ',
                style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary,
                    letterSpacing: .6)),
            const SizedBox(height: 8),
            Text('${profile['hoTen'] ?? 'Sinh viên'}',
                style:
                    const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text(
                'MSSV ${profile['maSinhVien'] ?? '—'}  ·  Khóa ${profile['khoaSo'] ?? '—'}',
                style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 3),
            Text(
                '${profile['nganhHoc'] ?? 'Ngành học'}  ·  Năm thứ ${profile['namThu'] ?? '—'}',
                style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 3),
            Text(
              profile['coChuongTrinhDaoTao'] == true
                  ? 'CTĐT đang áp dụng: K${profile['khoaSo'] ?? '—'} · ${profile['soMonTrongChuongTrinh'] ?? 0} môn'
                  : 'Chưa có CTĐT riêng cho ngành/khóa; hệ thống đang dùng học kỳ chuẩn của môn.',
              style:
                  const TextStyle(fontSize: 12, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 10),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
              decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(10)),
              child: Text(
                  '${profile['tenHocKyHienTai'] ?? profile['hocKyHienTai'] ?? 'Học kỳ hiện tại'}  ·  CTĐT học kỳ ${profile['hocKyChuan'] ?? '—'}',
                  style: const TextStyle(
                      color: Color(0xFF1D4ED8),
                      fontWeight: FontWeight.w700,
                      fontSize: 12)),
            ),
            const SizedBox(height: 8),
            Text(
                profile['dangKyDuocHocVuot'] == true
                    ? 'Học vượt tối đa đến học kỳ ${profile['hocKyHocVuotGoiY'] ?? '—'}; học lại chỉ hiện môn chưa đạt.'
                    : 'Bạn đang ở học kỳ cuối của CTĐT; học lại chỉ hiện môn chưa đạt.',
                style: const TextStyle(
                    fontSize: 12, color: AppColors.textSecondary)),
          ],
        ),
      );

  Widget _creditSummary(Map<String, dynamic> selected) {
    final current = _int(selected['tongSoTinChi']);
    final max = _int(_profile?['tinChiToiDa']);
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE2E8F0))),
      child: Row(children: [
        const Icon(Icons.credit_score_outlined, color: AppColors.primary),
        const SizedBox(width: 10),
        Expanded(
            child:
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('Đã chọn $current${max > 0 ? ' / $max' : ''} tín chỉ',
              style: const TextStyle(fontWeight: FontWeight.w800)),
          Text(
              '${_int(selected['tongSoMon'])} học phần  ·  ${_int(selected['tongSoTiet'])} tiết',
              style: const TextStyle(
                  fontSize: 12, color: AppColors.textSecondary)),
        ])),
      ]),
    );
  }

  Widget _subjectCard(Map<String, dynamic> subject) {
    final groups = (subject['nhomHocPhans'] as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>()
        .toList();
    final selectedCode = '${subject['maLopDaChon'] ?? ''}';
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      color: Colors.white,
      elevation: 0,
      shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFE2E8F0))),
      child: ExpansionTile(
        tilePadding: const EdgeInsets.symmetric(horizontal: 15, vertical: 5),
        childrenPadding: const EdgeInsets.fromLTRB(14, 0, 14, 12),
        title: Text('${subject['tenMonHoc'] ?? 'Môn học'}',
            style: const TextStyle(
                fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
        subtitle: Padding(
            padding: const EdgeInsets.only(top: 5),
            child: Text(
                '${subject['maMonHoc'] ?? ''}  ·  ${_int(subject['tongSoTinChi'])} tín chỉ  ·  ${_int(subject['tongSoTiet'])} tiết  ·  ${groups.length} nhóm',
                style: const TextStyle(
                    fontSize: 12, color: AppColors.textSecondary))),
        trailing: selectedCode.isNotEmpty
            ? const Icon(Icons.check_circle, color: Color(0xFF16A34A))
            : null,
        children:
            groups.map((group) => _groupRow(group, selectedCode)).toList(),
      ),
    );
  }

  Widget _groupRow(Map<String, dynamic> group, String selectedCode) {
    final code = '${group['maLopHocPhan'] ?? ''}';
    final registered = group['daDangKy'] == true || code == selectedCode;
    final capacity = _int(group['siSoToiDa']);
    final enrolled = _int(group['siSoHienTai']);
    final full = _int(group['conTrong']) <= 0;
    final blockedReason = group['lyDoKhongDuDieuKien']?.toString();
    final canRegister = blockedReason == null || blockedReason.isEmpty;
    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
          color: registered ? const Color(0xFFF0FDF4) : const Color(0xFFF8FAFC),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
              color: registered
                  ? const Color(0xFF86EFAC)
                  : const Color(0xFFE2E8F0))),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Expanded(
              child: Text(
                  '${group['tenNhom'] ?? 'Nhóm'}  ·  ${group['maLopHocPhan'] ?? ''}',
                  style: const TextStyle(
                      fontWeight: FontWeight.w800, fontSize: 13))),
          if (registered)
            const Text('Đã chọn',
                style: TextStyle(
                    color: Color(0xFF15803D),
                    fontSize: 12,
                    fontWeight: FontWeight.w700)),
        ]),
        const SizedBox(height: 5),
        Text('GV: ${group['tenGiangVien'] ?? 'Chưa phân công'}',
            style:
                const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
        Text(
            '${group['lichHoc'] ?? 'Chưa có lịch'}  ·  ${group['phongHoc'] ?? 'Chưa xếp phòng'}',
            style:
                const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
        if (!registered && !canRegister) ...[
          const SizedBox(height: 6),
          Text(
            blockedReason,
            style: const TextStyle(fontSize: 12, color: AppColors.danger),
          ),
        ],
        const SizedBox(height: 8),
Row(children: [
            Expanded(
                child: Text(
                    'Sĩ số $enrolled/$capacity  ·  Còn ${_int(group['conTrong'])} chỗ',
                    style: TextStyle(
                        fontSize: 11,
                        color:
                            full ? AppColors.danger : AppColors.textSecondary))),
            if (registered)
              TextButton(
                  onPressed: _busySections.contains(code) ? null : () => _unregister(group),
                  child: _busySections.contains(code)
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Hủy đăng ký'))
            else
              FilledButton.tonal(
                  onPressed: full || !canRegister || _busySections.contains(code)
                      ? null
                      : () => _register(group),
                  child: _busySections.contains(code)
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Chọn nhóm')),
          ]),
      ]),
    );
  }

  Widget _selectedCourses() {
    final courses =
        (_selected?['danhSachHocPhan'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .toList();
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      const Text('Học phần đã đăng ký',
          style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary)),
      const SizedBox(height: 8),
      if (courses.isEmpty)
        const Text('Bạn chưa chọn nhóm học phần nào trong học kỳ này.',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13))
      else
        ...courses.map((course) => ListTile(
              contentPadding: const EdgeInsets.symmetric(horizontal: 12),
              tileColor: Colors.white,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
              title: Text('${course['tenMonHoc'] ?? ''}',
                  style: const TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text(
                  '${course['tenNhom'] ?? ''}  ·  ${course['tongSoTinChi'] ?? 0} tín chỉ  ·  ${course['giangVien'] ?? ''}'),
              trailing: IconButton(
                tooltip: 'Hủy đăng ký nhóm học phần',
                onPressed: () => _unregister(course),
                icon: const Icon(Icons.remove_circle_outline,
                    color: AppColors.danger),
              ),
            )),
    ]);
  }

  Widget _emptyState() => Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: const Color(0xFFE2E8F0))),
        child: Text(
            _tabs.index == 2
                ? 'Không có môn học lại đang mở cho hồ sơ của bạn.'
                : 'Chưa có môn học nào được giáo vụ mở trong danh mục này.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textSecondary)),
      );

  Widget _errorView() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            const Icon(Icons.cloud_off, size: 44, color: AppColors.textMuted),
            const SizedBox(height: 12),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 14),
            OutlinedButton.icon(
                onPressed: _loadAll,
                icon: const Icon(Icons.refresh),
                label: const Text('Thử lại')),
          ]),
        ),
      );
}
