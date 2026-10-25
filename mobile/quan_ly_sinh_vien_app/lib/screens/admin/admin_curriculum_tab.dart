import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../core/api/api_constants.dart';
import '../../core/auth/token_storage.dart';
import '../../core/theme/app_colors.dart';

class AdminCurriculumTab extends StatefulWidget {
  const AdminCurriculumTab({super.key});

  @override
  State<AdminCurriculumTab> createState() => _AdminCurriculumTabState();
}

class _AdminCurriculumTabState extends State<AdminCurriculumTab> {
  final _semesterController = TextEditingController(text: '2026-1');
  List<Map<String, dynamic>> _subjects = [];
  List<Map<String, dynamic>> _lecturers = [];
  List<Map<String, dynamic>> _subjectStats = [];
  List<Map<String, dynamic>> _curriculumEntries = [];
  List<Map<String, dynamic>> _rooms = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _semesterController.dispose();
    super.dispose();
  }

  Future<http.Response> _request(String method, String path,
      {Map<String, dynamic>? body}) async {
    final token = await TokenStorage.getAccessToken();
    if (token == null || token.isEmpty || token.startsWith('mock_')) {
      throw const _TrainingException(
          'Đăng nhập bằng tài khoản giáo vụ để tiếp tục.');
    }
    final uri = Uri.parse('${ApiConstants.baseUrl}$path');
    final headers = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
    switch (method) {
      case 'GET':
        return http
            .get(uri, headers: headers)
            .timeout(const Duration(seconds: 12));
      case 'POST':
        return http
            .post(uri, headers: headers, body: jsonEncode(body))
            .timeout(const Duration(seconds: 12));
      case 'PUT':
        return http
            .put(uri, headers: headers, body: jsonEncode(body))
            .timeout(const Duration(seconds: 12));
      default:
        throw ArgumentError.value(method, 'method', 'Unsupported HTTP method');
    }
  }

  Map<String, dynamic> _decodeObject(http.Response response) {
    _ensureSuccess(response);
    final data = jsonDecode(response.body);
    if (data is! Map<String, dynamic>)
      throw const _TrainingException('Dữ liệu máy chủ không hợp lệ.');
    return data;
  }

  void _ensureSuccess(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) return;
    try {
      final data = jsonDecode(response.body);
      if (data is Map<String, dynamic> && data['message'] is String) {
        throw _TrainingException(data['message'] as String);
      }
    } on FormatException {
      // Show a status-based error for non-JSON responses.
    }
    throw _TrainingException(
        'Yêu cầu thất bại (mã lỗi ${response.statusCode}).');
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final semester =
          Uri.encodeQueryComponent(_semesterController.text.trim());
      final responses = await Future.wait([
        _request('GET', '/api/v1/mon-hoc'),
        _request('GET', '/api/v1/giao-vu/thong-ke-dao-tao?maHocKy=$semester'),
        _request('GET', '/api/v1/giao-vu/chuong-trinh-dao-tao'),
        _request('GET', '/api/v1/giao-vu/phong-hoc'),
      ]);
      _ensureSuccess(responses[0]);
      final subjects = jsonDecode(responses[0].body);
      if (subjects is! List)
        throw const _TrainingException('Danh sách môn học không hợp lệ.');
      final stats = _decodeObject(responses[1]);
      _ensureSuccess(responses[2]);
      final curriculum = jsonDecode(responses[2].body);
      if (curriculum is! List)
        throw const _TrainingException('Danh sách CTĐT không hợp lệ.');
      _ensureSuccess(responses[3]);
      final rooms = jsonDecode(responses[3].body);
      if (rooms is! List)
        throw const _TrainingException('Danh sách phòng học không hợp lệ.');
      if (!mounted) return;
      setState(() {
        _subjects = subjects.whereType<Map<String, dynamic>>().toList();
        _lecturers = (stats['thongKeGiangVien'] as List<dynamic>? ?? const [])
            .whereType<Map<String, dynamic>>()
            .toList();
        _subjectStats =
            (stats['thongKeTheoMonHoc'] as List<dynamic>? ?? const [])
                .whereType<Map<String, dynamic>>()
                .toList();
        _curriculumEntries =
            curriculum.whereType<Map<String, dynamic>>().toList();
        _rooms = rooms.whereType<Map<String, dynamic>>().toList();
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is _TrainingException
            ? error.message
            : 'Không thể tải dữ liệu đào tạo.';
        _loading = false;
      });
    }
  }

  Future<void> _createSubject() async {
    final code = TextEditingController();
    final name = TextEditingController();
    final lecture = TextEditingController(text: '2');
    final practice = TextEditingController(text: '0');
    final lecturePeriods = TextEditingController(text: '30');
    final practicePeriods = TextEditingController(text: '0');
    final groups = TextEditingController(text: '10');
    final major = TextEditingController(text: 'Công nghệ thông tin');
    final cohort = TextEditingController(text: '15');
    var curriculumSemester = 1;
    final submit = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
          builder: (context, setDialogState) => AlertDialog(
                title: const Text('Thêm môn theo CTĐT'),
                content: SingleChildScrollView(
                    child: Column(mainAxisSize: MainAxisSize.min, children: [
                  _input(code, 'Mã môn học'),
                  _input(name, 'Tên môn học'),
                  _input(major, 'Ngành đào tạo'),
                  _input(cohort, 'Khóa CTĐT', numeric: true),
                  Row(children: [
                    Expanded(
                        child: _input(lecture, 'Tín chỉ lý thuyết',
                            numeric: true)),
                    const SizedBox(width: 8),
                    Expanded(
                        child: _input(practice, 'Tín chỉ thực hành',
                            numeric: true))
                  ]),
                  Row(children: [
                    Expanded(
                        child: _input(lecturePeriods, 'Tiết lý thuyết',
                            numeric: true)),
                    const SizedBox(width: 8),
                    Expanded(
                        child: _input(practicePeriods, 'Tiết thực hành',
                            numeric: true))
                  ]),
                  DropdownButtonFormField<int>(
                      value: curriculumSemester,
                      decoration:
                          const InputDecoration(labelText: 'Học kỳ chuẩn CTĐT'),
                      items: List.generate(
                          8,
                          (i) => DropdownMenuItem(
                              value: i + 1, child: Text('Học kỳ ${i + 1}'))),
                      onChanged: (value) => setDialogState(
                          () => curriculumSemester = value ?? 1)),
                  _input(groups, 'Số nhóm dự kiến (10–13)', numeric: true),
                ])),
                actions: [
                  TextButton(
                      onPressed: () => Navigator.pop(context, false),
                      child: const Text('Hủy')),
                  FilledButton(
                      onPressed: () => Navigator.pop(context, true),
                      child: const Text('Lưu môn'))
                ],
              )),
    );
    if (submit != true) {
      code.dispose();
      name.dispose();
      lecture.dispose();
      practice.dispose();
      lecturePeriods.dispose();
      practicePeriods.dispose();
      groups.dispose();
      major.dispose();
      cohort.dispose();
      return;
    }
    try {
      final response =
          await _request('POST', '/api/v1/giao-vu/tao-mon-hoc', body: {
        'maMonHoc': code.text.trim(),
        'tenMonHoc': name.text.trim(),
        'soTinChiLyThuyet': int.tryParse(lecture.text) ?? 0,
        'soTinChiThucHanh': int.tryParse(practice.text) ?? 0,
        'soTietLyThuyet': int.tryParse(lecturePeriods.text) ?? 0,
        'soTietThucHanh': int.tryParse(practicePeriods.text) ?? 0,
        'hocKyGoiY': curriculumSemester,
        'soNhomDuKien': int.tryParse(groups.text) ?? 10,
        'nganhHoc': major.text.trim(),
        'khoaSo': int.tryParse(cohort.text) ?? 0,
      });
      _decodeObject(response);
      await _load();
      if (mounted) _message('Đã lưu môn học và thông tin CTĐT.');
    } catch (error) {
      if (mounted)
        _message(error is _TrainingException
            ? error.message
            : 'Không thể tạo môn học.');
    } finally {
      code.dispose();
      name.dispose();
      lecture.dispose();
      practice.dispose();
      lecturePeriods.dispose();
      practicePeriods.dispose();
      groups.dispose();
      major.dispose();
      cohort.dispose();
    }
  }

  Widget _input(TextEditingController controller, String label,
          {bool numeric = false}) =>
      Padding(
        padding: const EdgeInsets.only(top: 8),
        child: TextField(
            controller: controller,
            keyboardType: numeric ? TextInputType.number : TextInputType.text,
            decoration: InputDecoration(
                labelText: label, border: const OutlineInputBorder())),
      );

  Future<void> _openGroups(Map<String, dynamic> subject) async {
    final groups =
        TextEditingController(text: '${subject['soNhomDuKien'] ?? 10}');
    final capacity = TextEditingController(text: '45');
    final submit = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Mở nhóm · ${subject['tenMonHoc'] ?? ''}'),
        content: Column(mainAxisSize: MainAxisSize.min, children: [
          TextField(
              controller: _semesterController,
              decoration:
                  const InputDecoration(labelText: 'Mã học kỳ (VD: 2026-1)')),
          _input(groups, 'Số nhóm (thường 10–13)', numeric: true),
          _input(capacity, 'Sĩ số tối đa mỗi nhóm', numeric: true),
          const Padding(
              padding: EdgeInsets.only(top: 10),
              child: Text(
                  'Nhóm được tạo ở trạng thái chưa phân công giảng viên và chưa xếp lịch.',
                  style:
                      TextStyle(fontSize: 12, color: AppColors.textSecondary))),
        ]),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy')),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Mở nhóm'))
        ],
      ),
    );
    if (submit != true) return;
    try {
      final response =
          await _request('POST', '/api/v1/giao-vu/mo-nhom-mon-hoc', body: {
        'maMonHoc': subject['maMonHoc'],
        'maHocKy': _semesterController.text.trim(),
        'soLuongNhom': int.tryParse(groups.text) ?? 10,
        'siSoToiDa': int.tryParse(capacity.text) ?? 45,
      });
      final result = _decodeObject(response);
      await _load();
      if (mounted)
        _message(result['message']?.toString() ?? 'Đã mở nhóm học phần.');
    } catch (error) {
      if (mounted)
        _message(error is _TrainingException
            ? error.message
            : 'Không thể mở nhóm học phần.');
    } finally {
      groups.dispose();
      capacity.dispose();
    }
  }

  Future<void> _assignLecturer(Map<String, dynamic> section) async {
    final selected = await showDialog<String>(
      context: context,
      builder: (context) => SimpleDialog(
        title: Text('Phân công ${section['tenNhom'] ?? 'nhóm'}'),
        children: _lecturers
            .map((lecturer) => SimpleDialogOption(
                  onPressed: () => Navigator.pop(
                      context, lecturer['maGiangVien']?.toString()),
                  child: Text(
                      '${lecturer['hoTen'] ?? ''} · ${lecturer['maGiangVien'] ?? ''}'),
                ))
            .toList(),
      ),
    );
    if (selected == null) return;
    try {
      final response =
          await _request('PUT', '/api/v1/giao-vu/phan-cong-giang-vien', body: {
        'maLopHocPhan': section['maLopHocPhan'],
        'maGiangVien': selected,
      });
      _decodeObject(response);
      await _load();
      if (mounted) _message('Đã phân công giảng viên.');
    } catch (error) {
      if (mounted)
        _message(error is _TrainingException
            ? error.message
            : 'Không thể phân công giảng viên.');
    }
  }

  Future<void> _assignCurriculum(Map<String, dynamic> subject) async {
      final major = TextEditingController(text: 'Công nghệ thông tin');
      final cohort = TextEditingController(text: '15');
      var term = _number(subject['hocKyChuan']).clamp(1, 8).toInt();
      final submit = await showDialog<bool>(
        context: context,
        builder: (context) => StatefulBuilder(
          builder: (context, setDialogState) => AlertDialog(
            title: Text('Gán môn vào CTĐT · ${subject['tenMonHoc'] ?? ''}'),
            content: Column(mainAxisSize: MainAxisSize.min, children: [
              _input(major, 'Ngành đào tạo'),
              _input(cohort, 'Khóa CTĐT', numeric: true),
              DropdownButtonFormField<int>(
                value: term,
                decoration: const InputDecoration(labelText: 'Học kỳ trong CTĐT'),
                items: List.generate(8, (index) => DropdownMenuItem(
                    value: index + 1, child: Text('Học kỳ ${index + 1}'))),
                onChanged: (value) => setDialogState(() => term = value ?? term),
              ),
            ]),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: const Text('Hủy')),
              FilledButton(
                  onPressed: () => Navigator.pop(context, true),
                  child: const Text('Lưu CTĐT')),
            ],
          ),
        ),
      );
      if (submit == true) {
        try {
          final response = await _request(
            'PUT',
            '/api/v1/giao-vu/chuong-trinh-dao-tao/mon-hoc',
            body: {
              'nganhHoc': major.text.trim(),
              'khoaSo': int.tryParse(cohort.text) ?? 0,
              'maMonHoc': subject['maMonHoc'],
              'hocKy': term,
            },
          );
          _decodeObject(response);
          await _load();
          if (mounted) _message('Đã cập nhật chương trình đào tạo.');
        } catch (error) {
          if (mounted) {
            _message(error is _TrainingException
                ? error.message
                : 'Không thể cập nhật chương trình đào tạo.');
          }
        }
      }
      major.dispose();
      cohort.dispose();
  }

  Future<void> _setSchedule(Map<String, dynamic> section) async {
    if (_rooms.isEmpty) {
      _message('Chưa có phòng học được khai báo trong hệ thống.');
      return;
    }
    final start = TextEditingController(text: '1');
    final end = TextEditingController(text: '3');
    var roomCode = _rooms.first['maPhong'].toString();
    var day = 2;
    final submit = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
          builder: (context, setDialogState) => AlertDialog(
                title: Text('Xếp lịch · ${section['tenNhom'] ?? 'nhóm'}'),
                content: Column(mainAxisSize: MainAxisSize.min, children: [
                  DropdownButtonFormField<String>(
                      value: roomCode,
                      decoration: const InputDecoration(labelText: 'Phòng học'),
                      items: _rooms
                          .map((room) => DropdownMenuItem(
                              value: room['maPhong'].toString(),
                              child: Text(
                                  '${room['tenPhong'] ?? room['maPhong']}')))
                          .toList(),
                      onChanged: (value) =>
                          setDialogState(() => roomCode = value ?? roomCode)),
                  DropdownButtonFormField<int>(
                      value: day,
                      decoration:
                          const InputDecoration(labelText: 'Thứ trong tuần'),
                      items: List.generate(
                          7,
                          (index) => DropdownMenuItem(
                              value: index + 2,
                              child: Text(index == 6
                                  ? 'Chủ nhật'
                                  : 'Thứ ${index + 2}'))),
                      onChanged: (value) =>
                          setDialogState(() => day = value ?? 2)),
                  Row(children: [
                    Expanded(
                        child: _input(start, 'Tiết bắt đầu', numeric: true)),
                    const SizedBox(width: 8),
                    Expanded(child: _input(end, 'Tiết kết thúc', numeric: true))
                  ]),
                ]),
                actions: [
                  TextButton(
                      onPressed: () => Navigator.pop(context, false),
                      child: const Text('Hủy')),
                  FilledButton(
                      onPressed: () => Navigator.pop(context, true),
                      child: const Text('Lưu lịch'))
                ],
              )),
    );
    if (submit == true) {
      try {
        final response =
            await _request('PUT', '/api/v1/giao-vu/lich-nhom', body: {
          'maLopHocPhan': section['maLopHocPhan'],
          'maPhong': roomCode,
          'thuTrongTuan': day,
          'tietBatDau': int.tryParse(start.text) ?? 1,
          'tietKetThuc': int.tryParse(end.text) ?? 3,
        });
        final result = _decodeObject(response);
        await _load();
        if (mounted)
          _message(result['message']?.toString() ?? 'Đã cập nhật lịch.');
      } catch (error) {
        if (mounted)
          _message(error is _TrainingException
              ? error.message
              : 'Không thể lưu lịch học.');
      }
    }
    start.dispose();
    end.dispose();
  }

  void _message(String value) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(value)));
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: const Color(0xFFF8FAFC),
        appBar: AppBar(
            title: const Text('CTĐT & mở lớp học phần'),
            backgroundColor: Colors.white,
            foregroundColor: AppColors.textPrimary,
            actions: [
              IconButton(onPressed: _load, icon: const Icon(Icons.refresh))
            ]),
        floatingActionButton: FloatingActionButton.extended(
            onPressed: _createSubject,
            icon: const Icon(Icons.add),
            label: const Text('Thêm môn')),
        body: Column(children: [
          Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Row(children: [
                Expanded(
                    child: TextField(
                        controller: _semesterController,
                        decoration: const InputDecoration(
                            labelText: 'Học kỳ quản lý', hintText: '2026-1'))),
                const SizedBox(width: 8),
                IconButton(onPressed: _load, icon: const Icon(Icons.search))
              ])),
          if (_loading) const LinearProgressIndicator(minHeight: 2),
          Expanded(
              child: _error != null
                  ? Center(
                      child: Padding(
                          padding: const EdgeInsets.all(24),
                          child:
                              Column(mainAxisSize: MainAxisSize.min, children: [
                            Text(_error!, textAlign: TextAlign.center),
                            TextButton(
                                onPressed: _load, child: const Text('Thử lại'))
                          ])))
                  : _loading
                      ? const SizedBox.shrink()
                      : RefreshIndicator(
                          onRefresh: _load,
                          child: ListView(
                              padding: const EdgeInsets.fromLTRB(16, 8, 16, 90),
                              children: [
                                _statistics(),
                                const SizedBox(height: 14),
                                ..._subjects.map(_subjectCard),
                              ]))),
        ]),
      );

  Widget _statistics() {
    final lecturers = _lecturers.length;
    final groups = _subjectStats.fold<int>(
        0, (total, subject) => total + _number(subject['soLuongNhom']));
    return Container(
        padding: const EdgeInsets.all(15),
        decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(15),
            border: Border.all(color: const Color(0xFFE2E8F0))),
        child: Column(children: [
          Row(children: [
            Expanded(child: _metric('$lecturers', 'Giảng viên')),
            Expanded(child: _metric('${_subjects.length}', 'Môn học')),
            Expanded(child: _metric('$groups', 'Nhóm học kỳ'))
          ]),
          if (_lecturers.isNotEmpty) ...[
            const Divider(height: 22),
            ..._lecturers.map((lecturer) {
              final assigned =
                  (lecturer['nhomDangDay'] as List<dynamic>? ?? const [])
                      .whereType<Map<String, dynamic>>()
                      .toList();
              return ExpansionTile(
                tilePadding: EdgeInsets.zero,
                title: Text(
                    '${lecturer['hoTen'] ?? ''} · ${lecturer['maGiangVien'] ?? ''}'),
                subtitle: Text(
                    '${_number(lecturer['soNhomPhanCong'])} nhóm · ${_number(lecturer['tongSoTietDay'])} tiết'),
                children: assigned.isEmpty
                    ? const [
                        ListTile(
                            dense: true,
                            title: Text('Chưa được phân công nhóm nào.'))
                      ]
                    : assigned
                        .map((section) => ListTile(
                              dense: true,
                              title: Text(
                                  '${section['tenMonHoc'] ?? ''} · ${section['tenNhom'] ?? ''}'),
                              subtitle: Text(
                                  '${_number(section['siSo'])} sinh viên'),
                            ))
                        .toList(),
              );
            }),
          ],
        ]));
  }

  Widget _metric(String number, String label) => Column(children: [
        Text(number,
            style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.primary)),
        Text(label,
            style:
                const TextStyle(fontSize: 11, color: AppColors.textSecondary))
      ]);

  Widget _subjectCard(Map<String, dynamic> subject) {
    Map<String, dynamic>? stats;
    for (final item in _subjectStats) {
      if (item['maMonHoc'] == subject['maMonHoc']) {
        stats = item;
        break;
      }
    }
    final groups = (stats?['danhSachNhom'] as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>()
        .toList();
    return Card(
        margin: const EdgeInsets.only(bottom: 10),
        color: Colors.white,
        child: ExpansionTile(
          title: Text('${subject['tenMonHoc'] ?? ''}',
              style: const TextStyle(fontWeight: FontWeight.w800)),
          subtitle: Text(
              '${subject['maMonHoc'] ?? ''}  ·  HK CTĐT ${subject['hocKyChuan'] ?? 1}  ·  ${subject['tongSoTinChi'] ?? 0} TC  ·  ${subject['tongSoTiet'] ?? 0} tiết  ·  Dự kiến ${subject['soNhomDuKien'] ?? 10}, đang mở ${groups.length} nhóm'),
          childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
          children: [
            if (_curriculumEntries.any(
                (entry) => entry['maMonHoc'] == subject['maMonHoc']))
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    _curriculumEntries
                        .where((entry) =>
                            entry['maMonHoc'] == subject['maMonHoc'])
                        .map((entry) =>
                            '${entry['nganhHoc']} · K${entry['khoaSo']} · HK${entry['hocKy']}')
                        .join('  |  '),
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.textSecondary),
                  ),
                ),
              ),
            Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                    onPressed: () => _assignCurriculum(subject),
                    icon: const Icon(Icons.account_tree_outlined),
                    label: const Text('Gán/sửa CTĐT theo ngành, khóa'))),
            Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                    onPressed: () => _openGroups(subject),
                    icon: const Icon(Icons.add_circle_outline),
                    label: const Text('Mở nhóm cho học kỳ này'))),
            if (groups.isEmpty)
              const Padding(
                  padding: EdgeInsets.all(10),
                  child: Align(
                      alignment: Alignment.centerLeft,
                      child: Text('Chưa mở nhóm trong học kỳ đang chọn.',
                          style: TextStyle(color: AppColors.textSecondary))))
            else
              ...groups.map((group) {
                final students =
                    (group['sinhViens'] as List<dynamic>? ?? const [])
                        .whereType<Map<String, dynamic>>()
                        .toList();
                return ExpansionTile(
                  tilePadding: const EdgeInsets.symmetric(horizontal: 8),
                  title: Text(
                      '${group['tenNhom'] ?? ''} · ${group['maLopHocPhan'] ?? ''}'),
                  subtitle: Text(
                      'GV: ${group['giangVien'] ?? 'Chưa phân công'}  ·  Sĩ số ${group['siSoHienTai'] ?? 0}/${group['siSoToiDa'] ?? 0}  ·  ${group['lichHoc'] ?? 'Chưa xếp lịch'}'),
                  trailing: Wrap(children: [
                    IconButton(
                        tooltip: 'Xếp lịch, phòng học',
                        onPressed: () => _setSchedule(group),
                        icon: const Icon(Icons.edit_calendar_outlined,
                            color: AppColors.primary)),
                    IconButton(
                        tooltip: 'Phân công giảng viên',
                        onPressed: _lecturers.isEmpty
                            ? null
                            : () => _assignLecturer(group),
                        icon: const Icon(Icons.person_add_alt_1_outlined,
                            color: AppColors.primary)),
                  ]),
                  children: [
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        child: Text(
                            'Sinh viên tham gia (${students.length})',
                            style: const TextStyle(
                                fontWeight: FontWeight.w700,
                                color: AppColors.textSecondary)),
                      ),
                    ),
                    if (students.isEmpty)
                      const ListTile(
                          dense: true,
                          title: Text('Chưa có sinh viên đăng ký nhóm này.'))
                    else
                      ...students.map((student) => ListTile(
                            dense: true,
                            leading: const Icon(Icons.person_outline),
                            title: Text(
                                '${student['hoTen'] ?? 'Sinh viên'} · ${student['maSinhVien'] ?? ''}'),
                            subtitle: Text(
                                '${student['lopSinhHoat'] ?? ''} · ${student['khoaHoc'] ?? ''}'),
                          )),
                  ],
                );
              }),
          ],
        ));
  }

  int _number(Object? value) =>
      value is num ? value.toInt() : int.tryParse('$value') ?? 0;
}

class _TrainingException implements Exception {
  const _TrainingException(this.message);
  final String message;
}
