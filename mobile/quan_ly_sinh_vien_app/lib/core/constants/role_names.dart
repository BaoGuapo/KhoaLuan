class RoleNames {
  RoleNames._();

  static const admin = 'Admin';
  static const canBoQuanLy = 'CanBoQuanLy';
  static const giangVien = 'GiangVien';
  static const sinhVien = 'SinhVien';

  static String displayName(String role) => switch (role) {
        admin => 'Quản trị viên',
        canBoQuanLy => 'Cán bộ quản lý',
        giangVien => 'Giảng viên',
        sinhVien => 'Sinh viên',
        _ => role,
      };
}
