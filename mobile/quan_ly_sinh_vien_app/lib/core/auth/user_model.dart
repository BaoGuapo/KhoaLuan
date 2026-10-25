enum UserRole {
  sinhVien,
  giangVien,
  canBoQuanLy,
  admin,
}

extension UserRoleExtension on UserRole {
  String get nameInVietnamese {
    switch (this) {
      case UserRole.sinhVien:
        return 'Sinh viên';
      case UserRole.giangVien:
        return 'Giảng viên';
      case UserRole.canBoQuanLy:
        return 'Cán bộ quản lý';
      case UserRole.admin:
        return 'Quản trị viên hệ thống';
    }
  }

  static UserRole fromString(String roleStr) {
    switch (roleStr.toLowerCase()) {
      case 'giangvien':
      case 'giảng viên':
      case 'lecturer':
        return UserRole.giangVien;
      case 'admin':
      case 'quản trị viên':
        return UserRole.admin;
      case 'canboquanly':
      case 'cán bộ quản lý':
        return UserRole.canBoQuanLy;
      case 'sinhvien':
      case 'sinh viên':
      case 'student':
      default:
        return UserRole.sinhVien;
    }
  }
}

class UserModel {
  final String id;
  final String userName;
  final String fullName;
  final String email;
  final String code; // MSSV or Mã GV
  final UserRole role;
  final String? className;
  final String? department;
  final String? programType;
  final String? avatarUrl;
  final double? gpa;
  final int? creditsCompleted;
  final int? totalCredits;
  final int? attendanceRate;

  const UserModel({
    required this.id,
    required this.userName,
    required this.fullName,
    required this.email,
    required this.code,
    required this.role,
    this.className,
    this.department,
    this.programType,
    this.avatarUrl,
    this.gpa,
    this.creditsCompleted,
    this.totalCredits,
    this.attendanceRate,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id']?.toString() ?? '',
      userName: json['userName']?.toString() ?? '',
      fullName:
          json['fullName']?.toString() ?? json['userName']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      code: json['code']?.toString() ??
          json['studentId']?.toString() ??
          json['userName']?.toString() ??
          '',
      role:
          UserRoleExtension.fromString(json['role']?.toString() ?? 'sinhvien'),
      className: json['className']?.toString(),
      department: json['department']?.toString(),
      programType: json['programType']?.toString(),
      avatarUrl: json['avatarUrl']?.toString(),
      gpa: (json['gpa'] is num) ? (json['gpa'] as num).toDouble() : null,
      creditsCompleted: (json['creditsCompleted'] is num)
          ? (json['creditsCompleted'] as num).toInt()
          : null,
      totalCredits: (json['totalCredits'] is num)
          ? (json['totalCredits'] as num).toInt()
          : null,
      attendanceRate: (json['attendanceRate'] is num)
          ? (json['attendanceRate'] as num).toInt()
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userName': userName,
      'fullName': fullName,
      'email': email,
      'code': code,
      'role': role.name,
      'className': className,
      'department': department,
      'programType': programType,
      'avatarUrl': avatarUrl,
      'gpa': gpa,
      'creditsCompleted': creditsCompleted,
      'totalCredits': totalCredits,
      'attendanceRate': attendanceRate,
    };
  }
}
