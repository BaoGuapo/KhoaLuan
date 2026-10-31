import 'dart:async';
import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../../core/attendance/attendance_workflow_api.dart';
import 'face_profile_review_screen.dart';
import '../../core/constants/role_names.dart';

class AttendanceManagerScreen extends StatefulWidget {
  const AttendanceManagerScreen({super.key, this.roles = const []});
  final List<String> roles;
  @override
  State<AttendanceManagerScreen> createState() =>
      _AttendanceManagerScreenState();
}

class _AttendanceManagerScreenState extends State<AttendanceManagerScreen> {
  final _api = AttendanceWorkflowApi();
  List<dynamic> _lessons = [], _results = [], _profiles = [];
  String? _lesson, _sessionId, _message;
  Map<String, dynamic>? _qr;
  Timer? _poll;
  bool _busy = false, _polling = false;
  int _minutes = 10;
  bool get _canApprove => widget.roles.any(
    (role) => role == RoleNames.admin || role == RoleNames.canBoQuanLy,
  );
  @override
  void initState() {
    super.initState();
    unawaited(_run(_loadLessons));
  }

  Future<void> _loadLessons() async {
    final rows = await _api.request('buoi-hoc') as List;
    if (!mounted) return;
    setState(() {
      _lessons = rows;
      if (!_lessons.any((x) => x['maBuoiHoc'] == _lesson)) {
        _lesson =
            rows.where((x) => x['daChot'] != true).firstOrNull?['maBuoiHoc']
                as String?;
      }
    });
  }

