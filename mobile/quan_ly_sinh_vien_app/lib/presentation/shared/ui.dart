import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

/// Màu và nhãn dùng chung cho các trạng thái điểm danh trả về từ backend.
class AttendanceStatus {
  AttendanceStatus._();

  static const present = Color(0xFF0B8F55);
  static const late = Color(0xFFE09B12);
  static const excused = Color(0xFF2F7DD1);
  static const absent = Color(0xFFD64545);
  static const pending = Color(0xFFB4C0CC);

  static String label(String? status) => switch (status) {
        'CoMat' => 'Có mặt',
        'DiTre' => 'Đi trễ',
        'CoPhep' => 'Có phép',
        'Vang' => 'Vắng',
        _ => 'Chưa điểm danh',
      };

  static Color color(String? status) => switch (status) {
        'CoMat' => present,
        'DiTre' => late,
        'CoPhep' => excused,
        'Vang' => absent,
        _ => pending,
      };
}

/// Mức cảnh báo chuyên cần: vắng từ 2 buổi bị cảnh báo, từ 4 buổi bị cấm thi.
class WarningLevel {
  WarningLevel._();

  static bool isFlagged(String? level) => level == 'CanhBao' || level == 'CamThi';

  static String label(String? level) => switch (level) {
        'CanhBao' => 'Cảnh báo',
        'CamThi' => 'Cấm thi',
        _ => 'Bình thường',
      };

  static Color color(String? level) => switch (level) {
        'CanhBao' => AttendanceStatus.late,
        'CamThi' => AttendanceStatus.absent,
        _ => AttendanceStatus.present,
      };
}

final _percent = NumberFormat.decimalPercentPattern(locale: 'vi', decimalDigits: 1);
final dateFormat = DateFormat('dd/MM/yyyy');
final shortDateFormat = DateFormat('dd/MM');
final timeFormat = DateFormat('HH:mm');

String formatRate(double rate) => _percent.format(rate);

Color rateColor(double rate) => rate >= 0.8
    ? AttendanceStatus.present
    : rate >= 0.6
        ? AttendanceStatus.late
        : AttendanceStatus.absent;

String dayOfWeekLabel(int thu) => thu == 8 ? 'Chủ nhật' : 'Thứ $thu';

/// Thứ trong tuần theo quy ước backend: 2 = Thứ hai ... 8 = Chủ nhật.
int thuOf(DateTime date) => date.weekday == DateTime.sunday ? 8 : date.weekday + 1;

const _periodStarts = [
  '07:00', '07:55', '09:00', '09:55', '10:50',
  '13:00', '13:55', '15:00', '15:55', '16:50',
  '18:00', '18:55', '20:00', '20:55', '21:50',
];

/// Khung giờ của các tiết, mỗi tiết 50 phút (khớp ClassPeriods ở backend).
String periodRange(int from, int to) {
  if (from < 1 || to > _periodStarts.length || to < from) return 'Tiết $from–$to';
  final end = DateFormat('HH:mm')
      .parse(_periodStarts[to - 1])
      .add(const Duration(minutes: 50));
  return 'Tiết $from–$to · ${_periodStarts[from - 1]}–${timeFormat.format(end)}';
}

DateTime? parseDate(Object? value) =>
    value is String ? DateTime.tryParse(value) : null;

/// Thời điểm backend lưu theo UTC; EF có thể bỏ hậu tố "Z" nên phải tự gắn trước khi đổi sang giờ máy.
DateTime? parseUtcToLocal(Object? value) {
  if (value is! String || value.isEmpty) return null;
  final hasZone = value.endsWith('Z') || RegExp(r'[+-]\d{2}:?\d{2}$').hasMatch(value);
  return DateTime.tryParse(hasZone ? value : '${value}Z')?.toLocal();
}

/// Tên phòng có thể đã gồm chữ "Phòng" (ví dụ "Phòng thực hành 01") nên không thêm tiền tố lần nữa.
String roomLabel(Object? value) {
  final room = asText(value).trim();
  if (room.isEmpty) return 'Chưa xếp phòng';
  return room.toLowerCase().startsWith('phòng') ? room : 'Phòng $room';
}

int asInt(Object? value) => value is num ? value.toInt() : 0;

double asDouble(Object? value) => value is num ? value.toDouble() : 0;

String asText(Object? value, [String fallback = '']) =>
    value == null ? fallback : value.toString();

/// Tải dữ liệu bất đồng bộ, hiển thị trạng thái đang tải/lỗi và hỗ trợ kéo để làm mới.
class AsyncPage<T> extends StatefulWidget {
  const AsyncPage({
    required this.load,
    required this.builder,
    super.key,
  });

  final Future<T> Function() load;
  final Widget Function(BuildContext context, T data, Future<void> Function() reload)
      builder;

  @override
  State<AsyncPage<T>> createState() => AsyncPageState<T>();
}

