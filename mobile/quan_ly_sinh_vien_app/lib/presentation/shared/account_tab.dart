import 'dart:typed_data';

import 'package:flutter/material.dart';

import '../../core/config/api_config.dart';
import '../../core/constants/role_names.dart';
import '../../core/network/portal_api.dart';
import '../auth/auth_controller.dart';
import '../auth/change_password_screen.dart';
import 'ui.dart';

class _Profile {
  const _Profile(this.info, this.photo);

  final Map<String, dynamic> info;
  final Uint8List? photo;
}

class AccountTab extends StatefulWidget {
  const AccountTab({required this.controller, this.extraTiles = const [], super.key});

  final AuthController controller;

  /// Các mục riêng theo vai trò, ví dụ hồ sơ khuôn mặt của sinh viên.
  final List<Widget> extraTiles;

  @override
  State<AccountTab> createState() => _AccountTabState();
}

class _AccountTabState extends State<AccountTab> {
  final _api = PortalApi();
  late Future<_Profile> _profile = _load();

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  bool get _isStudent =>
      widget.controller.session?.roles.contains(RoleNames.sinhVien) ?? false;

  Future<_Profile> _load() async {
    final info = await _api.getObject('api/v1/nguoi-dung/me');
    Uint8List? photo;
    if (_isStudent) {
      // Ảnh thẻ không bắt buộc; thiếu ảnh hoặc lỗi tải thì dùng chữ cái đầu tên.
      try {
        final bytes = await _api.getBytes('api/v1/anh-the/cua-toi');
        if (bytes != null && bytes.isNotEmpty) photo = Uint8List.fromList(bytes);
      } catch (_) {}
    }
    return _Profile(info, photo);
  }

