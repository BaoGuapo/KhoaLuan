import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/auth/user_model.dart';

void main() {
  group('UserRoleExtension.fromString', () {
    test('nhận diện vai trò Sinh viên', () {
      expect(UserRoleExtension.fromString('sinhvien'), UserRole.sinhVien);
      expect(UserRoleExtension.fromString('student'), UserRole.sinhVien);
      expect(UserRoleExtension.fromString('sinh viên'), UserRole.sinhVien);
    });

    test('nhận diện vai trò Giảng viên', () {
      expect(UserRoleExtension.fromString('giangvien'), UserRole.giangVien);
      expect(UserRoleExtension.fromString('lecturer'), UserRole.giangVien);
      expect(UserRoleExtension.fromString('giảng viên'), UserRole.giangVien);
    });

    test('nhận diện vai trò Admin', () {
      expect(UserRoleExtension.fromString('admin'), UserRole.admin);
      expect(UserRoleExtension.fromString('quản trị viên'), UserRole.admin);
    });

    test('nhận diện vai trò Cán bộ quản lý', () {
      expect(
          UserRoleExtension.fromString('canboquanly'), UserRole.canBoQuanLy);
      expect(UserRoleExtension.fromString('cán bộ quản lý'),
          UserRole.canBoQuanLy);
    });

    test('giá trị không hợp lệ mặc định là Sinh viên', () {
      expect(UserRoleExtension.fromString('unknown'), UserRole.sinhVien);
      expect(UserRoleExtension.fromString(''), UserRole.sinhVien);
    });
  });

  group('UserRole.nameInVietnamese', () {
    test('trả về tên tiếng Việt chính xác', () {
      expect(UserRole.sinhVien.nameInVietnamese, 'Sinh viên');
      expect(UserRole.giangVien.nameInVietnamese, 'Giảng viên');
      expect(UserRole.canBoQuanLy.nameInVietnamese, 'Cán bộ quản lý');
      expect(UserRole.admin.nameInVietnamese, 'Quản trị viên hệ thống');
    });
  });

  group('UserModel.fromJson', () {
    test('parse đầy đủ JSON sinh viên', () {
      final json = {
        'id': 'uid-001',
        'userName': '2001230048',
        'fullName': 'Nguyễn Văn An',
        'email': 'an@student.edu.vn',
        'code': '2001230048',
        'role': 'sinhvien',
        'gpa': 3.5,
        'creditsCompleted': 90,
        'totalCredits': 120,
        'attendanceRate': 95,
      };
      final user = UserModel.fromJson(json);

      expect(user.id, 'uid-001');
      expect(user.userName, '2001230048');
      expect(user.fullName, 'Nguyễn Văn An');
      expect(user.email, 'an@student.edu.vn');
      expect(user.role, UserRole.sinhVien);
      expect(user.gpa, 3.5);
      expect(user.creditsCompleted, 90);
      expect(user.totalCredits, 120);
      expect(user.attendanceRate, 95);
    });

    test('parse JSON thiếu fullName dùng userName thay thế', () {
      final json = {
        'id': 'uid-002',
        'userName': 'GV001',
        'email': 'gv@edu.vn',
        'role': 'giangvien',
      };
      final user = UserModel.fromJson(json);
      expect(user.fullName, 'GV001');
      expect(user.role, UserRole.giangVien);
      expect(user.gpa, isNull);
    });

    test('parse JSON với gpa kiểu int vẫn cho ra double', () {
      final json = {
        'id': 'x',
        'userName': 'admin',
        'email': 'a@a.com',
        'role': 'admin',
        'gpa': 4,
      };
      final user = UserModel.fromJson(json);
      expect(user.gpa, 4.0);
      expect(user.gpa, isA<double>());
    });

    test('toJson → fromJson round-trip giữ nguyên dữ liệu', () {
      const original = UserModel(
        id: 'id-rnd',
        userName: '2001230048',
        fullName: 'Test User',
        email: 'test@edu.vn',
        code: '2001230048',
        role: UserRole.sinhVien,
        gpa: 3.2,
        creditsCompleted: 60,
        totalCredits: 120,
      );
      final restored = UserModel.fromJson(original.toJson());

      expect(restored.id, original.id);
      expect(restored.fullName, original.fullName);
      expect(restored.role, original.role);
      expect(restored.gpa, original.gpa);
      expect(restored.creditsCompleted, original.creditsCompleted);
    });
  });
}
