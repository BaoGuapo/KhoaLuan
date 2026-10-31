import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import '../../core/attendance/attendance_workflow_api.dart';

class FaceProfileReviewScreen extends StatefulWidget {
  const FaceProfileReviewScreen({
    super.key,
    required this.studentCode,
    this.api,
  });
  final String studentCode;
  final AttendanceWorkflowApi? api;
  @override
  State<FaceProfileReviewScreen> createState() =>
      _FaceProfileReviewScreenState();
}

class _FaceProfileReviewScreenState extends State<FaceProfileReviewScreen> {
  late final _api = widget.api ?? AttendanceWorkflowApi();
  final _note = TextEditingController();
  Map<String, dynamic>? _review;
  bool _compared = false, _busy = false;
  String? _message;
  bool get _pending =>
      _review != null &&
      _review!['daDuyet'] != true &&
      _review!['biTuChoi'] != true;
  bool get _hasImages =>
      _review?['anhThe'] != null &&
      (_review?['anhDangKy'] as List?)?.length == 3;

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _busy = true);
    try {
      final value = await _api.request(
        'ho-so/${Uri.encodeComponent(widget.studentCode)}/doi-chieu',
      );
      if (mounted) {
        setState(() {
          _review = Map<String, dynamic>.from(value as Map);
          _compared = false;
          _message = null;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _message = e.toString();
          if (e is AttendanceWorkflowException && e.code == 'ReviewChanged') {
            _compared = false;
          }
        });
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _decide(bool approve) async {
    if (_busy || !_pending) return;
    if (approve && (!_compared || !_hasImages)) return;
    if (!approve && _note.text.trim().isEmpty) {
      setState(() => _message = 'Nhập lý do yêu cầu sinh viên đăng ký lại.');
      return;
    }
    setState(() => _busy = true);
    try {
      final result = await _api.request(
        'ho-so/${Uri.encodeComponent(widget.studentCode)}/${approve ? 'duyet' : 'tu-choi'}',
        body: {
          'profileVersion': _review!['profileVersion'],
          'identityPhotoVersion': _review!['identityPhotoVersion'],
          'confirmCompared': _compared,
          'note': _note.text.trim(),
        },
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text((result as Map)['message'].toString())),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _message = e.toString();
          if (e is AttendanceWorkflowException && e.code == 'ReviewChanged') {
            _compared = false;
          }
        });
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _photo(String? data, String label) => Column(
    children: [
      Text(label, style: Theme.of(context).textTheme.titleSmall),
      const SizedBox(height: 8),
      if (data == null)
        const Padding(padding: EdgeInsets.all(24), child: Text('Chưa có ảnh.'))
      else
        Image.memory(
          base64Decode(data.split(',').last),
          height: 240,
          fit: BoxFit.contain,
          errorBuilder: (_, _, _) =>
              const Text('Không đọc được ảnh. Tải lại hồ sơ để kiểm tra.'),
        ),
    ],
  );
  @override
  void dispose() {
    _note.dispose();
    _api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Đối chiếu ảnh hồ sơ')),
    body: SingleChildScrollView(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_busy) const LinearProgressIndicator(),
            if (_message != null)
              Text(_message!, style: const TextStyle(color: Colors.red)),
            Text(
              '${widget.studentCode} · ${_review?['hoTen'] ?? ''}',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 12),
            const Text(
              'Đối chiếu ảnh thẻ từ hồ sơ gốc với ảnh sinh viên đã chụp khi đăng ký. Sinh viên không cần có mặt và không phải chụp lại để duyệt.',
            ),
            const SizedBox(height: 16),
            if (_review != null) ...[
              _photo(
                _review!['anhThe'] as String?,
                'Ảnh thẻ trong hồ sơ sinh viên',
              ),
              if (_review!['anhTheCapNhatBoi'] != null)
                Text(
                  'Ảnh thẻ được cập nhật bởi ${_review!['anhTheCapNhatBoi']}.',
                ),
              if (_review!['anhThe'] == null)
                const Text(
                  'Cập nhật ảnh thẻ từ hồ sơ gốc tại trang Duyệt hồ sơ khuôn mặt trên cổng CBQL/Admin.',
                ),
              const Divider(height: 32),
              Text(
                'Ảnh chụp khi đăng ký',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              Wrap(
                spacing: 12,
                runSpacing: 12,
                children: [
                  for (final (index, image)
                      in (_review!['anhDangKy'] as List? ?? []).indexed)
                    SizedBox(
                      width: 180,
                      child: _photo(
                        image as String,
                        ['Nhìn thẳng', 'Quay đầu', 'Nhìn thẳng lại'][index
                            .clamp(0, 2)],
                      ),
                    ),
                ],
              ),
              if (!_hasImages)
                const Text(
                  'Thiếu ảnh đối chiếu. Không thể duyệt. Nếu hồ sơ cũ thiếu ảnh đăng ký, yêu cầu sinh viên đăng ký lại kèm lý do.',
                ),
              if (_review!['biTuChoi'] == true)
                Text('Lý do trả hồ sơ: ${_review!['lyDoTuChoi']}'),
              if (_review!['daDuyet'] == true)
                Text('Đã duyệt bởi ${_review!['nguoiDuyet']}.'),
              if (_pending) ...[
                CheckboxListTile(
                  value: _compared,
                  onChanged: _busy || !_hasImages
                      ? null
                      : (value) => setState(() => _compared = value ?? false),
                  title: const Text(
                    'Tôi đã đối chiếu hai nguồn ảnh và xác nhận đúng sinh viên.',
                  ),
                ),
                TextField(
                  controller: _note,
                  maxLength: 500,
                  minLines: 2,
                  maxLines: 4,
                  decoration: const InputDecoration(
                    labelText: 'Ghi chú / lý do yêu cầu đăng ký lại',
                  ),
                ),
                FilledButton(
                  onPressed: _busy || !_compared || !_hasImages
                      ? null
                      : () => _decide(true),
                  child: const Text('Duyệt hồ sơ'),
                ),
                OutlinedButton(
                  onPressed: _busy ? null : () => _decide(false),
                  child: const Text('Yêu cầu đăng ký lại'),
                ),
              ],
            ],
            TextButton(
              onPressed: _busy ? null : _load,
              child: const Text('Tải lại hồ sơ'),
            ),
          ],
        ),
      ),
    ),
  );
}
