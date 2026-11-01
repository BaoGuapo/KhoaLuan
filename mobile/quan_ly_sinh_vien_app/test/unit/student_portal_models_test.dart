import 'package:flutter_test/flutter_test.dart';
import 'package:quan_ly_sinh_vien_app/core/student/student_portal_service.dart';

void main() {
  group('StudentContact.fromJson', () {
    test('parse đầy đủ hồ sơ sinh viên', () {
      final json = {
        'maSinhVien': '2001230048',
        'hoTen': 'Nguyễn Văn An',
        'email': 'an@student.edu.vn',
        'soDienThoai': '0909123456',
        'diaChi': '123 Lê Lợi, Q.1, TP.HCM',
        'soDienThoaiNguoiThan': '0912345678',
        'lopSinhHoat': 'CNTT2020A',
        'nganhHoc': 'Công nghệ thông tin',
        'khoaHoc': 'K2020',
        'ngaySinh': '2002-05-15T00:00:00',
      };
      final contact = StudentContact.fromJson(json);

      expect(contact.studentCode, '2001230048');
      expect(contact.fullName, 'Nguyễn Văn An');
      expect(contact.email, 'an@student.edu.vn');
      expect(contact.phone, '0909123456');
      expect(contact.address, '123 Lê Lợi, Q.1, TP.HCM');
      expect(contact.emergencyPhone, '0912345678');
      expect(contact.className, 'CNTT2020A');
      expect(contact.major, 'Công nghệ thông tin');
      expect(contact.cohort, 'K2020');
      expect(contact.birthDate, isNotNull);
      expect(contact.birthDate!.year, 2002);
      expect(contact.birthDate!.month, 5);
      expect(contact.birthDate!.day, 15);
    });

    test('parse JSON thiếu các trường tùy chọn → giá trị mặc định rỗng', () {
      final json = {
        'maSinhVien': 'SV002',
        'hoTen': 'Test User',
        'email': 'test@edu.vn',
      };
      final contact = StudentContact.fromJson(json);

      expect(contact.phone, '');
      expect(contact.address, '');
      expect(contact.emergencyPhone, '');
      expect(contact.className, '');
      expect(contact.birthDate, isNull);
    });

    test('ngaySinh không hợp lệ → birthDate null', () {
      final json = {
        'maSinhVien': 'SV003',
        'hoTen': 'X',
        'email': 'x@edu.vn',
        'ngaySinh': 'invalid-date',
      };
      final contact = StudentContact.fromJson(json);
      expect(contact.birthDate, isNull);
    });
  });

  group('SemesterGrades.fromJson', () {
    test('parse học kỳ có điểm đầy đủ', () {
      final json = {
        'maHocKy': '2024-1',
        'tenHocKy': 'HK1 2024-2025',
        'tongTinChi': 20,
        'gpaHeBon': 3.5,
        'soMon': 5,
        'monHoc': [
          {'tenMonHoc': 'Lập trình', 'diemTongKet': 8.5},
          {'tenMonHoc': 'Toán', 'diemTongKet': 9.0},
        ],
      };
      final semester = SemesterGrades.fromJson(json);

      expect(semester.code, '2024-1');
      expect(semester.name, 'HK1 2024-2025');
      expect(semester.creditsWithGrades, 20);
      expect(semester.gpa, closeTo(3.5, 0.001));
      expect(semester.courseCount, 5);
      expect(semester.courses, hasLength(2));
    });

    test('gpa null (học kỳ đang học) được parse là null', () {
      final json = {
        'maHocKy': '2026-1',
        'tenHocKy': 'HK1 2026-2027',
        'tongTinChi': 0,
        'gpaHeBon': null,
        'soMon': 10,
        'monHoc': [],
      };
      final semester = SemesterGrades.fromJson(json);
      expect(semester.gpa, isNull);
      expect(semester.courses, isEmpty);
    });

    test('gpa là kiểu int vẫn parse ra double', () {
      final json = {
        'maHocKy': '2025-1',
        'tenHocKy': 'HK1 2025',
        'tongTinChi': 18,
        'gpaHeBon': 4,
        'soMon': 6,
        'monHoc': [],
      };
      final semester = SemesterGrades.fromJson(json);
      expect(semester.gpa, 4.0);
      expect(semester.gpa, isA<double>());
    });
  });

  group('AcademicResults.fromJson', () {
    test('parse kết quả học tập với nhiều học kỳ', () {
      final json = {
        'laDuLieuDemo': true,
        'gpaTichLuyHeBon': 3.2,
        'tongTinChiDaCoDiem': 80,
        'hocKy': [
          {
            'maHocKy': '2024-1',
            'tenHocKy': 'HK1 2024',
            'tongTinChi': 20,
            'gpaHeBon': 3.2,
            'soMon': 5,
            'monHoc': [],
          },
          {
            'maHocKy': '2024-2',
            'tenHocKy': 'HK2 2024',
            'tongTinChi': 20,
            'gpaHeBon': 3.4,
            'soMon': 5,
            'monHoc': [],
          },
        ],
      };
      final results = AcademicResults.fromJson(json);

      expect(results.isDemo, isTrue);
      expect(results.cumulativeGpa, closeTo(3.2, 0.001));
      expect(results.gradedCredits, 80);
      expect(results.semesters, hasLength(2));
      expect(results.semesters.first.code, '2024-1');
    });

    test('isDemo = false khi không có flag demo', () {
      final json = {
        'gpaTichLuyHeBon': 3.8,
        'tongTinChiDaCoDiem': 100,
        'hocKy': [],
      };
      final results = AcademicResults.fromJson(json);
      expect(results.isDemo, isFalse);
      expect(results.semesters, isEmpty);
    });
  });

  group('TuitionFee.fromJson', () {
    test('parse học phí đầy đủ', () {
      final json = {
        'id': 'fee-001',
        'maSinhVien': '2001230048',
        'hoTen': 'Nguyễn Văn An',
        'maHocKy': '2024-1',
        'tongHocPhi': 8500000,
        'daThanhToan': 5000000,
        'hanThanhToan': '2024-12-31T00:00:00Z',
        'ghiChu': 'Đã thanh toán đợt 1',
        'trangThai': 'ChuaThanhToan',
        'laDuLieuDemo': false,
      };
      final fee = TuitionFee.fromJson(json);

      expect(fee.id, 'fee-001');
      expect(fee.studentCode, '2001230048');
      expect(fee.total, 8500000.0);
      expect(fee.paid, 5000000.0);
      expect(fee.remaining, closeTo(3500000.0, 0.01));
      expect(fee.note, 'Đã thanh toán đợt 1');
      expect(fee.isDemo, isFalse);
    });

    test('remaining = total - paid (tính đúng số dư)', () {
      final json = {
        'id': 'fee-002',
        'maSinhVien': 'SV',
        'maHocKy': '2025-1',
        'tongHocPhi': 10000000,
        'daThanhToan': 10000000,
        'hanThanhToan': '2025-12-31T00:00:00Z',
        'ghiChu': '',
        'trangThai': 'DaThanhToan',
      };
      final fee = TuitionFee.fromJson(json);
      expect(fee.remaining, closeTo(0.0, 0.01));
    });

    test('semesterLabel định dạng đúng khi maHocKy dạng YYYY-N', () {
      final json = {
        'id': 'x',
        'maSinhVien': 'SV',
        'maHocKy': '2024-2',
        'tongHocPhi': 0,
        'daThanhToan': 0,
        'hanThanhToan': '2025-01-01T00:00:00Z',
        'ghiChu': '',
        'trangThai': '',
      };
      final fee = TuitionFee.fromJson(json);
      expect(fee.semesterLabel, contains('2024'));
      expect(fee.semesterLabel, contains('2025'));
      expect(fee.semesterLabel, contains('2'));
    });

    test('semesterLabel fallback khi maHocKy không đúng dạng', () {
      final json = {
        'id': 'x',
        'maSinhVien': 'SV',
        'maHocKy': 'INVALID',
        'tongHocPhi': 0,
        'daThanhToan': 0,
        'hanThanhToan': '2025-01-01T00:00:00Z',
        'ghiChu': '',
        'trangThai': '',
      };
      final fee = TuitionFee.fromJson(json);
      expect(fee.semesterLabel, 'INVALID');
    });
  });

  group('StudentPortalException', () {
    test('toString trả về message', () {
      const ex = StudentPortalException('Lỗi kết nối máy chủ');
      expect(ex.toString(), 'Lỗi kết nối máy chủ');
    });
  });
}
