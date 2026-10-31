import 'package:flutter/material.dart';
import 'attendance_workflow_screen.dart';

// All entry points require a valid QR before attendance face capture.
class StudentFaceScanScreen extends StatelessWidget {
  const StudentFaceScanScreen({
    super.key,
    this.maBuoiHoc,
    this.tenMonHoc,
    this.phongHoc,
    this.thoiGian,
  });
  final String? maBuoiHoc, tenMonHoc, phongHoc, thoiGian;
  @override
  Widget build(BuildContext context) => const AttendanceWorkflowScreen();
}
