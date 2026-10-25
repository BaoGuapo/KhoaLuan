import 'package:flutter/material.dart';

class FaceScanIcon extends StatelessWidget {
  final double size;
  final Color color;
  final double strokeWidth;
  final double cornerLength;

  const FaceScanIcon({
    super.key,
    this.size = 28,
    this.color = Colors.white,
    this.strokeWidth = 2.5,
    this.cornerLength = 6,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(
        painter: _FaceScanPainter(
          color: color,
          strokeWidth: strokeWidth,
          cornerLength: cornerLength,
        ),
      ),
    );
  }
}

class _FaceScanPainter extends CustomPainter {
  final Color color;
  final double strokeWidth;
  final double cornerLength;

  _FaceScanPainter({
    required this.color,
    required this.strokeWidth,
    required this.cornerLength,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = strokeWidth
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;

    final w = size.width;
    final h = size.height;
    final cl = cornerLength.clamp(2.0, w / 2);

    // Top Left
    canvas.drawLine(Offset(0, cl), const Offset(0, 0), paint);
    canvas.drawLine(const Offset(0, 0), Offset(cl, 0), paint);

    // Top Right
    canvas.drawLine(Offset(w - cl, 0), Offset(w, 0), paint);
    canvas.drawLine(Offset(w, 0), Offset(w, cl), paint);

    // Bottom Left
    canvas.drawLine(Offset(0, h - cl), Offset(0, h), paint);
    canvas.drawLine(Offset(0, h), Offset(cl, h), paint);

    // Bottom Right
    canvas.drawLine(Offset(w - cl, h), Offset(w, h), paint);
    canvas.drawLine(Offset(w, h), Offset(w, h - cl), paint);
  }

  @override
  bool shouldRepaint(covariant _FaceScanPainter oldDelegate) {
    return oldDelegate.color != color ||
        oldDelegate.strokeWidth != strokeWidth ||
        oldDelegate.cornerLength != cornerLength;
  }
}