  Future<void> _reload() async {
    final next = _load();
    setState(() => _profile = next);
    try {
      await next;
    } catch (_) {
      // Lỗi đã hiển thị qua FutureBuilder.
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = widget.controller.session;
    if (session == null) return const SizedBox.shrink();

    return FutureBuilder<_Profile>(
      future: _profile,
      builder: (context, snapshot) {
        final info = snapshot.data?.info ?? const <String, dynamic>{};
        final academic =
            Map<String, dynamic>.from(info['academicProfile'] as Map? ?? const {});
        final fullName = asText(info['fullName']).trim();
        final code = asText(info['profileCode'], session.userName);

        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              _ProfileHeader(
                fullName: fullName.isEmpty ? session.userName : fullName,
                code: code,
                roles: session.roles.map(RoleNames.displayName).join(', '),
                photo: snapshot.data?.photo,
                loading: snapshot.connectionState != ConnectionState.done,
              ),
              if (snapshot.hasError)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Card(
                    child: ListTile(
                      leading: const Icon(Icons.cloud_off_outlined),
                      title: const Text('Không tải được hồ sơ'),
                      subtitle: Text(snapshot.error.toString()),
                      trailing: TextButton(onPressed: _reload, child: const Text('Thử lại')),
                    ),
                  ),
                ),
              if (snapshot.hasData) ..._details(info, academic),
              const SectionTitle('Cài đặt'),
              Card(
                child: Column(
                  children: [
                    ...widget.extraTiles,
                    ListTile(
                      leading: const Icon(Icons.password),
                      title: const Text('Đổi mật khẩu'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => Navigator.of(context).push(
                        MaterialPageRoute<void>(
                          builder: (_) => ChangePasswordScreen(
                            controller: widget.controller,
                            isRequired: false,
                          ),
                        ),
                      ),
                    ),
                    ListTile(
                      leading: const Icon(Icons.logout, color: AttendanceStatus.absent),
                      title: const Text(
                        'Đăng xuất',
                        style: TextStyle(color: AttendanceStatus.absent),
                      ),
                      onTap: widget.controller.isBusy ? null : () => _confirmLogout(context),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Text(
                'Máy chủ: ${ApiConfig.baseUrl}',
                textAlign: TextAlign.center,
                style: Theme.of(context)
                    .textTheme
                    .bodySmall
                    ?.copyWith(color: const Color(0xFF8A9BAE)),
              ),
            ],
          ),
        );
      },
    );
  }

  List<Widget> _details(Map<String, dynamic> info, Map<String, dynamic> academic) {
    final birthday = parseDate(academic['ngaySinh']);
    final contact = <(IconData, String, String?)>[
      (Icons.email_outlined, 'Email', asText(info['email'])),
      (Icons.phone_outlined, 'Số điện thoại', asText(info['phoneNumber'])),
      (Icons.cake_outlined, 'Ngày sinh', birthday == null ? null : dateFormat.format(birthday)),
      (Icons.wc_outlined, 'Giới tính', asText(academic['gioiTinh'])),
    ];
    final study = _isStudent
        ? <(IconData, String, String?)>[
            (Icons.groups_outlined, 'Lớp', asText(academic['lopHoc'])),
            (Icons.school_outlined, 'Ngành', asText(academic['nganh'])),
            (Icons.auto_stories_outlined, 'Chuyên ngành', asText(academic['chuyenNganh'])),
            (Icons.account_balance_outlined, 'Khoa', asText(academic['khoa'])),
            (Icons.date_range_outlined, 'Khóa học', asText(academic['khoaHoc'])),
            (Icons.workspace_premium_outlined, 'Bậc đào tạo', asText(academic['bacDaoTao'])),
          ]
        : <(IconData, String, String?)>[
            (Icons.account_balance_outlined, 'Khoa', asText(academic['khoa'])),
            (Icons.auto_stories_outlined, 'Chuyên ngành', asText(academic['chuyenNganh'])),
            (Icons.badge_outlined, 'Chức danh', asText(academic['chucDanh'])),
            (Icons.workspace_premium_outlined, 'Trình độ', asText(academic['trinhDoHocVan'])),
          ];

    return [
      ..._section('Thông tin liên hệ', contact),
      ..._section(_isStudent ? 'Thông tin học tập' : 'Thông tin công tác', study),
    ];
  }

  /// Chỉ hiện các trường có dữ liệu; cả nhóm trống thì ẩn luôn.
  List<Widget> _section(String title, List<(IconData, String, String?)> rows) {
    final filled = rows.where((x) => x.$3 != null && x.$3!.trim().isNotEmpty).toList();
    if (filled.isEmpty) return const [];
    return [
      SectionTitle(title),
      Card(
        child: Column(
          children: [
            for (final (i, row) in filled.indexed) ...[
              if (i > 0) const Divider(height: 1, indent: 56),
              ListTile(
                leading: Icon(row.$1, color: const Color(0xFF5B6B7C)),
                title: Text(
                  row.$2,
                  style: const TextStyle(fontSize: 12, color: Color(0xFF5B6B7C)),
                ),
                subtitle: Text(
                  row.$3!,
                  style: const TextStyle(fontSize: 15, color: Color(0xFF1B2733)),
                ),
              ),
            ],
          ],
        ),
      ),
    ];
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc chắn muốn đăng xuất?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Đăng xuất')),
        ],
      ),
    );
    if (confirmed == true) await widget.controller.logout();
  }
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader({
    required this.fullName,
    required this.code,
    required this.roles,
    required this.photo,
    required this.loading,
  });

  final String fullName;
  final String code;
  final String roles;
  final Uint8List? photo;
  final bool loading;

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    final initials = fullName.trim().isEmpty
        ? '?'
        : fullName.trim().split(RegExp(r'\s+')).last[0].toUpperCase();

    return Card(
      color: primary,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            CircleAvatar(
              radius: 36,
              backgroundColor: Colors.white,
              child: CircleAvatar(
                radius: 33,
                backgroundColor: const Color(0xFFE3F2FD),
                backgroundImage: photo == null ? null : MemoryImage(photo!),
                child: photo == null
                    ? Text(
                        initials,
                        style: TextStyle(
                          color: primary,
                          fontSize: 28,
                          fontWeight: FontWeight.w700,
                        ),
                      )
                    : null,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    fullName,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 20,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    code,
                    style: const TextStyle(color: Colors.white, fontSize: 15),
                  ),
                  const SizedBox(height: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      roles,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            if (loading)
              const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
              ),
          ],
        ),
      ),
    );
  }
}
