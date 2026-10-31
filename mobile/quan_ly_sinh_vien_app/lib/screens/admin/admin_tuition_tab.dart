import 'package:flutter/material.dart';

import '../../core/student/student_portal_service.dart';
import '../../core/theme/app_colors.dart';
import '../student/student_portal_format.dart';

class AdminTuitionTab extends StatefulWidget {
  const AdminTuitionTab({super.key});

  @override
  State<AdminTuitionTab> createState() => _AdminTuitionTabState();
}

class _AdminTuitionTabState extends State<AdminTuitionTab> {
  final _service = StudentPortalService();
  final _searchController = TextEditingController(text: '2001230048');
  List<TuitionFee> _fees = const [];
  String? _error;
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final fees = await _service.getFeesForManagement(
        studentCode: _searchController.text,
      );
      if (!mounted) return;
      setState(() {
        _fees = fees;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error is StudentPortalException
            ? error.message
            : 'Không tải được danh sách học phí.';
        _loading = false;
      });
    }
  }

  Future<void> _edit(TuitionFee fee) async {
    final result = await showDialog<_FeeEditResult>(
      context: context,
      builder: (_) => _FeeEditDialog(fee: fee),
    );
    if (result == null) return;
    try {
      await _service.updateFee(
        id: fee.id,
        total: result.total,
        paid: result.paid,
        dueDate: result.dueDate,
        note: result.note,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đã cập nhật khoản học phí.')),
      );
      await _load();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            error is StudentPortalException
                ? error.message
                : 'Không cập nhật được học phí.',
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: const Color(0xFFF8FAFC),
        appBar: AppBar(
          title: const Text(
            'Quản lý học phí',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
          backgroundColor: Colors.white,
          elevation: 0,
          actions: [
            IconButton(
              tooltip: 'Làm mới',
              onPressed: _loading ? null : _load,
              icon: const Icon(Icons.refresh, color: AppColors.primary),
            ),
          ],
        ),
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _searchController,
                      textInputAction: TextInputAction.search,
                      onSubmitted: (_) => _load(),
                      decoration: const InputDecoration(
                        labelText: 'Mã sinh viên (để trống xem tất cả)',
                        border: OutlineInputBorder(),
                        isDense: true,
                        prefixIcon: Icon(Icons.search),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton.filled(
                    onPressed: _loading ? null : _load,
                    icon: const Icon(Icons.arrow_forward),
                  ),
                ],
              ),
            ),
            if (_error != null)
              Expanded(
                child: Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_error!, textAlign: TextAlign.center),
                        TextButton(
                            onPressed: _load, child: const Text('Thử lại')),
                      ],
                    ),
                  ),
                ),
              )
            else if (_loading)
              const Expanded(child: Center(child: CircularProgressIndicator()))
            else if (_fees.isEmpty)
              const Expanded(
                child: Center(child: Text('Không tìm thấy khoản thu học phí.')),
              )
            else
              Expanded(
                child: RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _fees.length,
                    itemBuilder: (context, index) {
                      final fee = _fees[index];
                      return Card(
                        color: Colors.white,
                        margin: const EdgeInsets.only(bottom: 12),
                        child: ListTile(
                          contentPadding: const EdgeInsets.all(14),
                          title: Text(
                            '${fee.studentName} · ${fee.studentCode}',
                            style: const TextStyle(
                                fontWeight: FontWeight.w700, fontSize: 13),
                          ),
                          subtitle: Padding(
                            padding: const EdgeInsets.only(top: 8),
                            child: Text(
                              '${fee.semesterLabel}\n'
                              'Tổng: ${formatVnd(fee.total)}\n'
                              'Đã thu: ${formatVnd(fee.paid)} · Còn: ${formatVnd(fee.remaining)}\n'
                              'Hạn: ${formatPortalDate(fee.dueDate)}',
                              style: const TextStyle(height: 1.5),
                            ),
                          ),
                          isThreeLine: true,
                          trailing: IconButton(
                            tooltip: 'Sửa khoản thu',
                            onPressed: () => _edit(fee),
                            icon: const Icon(Icons.edit_outlined,
                                color: AppColors.primary),
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ),
            if (_fees.any((fee) => fee.isDemo))
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                color: const Color(0xFFFFFBEB),
                child: const Text(
                  'Số liệu mang nhãn DEMO chỉ dùng thử. Chỉ admin/cán bộ quản lý được sửa; không dùng làm đối soát tài chính thật.',
                  style: TextStyle(color: Color(0xFF92400E), fontSize: 12),
                ),
              ),
          ],
        ),
      );
}

class _FeeEditResult {
  const _FeeEditResult({
    required this.total,
    required this.paid,
    required this.dueDate,
    required this.note,
  });
  final double total;
  final double paid;
  final DateTime dueDate;
  final String note;
}

class _FeeEditDialog extends StatefulWidget {
  const _FeeEditDialog({required this.fee});
  final TuitionFee fee;

  @override
  State<_FeeEditDialog> createState() => _FeeEditDialogState();
}

class _FeeEditDialogState extends State<_FeeEditDialog> {
  final _formKey = GlobalKey<FormState>();
  late final _totalController =
      TextEditingController(text: widget.fee.total.toStringAsFixed(0));
  late final _paidController =
      TextEditingController(text: widget.fee.paid.toStringAsFixed(0));
  late final _noteController = TextEditingController(text: widget.fee.note);
  late DateTime _dueDate = widget.fee.dueDate;

  @override
  void dispose() {
    _totalController.dispose();
    _paidController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
        title: Text('Sửa ${widget.fee.semesterLabel}'),
        content: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: _totalController,
                  keyboardType: TextInputType.number,
                  decoration:
                      const InputDecoration(labelText: 'Tổng học phí (VND)'),
                  validator: _validateMoney,
                ),
                TextFormField(
                  controller: _paidController,
                  keyboardType: TextInputType.number,
                  decoration:
                      const InputDecoration(labelText: 'Đã thanh toán (VND)'),
                  validator: (value) {
                    final error = _validateMoney(value);
                    if (error != null) return error;
                    final total = double.tryParse(_totalController.text);
                    final paid = double.tryParse(value ?? '');
                    if (total != null && paid != null && paid > total) {
                      return 'Không thể vượt quá tổng học phí';
                    }
                    return null;
                  },
                ),
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Hạn thanh toán'),
                  subtitle: Text(formatPortalDate(_dueDate)),
                  trailing: const Icon(Icons.calendar_month),
                  onTap: () async {
                    final date = await showDatePicker(
                      context: context,
                      initialDate: _dueDate,
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2040),
                    );
                    if (date != null) setState(() => _dueDate = date);
                  },
                ),
                TextFormField(
                  controller: _noteController,
                  maxLines: 2,
                  maxLength: 500,
                  decoration: const InputDecoration(labelText: 'Ghi chú'),
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Hủy')),
          ElevatedButton(
            onPressed: () {
              if (!_formKey.currentState!.validate()) return;
              Navigator.pop(
                context,
                _FeeEditResult(
                  total: double.parse(_totalController.text),
                  paid: double.parse(_paidController.text),
                  dueDate: _dueDate,
                  note: _noteController.text,
                ),
              );
            },
            child: const Text('Lưu'),
          ),
        ],
      );

  String? _validateMoney(String? value) {
    final parsed = double.tryParse(value ?? '');
    if (parsed == null || parsed < 0) return 'Nhập số tiền hợp lệ';
    return null;
  }
}
