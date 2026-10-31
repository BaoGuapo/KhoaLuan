import 'package:flutter/material.dart';

import '../../core/student/student_portal_service.dart';
import '../../core/theme/app_colors.dart';
import 'student_portal_format.dart';

class StudentTuitionScreen extends StatefulWidget {
  const StudentTuitionScreen({super.key});

  @override
  State<StudentTuitionScreen> createState() => _StudentTuitionScreenState();
}

class _StudentTuitionScreenState extends State<StudentTuitionScreen> {
  final _service = StudentPortalService();
  List<TuitionFee> _fees = const [];
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final fees = await _service.getMyFees();
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
            : 'Không tải được thông tin học phí.';
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final remaining = _fees.fold<double>(0, (sum, fee) => sum + fee.remaining);
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Học phí và công nợ',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 19,
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
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _TuitionMessage(message: _error!, onRetry: _load)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      if (_fees.any((fee) => fee.isDemo))
                        const _TuitionDemoBanner(),
                      Container(
                        padding: const EdgeInsets.all(18),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: const Color(0xFFE2E8F0)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Tổng công nợ hiện tại',
                              style: TextStyle(
                                  color: AppColors.textSecondary, fontSize: 13),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              formatVnd(remaining),
                              style: const TextStyle(
                                color: AppColors.primary,
                                fontSize: 26,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      if (_fees.isEmpty)
                        const _TuitionMessage(
                            message: 'Chưa có khoản thu học phí.')
                      else
                        ..._fees.map(_FeeCard.new),
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 8),
                        child: Text(
                          'Ứng dụng chỉ tra cứu khoản thu. Việc xác nhận số tiền và trạng thái đã thu do Phòng Tài chính/Kế toán cập nhật.',
                          style: TextStyle(
                            color: AppColors.textSecondary,
                            fontSize: 12,
                            height: 1.4,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }
}

class _FeeCard extends StatelessWidget {
  const _FeeCard(this.fee);
  final TuitionFee fee;

  @override
  Widget build(BuildContext context) {
    final complete = fee.remaining <= 0;
    final partiallyPaid = fee.paid > 0 && !complete;
    final color = complete
        ? AppColors.success
        : partiallyPaid
            ? const Color(0xFFB45309)
            : AppColors.danger;
    final label = complete
        ? 'Đã hoàn tất'
        : partiallyPaid
            ? 'Đã đóng một phần'
            : 'Chưa thanh toán';
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    fee.semesterLabel,
                    style: const TextStyle(
                        fontWeight: FontWeight.w800, fontSize: 16),
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: color.withOpacity(.1),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(label,
                      style: TextStyle(
                          color: color,
                          fontSize: 11,
                          fontWeight: FontWeight.w700)),
                ),
              ],
            ),
            const Divider(height: 22),
            _AmountLine('Tổng học phí', fee.total),
            _AmountLine('Đã thanh toán', fee.paid, color: AppColors.success),
            _AmountLine(
              'Còn phải đóng',
              fee.remaining,
              color: fee.remaining > 0 ? AppColors.danger : AppColors.success,
              strong: true,
            ),
            const SizedBox(height: 8),
            Text(
              'Hạn thanh toán: ${formatPortalDate(fee.dueDate)}',
              style:
                  const TextStyle(color: AppColors.textSecondary, fontSize: 12),
            ),
            if (fee.note.isNotEmpty) ...[
              const SizedBox(height: 5),
              Text(fee.note,
                  style: const TextStyle(
                      color: AppColors.textSecondary, fontSize: 11)),
            ],
          ],
        ),
      ),
    );
  }
}

class _AmountLine extends StatelessWidget {
  const _AmountLine(
    this.label,
    this.amount, {
    this.color = AppColors.textPrimary,
    this.strong = false,
  });

  final String label;
  final double amount;
  final Color color;
  final bool strong;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: const TextStyle(
                    color: AppColors.textSecondary, fontSize: 13)),
            Text(
              formatVnd(amount),
              style: TextStyle(
                color: color,
                fontSize: 13,
                fontWeight: strong ? FontWeight.w800 : FontWeight.w600,
              ),
            ),
          ],
        ),
      );
}

class _TuitionDemoBanner extends StatelessWidget {
  const _TuitionDemoBanner();

  @override
  Widget build(BuildContext context) => Container(
        margin: const EdgeInsets.only(bottom: 14),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: const Color(0xFFFFFBEB),
          border: Border.all(color: const Color(0xFFFDE68A)),
          borderRadius: BorderRadius.circular(12),
        ),
        child: const Text(
          'DỮ LIỆU DEMO: các khoản thu minh họa để thử giao diện, không phải hóa đơn/học phí chính thức.',
          style: TextStyle(color: Color(0xFF92400E), height: 1.35),
        ),
      );
}

class _TuitionMessage extends StatelessWidget {
  const _TuitionMessage({required this.message, this.onRetry});
  final String message;
  final Future<void> Function()? onRetry;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(message, textAlign: TextAlign.center),
              if (onRetry != null)
                TextButton(onPressed: onRetry, child: const Text('Thử lại')),
            ],
          ),
        ),
      );
}