  Future<void> _run(Future<void> Function() action) async {
    if (_busy) return;
    setState(() {
      _busy = true;
      _message = null;
    });
    try {
      await action();
    } catch (e) {
      if (mounted) setState(() => _message = e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _refreshSession() async {
    final id = _sessionId;
    if (id == null || _polling) return;
    _polling = true;
    try {
      final value = await _api.request('phien/$id/qr') as Map;
      final rows = await _api.request('phien/$id/ket-qua') as List;
      if (mounted && _sessionId == id) {
        setState(() {
          _qr = Map<String, dynamic>.from(value);
          _results = rows;
        });
      }
    } catch (e) {
      _poll?.cancel();
      if (mounted && _sessionId == id) {
        setState(() {
          _qr = null;
          _message = e.toString();
        });
      }
    } finally {
      _polling = false;
    }
  }

  Future<void> _showSession(String id) async {
    _poll?.cancel();
    _sessionId = id;
    await _refreshSession();
    if (_qr != null) {
      _poll = Timer.periodic(
        const Duration(seconds: 15),
        (_) => unawaited(_refreshSession()),
      );
    }
  }

  Future<void> _loadProfiles() async {
    final rows = await _api.request('ho-so/cho-duyet') as List;
    if (mounted) setState(() => _profiles = rows);
  }

  @override
  void dispose() {
    _poll?.cancel();
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Quản lý điểm danh QR')),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        if (_busy) const LinearProgressIndicator(),
        if (_message != null)
          Padding(padding: const EdgeInsets.all(12), child: Text(_message!)),
        const Text('Buổi học trong 7 ngày trước đến 14 ngày tới'),
        DropdownButtonFormField<String>(
          initialValue: _lesson,
          isExpanded: true,
          items: _lessons
              .map(
                (x) => DropdownMenuItem<String>(
                  value: x['maBuoiHoc'] as String,
                  enabled: x['daChot'] != true,
                  child: Text(
                    '${x['ngayHoc'].toString().split('T').first} · ${x['maLopHocPhan']} · ${x['tenMonHoc']}${x['daChot'] == true ? ' · Đã chốt' : ''}',
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              )
              .toList(),
          onChanged: _busy ? null : (v) => setState(() => _lesson = v),
        ),
        DropdownButtonFormField<int>(
          initialValue: _minutes,
          decoration: const InputDecoration(labelText: 'Thời gian mở phiên'),
          items: [5, 10, 15, 20, 30]
              .map((m) => DropdownMenuItem(value: m, child: Text('$m phút')))
              .toList(),
          onChanged: (v) => setState(() => _minutes = v ?? 10),
        ),
        const SizedBox(height: 12),
        ElevatedButton(
          onPressed: _busy || _lesson == null
              ? null
              : () => _run(() async {
                  final value =
                      await _api.request(
                            'buoi-hoc/${Uri.encodeComponent(_lesson!)}/mo',
                            body: {'minutes': _minutes},
                          )
                          as Map;
                  await _showSession(value['sessionId'] as String);
                  await _loadLessons();
                }),
          child: const Text('Mở phiên mới'),
        ),
        TextButton(
          onPressed: _busy || _lesson == null
              ? null
              : () => _run(() async {
                  final row = _lessons.firstWhere(
                    (x) => x['maBuoiHoc'] == _lesson,
                  );
                  if (row['sessionId'] == null) {
                    throw const AttendanceWorkflowException(
                      'Buổi học chưa có phiên đang mở.',
                    );
                  }
                  await _showSession(row['sessionId'] as String);
                }),
          child: const Text('Xem phiên đang mở'),
        ),
        if (_qr != null) ...[
          Text(
            '${_qr!['maLopHocPhan']} · ${_qr!['tenMonHoc']}',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          Center(
            child: ColoredBox(
              color: Colors.white,
              child: QrImageView(data: _qr!['qrToken'] as String, size: 320),
            ),
          ),
          Text(
            'QR tự cập nhật mỗi 15 giây. Phiên hết hạn lúc ${DateTime.parse(_qr!['sessionExpiresAtUtc'] as String).toLocal()}',
          ),
          OutlinedButton(
            onPressed: _busy
                ? null
                : () => _run(() async {
                    await _api.request('phien/$_sessionId/dong', body: {});
                    _poll?.cancel();
                    if (mounted) {
                      setState(() {
                        _sessionId = null;
                        _qr = null;
                        _message = 'Đã đóng phiên.';
                      });
                    }
                    await _loadLessons();
                  }),
            child: const Text('Đóng phiên'),
          ),
        ],
        const SizedBox(height: 16),
        Text(
          'Kết quả buổi học (${_results.length})',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        for (final row in _results)
          ListTile(
            title: Text('${row['maSinhVien']} · ${row['hoTen']}'),
            subtitle: Text(
              '${row['trangThai']} · ${row['thoiGianCheckIn'] ?? ''}',
            ),
          ),
        if (_canApprove) ...[
          const Divider(),
          Text(
            'Hồ sơ khuôn mặt',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const Text(
            'CBQL hoặc Admin đối chiếu ảnh thẻ với ảnh đăng ký và duyệt một lần để dùng chung cho tất cả lớp.',
          ),
          TextButton(
            onPressed: _busy ? null : () => _run(_loadProfiles),
            child: const Text('Tải danh sách hồ sơ sinh viên'),
          ),
          for (final row in _profiles)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${row['maSinhVien']} · ${row['hoTen']}'),
                    Text(
                      row['daDuyet'] == true
                          ? 'Đã duyệt'
                          : row['biTuChoi'] == true
                          ? 'Yêu cầu đăng ký lại'
                          : 'Chờ duyệt',
                    ),
                    TextButton(
                      onPressed: _busy
                          ? null
                          : () async {
                              await Navigator.push(
                                context,
                                MaterialPageRoute<void>(
                                  builder: (_) => FaceProfileReviewScreen(
                                    studentCode: row['maSinhVien'] as String,
                                  ),
                                ),
                              );
                              if (mounted) await _run(_loadProfiles);
                            },
                      child: const Text('Đối chiếu ảnh'),
                    ),
                    if (row['daDuyet'] == true)
                      TextButton(
                        onPressed: _busy
                            ? null
                            : () => _run(() async {
                                final yes = await showDialog<bool>(
                                  context: context,
                                  builder: (c) => AlertDialog(
                                    title: const Text('Hủy hồ sơ khuôn mặt?'),
                                    content: Text(
                                      'Hủy hồ sơ dùng chung của sinh viên ${row['maSinhVien']} sẽ ảnh hưởng điểm danh ở tất cả lớp. Sinh viên phải đăng ký và được duyệt lại.',
                                    ),
                                    actions: [
                                      TextButton(
                                        onPressed: () =>
                                            Navigator.pop(c, false),
                                        child: const Text('Giữ lại'),
                                      ),
                                      TextButton(
                                        onPressed: () => Navigator.pop(c, true),
                                        child: const Text('Hủy hồ sơ'),
                                      ),
                                    ],
                                  ),
                                );
                                if (yes == true) {
                                  await _api.request(
                                    'ho-so/${Uri.encodeComponent(row['maSinhVien'] as String)}/huy',
                                    body: {},
                                  );
                                  await _loadProfiles();
                                }
                              }),
                        child: const Text('Hủy hồ sơ để đăng ký lại'),
                      ),
                  ],
                ),
              ),
            ),
        ],
      ],
    ),
  );
}