class AsyncPageState<T> extends State<AsyncPage<T>> {
  T? _data;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    reload();
  }

  Future<void> reload() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final data = await widget.load();
      if (!mounted) return;
      setState(() {
        _data = data;
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

  @override
  Widget build(BuildContext context) {
    final data = _data;
    if (data == null) {
      if (_loading) return const Center(child: CircularProgressIndicator());
      return MessageView(
        icon: Icons.cloud_off_outlined,
        message: _error?.toString() ?? 'Không có dữ liệu.',
        actionLabel: 'Thử lại',
        onAction: reload,
      );
    }
    return Stack(
      children: [
        RefreshIndicator(
          onRefresh: reload,
          child: widget.builder(context, data, reload),
        ),
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
}

class MessageView extends StatelessWidget {
  const MessageView({
    required this.message,
    this.icon = Icons.info_outline,
    this.actionLabel,
    this.onAction,
    super.key,
  });

  final String message;
  final IconData icon;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 48, color: const Color(0xFF8A9BAE)),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}

/// Khối trống nằm trong danh sách cuộn được (để kéo làm mới vẫn hoạt động).
class EmptyCard extends StatelessWidget {
  const EmptyCard({required this.message, this.icon = Icons.inbox_outlined, super.key});

  final String message;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 20),
        child: Column(
          children: [
            Icon(icon, size: 36, color: const Color(0xFF8A9BAE)),
            const SizedBox(height: 8),
            Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Color(0xFF5B6B7C)),
            ),
          ],
        ),
      ),
    );
  }
}

class SectionTitle extends StatelessWidget {
  const SectionTitle(this.title, {this.trailing, super.key});

  final String title;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(4, 20, 4, 10),
      child: Row(
        children: [
          Expanded(
            child: Text(
              title,
              style: Theme.of(context)
                  .textTheme
                  .titleMedium
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}

class StatTile extends StatelessWidget {
  const StatTile({
    required this.label,
    required this.value,
    required this.icon,
    this.color = const Color(0xFF0867B2),
    super.key,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: color, size: 22),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    value,
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
                  ),
                  Text(
                    label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Lưới 2 cột cho các ô số liệu.
class StatGrid extends StatelessWidget {
  const StatGrid({required this.children, super.key});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final rows = <Widget>[];
    for (var i = 0; i < children.length; i += 2) {
      rows.add(Padding(
        padding: EdgeInsets.only(top: i == 0 ? 0 : 10),
        child: Row(
          children: [
            Expanded(child: children[i]),
            const SizedBox(width: 10),
            Expanded(
              child: i + 1 < children.length ? children[i + 1] : const SizedBox(),
            ),
          ],
        ),
      ));
    }
    return Column(children: rows);
  }
}

/// Thanh tỷ lệ các trạng thái điểm danh kèm chú thích số lượng.
class AttendanceBreakdown extends StatelessWidget {
  const AttendanceBreakdown({
    required this.coMat,
    required this.diTre,
    required this.coPhep,
    required this.vang,
    this.chuaDiemDanh = 0,
    this.showLegend = true,
    this.height = 12,
    super.key,
  });

  final int coMat;
  final int diTre;
  final int coPhep;
  final int vang;
  final int chuaDiemDanh;
  final bool showLegend;
  final double height;

  @override
  Widget build(BuildContext context) {
    final parts = [
      ('CoMat', coMat),
      ('DiTre', diTre),
      ('CoPhep', coPhep),
      ('Vang', vang),
      ('ChuaDiemDanh', chuaDiemDanh),
    ];
    final total = parts.fold<int>(0, (sum, x) => sum + x.$2);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(height / 2),
          child: SizedBox(
            height: height,
            child: total == 0
                ? Container(color: const Color(0xFFE6ECF2))
                : Row(
                    children: [
                      for (final part in parts)
                        if (part.$2 > 0)
                          Expanded(
                            flex: part.$2,
                            child: Container(color: AttendanceStatus.color(part.$1)),
                          ),
                    ],
                  ),
          ),
        ),
        if (showLegend) ...[
          const SizedBox(height: 10),
          Wrap(
            spacing: 14,
            runSpacing: 6,
            children: [
              for (final part in parts)
                if (part.$1 != 'ChuaDiemDanh' || part.$2 > 0)
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 10,
                        height: 10,
                        decoration: BoxDecoration(
                          color: AttendanceStatus.color(part.$1),
                          shape: BoxShape.circle,
                        ),
                      ),
                      const SizedBox(width: 5),
                      Text(
                        '${AttendanceStatus.label(part.$1)} ${part.$2}',
                        style: const TextStyle(fontSize: 12),
                      ),
                    ],
                  ),
            ],
          ),
        ],
      ],
    );
  }
}

class Pill extends StatelessWidget {
  const Pill(this.text, {required this.color, super.key});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        text,
        style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w700),
      ),
    );
  }
}

/// Biểu đồ cột tỷ lệ chuyên cần theo ngày (giá trị 0–1).
class RateBarChart extends StatelessWidget {
  const RateBarChart({required this.points, super.key});

  final List<(DateTime, double)> points;

  @override
  Widget build(BuildContext context) {
    if (points.isEmpty) {
      return const SizedBox(
        height: 60,
        child: Center(child: Text('Chưa có buổi học nào diễn ra.')),
      );
    }
    return SizedBox(
      height: 150,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        reverse: true,
        itemCount: points.length,
        separatorBuilder: (_, _) =>const SizedBox(width: 6),
        itemBuilder: (context, index) {
          final point = points[points.length - 1 - index];
          final rate = point.$2.clamp(0.0, 1.0);
          return Tooltip(
            message: '${dateFormat.format(point.$1)}: ${formatRate(point.$2)}',
            child: SizedBox(
              width: 30,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Text(
                    '${(rate * 100).round()}',
                    style: const TextStyle(fontSize: 10, color: Color(0xFF5B6B7C)),
                  ),
                  const SizedBox(height: 2),
                  Container(
                    height: 100 * rate + 2,
                    decoration: BoxDecoration(
                      color: rateColor(rate),
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    shortDateFormat.format(point.$1),
                    style: const TextStyle(fontSize: 10),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
