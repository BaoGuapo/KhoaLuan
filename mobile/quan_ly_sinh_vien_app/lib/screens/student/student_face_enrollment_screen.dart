import 'package:flutter/material.dart';
import 'attendance_workflow_screen.dart';

class StudentFaceEnrollmentScreen extends StatelessWidget {
  const StudentFaceEnrollmentScreen({super.key});
  @override
  Widget build(BuildContext context) =>
      const AttendanceWorkflowScreen(enrollment: true);
}
