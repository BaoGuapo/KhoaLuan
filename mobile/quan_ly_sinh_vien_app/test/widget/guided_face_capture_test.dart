import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as image;
import 'package:quan_ly_sinh_vien_app/core/face/live_pose_guidance.dart';
import 'package:quan_ly_sinh_vien_app/screens/student/widgets/guided_face_capture.dart';

class _Preview extends StatefulWidget {
  const _Preview(this.mounted, this.unmounted);
  final VoidCallback mounted, unmounted;
  @override
  State<_Preview> createState() => _PreviewState();
}

class _PreviewState extends State<_Preview> {
  @override
  void initState() {
    super.initState();
    widget.mounted();
  }

  @override
  void dispose() {
    widget.unmounted();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      const ColoredBox(color: Color(0xff517681));
}

Uint8List photo(int value) {
  final frame = image.Image(width: 160, height: 160);
  image.fill(frame, color: image.ColorRgb8(value, 80, 120));
  return Uint8List.fromList(image.encodeJpg(frame));
}

Future<void> observations(WidgetTester tester, int count) async {
  for (var i = 0; i < count; i++) {
    await tester.pump(const Duration(milliseconds: 650));
    await tester.pump();
  }
}

void main() {
  testWidgets(
    'wrong pose never advances; stable validated frames preserve one preview',
    (tester) async {
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      var mounts = 0, unmounts = 0, captures = 0, submits = 0;
      var pose = 0.0;
      final preview = _Preview(() => mounts++, () => unmounts++);
      List<String>? submitted;
      Widget screen(int remaining) => MaterialApp(
        home: Scaffold(
          appBar: AppBar(title: const Text('Đăng ký khuôn mặt')),
          body: GuidedFaceCapture(
            preview: preview,
            captureFrame: () async => photo(++captures * 9),
            inspectPose: (_) async =>
                LivePoseObservation(usable: true, message: 'OK', pose: pose),
            onSubmit: (frames) async {
              submits++;
              submitted = frames;
            },
            onCancel: () {},
            direction: 'right',
            identity: 'HKH-SV01 · Sinh viên',
            remaining: remaining,
          ),
        ),
      );
      await tester.pumpWidget(screen(90));
      await tester.tap(find.text('Bắt đầu xác thực trực tiếp'));
      await tester.pump();
      expect(find.byType(Image), findsNothing);
      await observations(tester, 2);
      expect(find.byType(Image), findsOneWidget);
      final first = base64Encode(photo(captures * 9));
      await tester.pumpWidget(screen(85));
      pose = .20;
      await observations(tester, 6);
      expect(find.textContaining('Bạn đang quay ngược hướng'), findsOneWidget);
      expect(find.byType(Image), findsOneWidget);
      pose = -.18;
      await observations(tester, 3);
      expect(find.byType(Image), findsNWidgets(2));
      final second = base64Encode(photo(captures * 9));
      await observations(tester, 4);
      expect(find.textContaining('Quay mặt về chính diện'), findsOneWidget);
      expect(find.byType(Image), findsNWidgets(2));
      pose = .01;
      await observations(tester, 3);
      expect(find.byType(Image), findsNWidgets(3));
      final third = base64Encode(photo(captures * 9));
      expect(submits, 0);
      expect(mounts, 1);
      expect(unmounts, 0);
      await tester.tap(find.text('Nhìn thẳng'));
      await tester.pumpAndSettle();
      expect(find.text('Ảnh bước 1'), findsOneWidget);
      await tester.tap(find.text('Đóng'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Gửi xác minh'));
      await tester.pump();
      expect(submits, 1);
      expect(submitted, [first, second, third]);
      await tester.ensureVisible(find.text('Chụp lại từ đầu'));
      await tester.tap(find.text('Chụp lại từ đầu'));
      await tester.pump();
      expect(find.byType(Image), findsNothing);
      expect(mounts, 1);
      expect(unmounts, 0);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets('duplicate frames never advance, pausing stops polling', (
    tester,
  ) async {
    var inspected = 0, submits = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GuidedFaceCapture(
            preview: const ColoredBox(color: Colors.black),
            captureFrame: () async => photo(30),
            inspectPose: (_) async => LivePoseObservation(
              usable: true,
              message: 'OK',
              pose: ++inspected <= 3 ? 0 : -.18,
            ),
            onSubmit: (_) async {
              submits++;
            },
            onCancel: () {},
            direction: 'right',
            identity: 'Sinh viên',
            remaining: 90,
          ),
        ),
      ),
    );
    await tester.tap(find.text('Bắt đầu xác thực trực tiếp'));
    await tester.pump();
    await observations(tester, 7);
    expect(find.textContaining('Camera trả lại ảnh cũ'), findsOneWidget);
    expect(find.byType(Image), findsOneWidget);
    expect(submits, 0);
    await tester.tap(find.text('Tạm dừng'));
    await tester.pump();
    final count = inspected;
    await observations(tester, 6);
    expect(inspected, count);
  });
  testWidgets(
    'missing face resets stability and response after leaving is discarded',
    (tester) async {
      var calls = 0;
      final pending = Completer<LivePoseObservation>();
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: GuidedFaceCapture(
              preview: const ColoredBox(color: Colors.black),
              captureFrame: () async => photo(++calls * 20),
              inspectPose: (_) async {
                if (calls == 2) {
                  return const LivePoseObservation(
                    usable: false,
                    message: 'Không tìm thấy mặt.',
                  );
                }
                if (calls == 5) return pending.future;
                return const LivePoseObservation(
                  usable: true,
                  message: 'OK',
                  pose: 0,
                );
              },
              onSubmit: (_) async => fail('Must not submit'),
              onCancel: () {},
              direction: 'left',
              identity: 'Sinh viên',
              remaining: 90,
            ),
          ),
        ),
      );
      await tester.tap(find.text('Bắt đầu xác thực trực tiếp'));
      await tester.pump();
      await observations(tester, 1);
      expect(find.text('Không tìm thấy mặt.'), findsOneWidget);
      await observations(tester, 3);
      expect(find.byType(Image), findsNothing);
      await tester.pumpWidget(const SizedBox());
      pending.complete(
        const LivePoseObservation(usable: true, message: 'OK', pose: 0),
      );
      await tester.pump(const Duration(seconds: 10));
      expect(calls, 5);
      expect(tester.takeException(), isNull);
    },
  );
}
