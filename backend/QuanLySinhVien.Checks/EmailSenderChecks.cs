using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLySinhVien.Api.Services;

/// <summary>
/// Dựng một máy chủ SMTP tối giản ngay trong tiến trình rồi cho EmailBackgroundSender gửi thật vào đó,
/// nên kiểm tra được cả phần đối thoại SMTP chứ không chỉ phần soạn nội dung.
/// </summary>
internal static class EmailSenderChecks
{
    private static int _count;

    private static void Check(bool success, string label)
    { if (!success) throw new Exception(label); _count++; Console.WriteLine("PASS: " + label); }

    public static async Task Run()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recipients = new List<string>();
        _ = Task.Run(() => ServeOnceAsync(listener, received, recipients));

        var settings = new EmailSettings
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = port,
            UseStartTls = false,
            FromAddress = "noreply@huit.local.test",
            FromName = "Hệ thống quản lý sinh viên"
        };
        Check(settings.IsConfigured, "cấu hình SMTP hợp lệ");

        var queue = new EmailQueue(NullLogger<EmailQueue>.Instance);
        var sender = new EmailBackgroundSender(queue, settings, NullLogger<EmailBackgroundSender>.Instance);

        // Địa chỉ rỗng và trùng lặp phải bị loại trước khi gửi.
        queue.Queue(new EmailMessage(
            ["sv01@huit.local.test", "sv01@huit.local.test", "  ", "sv02@huit.local.test"],
            "[Cảnh báo chuyên cần] Lập trình Web",
            "Số buổi vắng: 2"));

        using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await sender.StartAsync(stopping.Token);
        var raw = await received.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await sender.StopAsync(CancellationToken.None);
        listener.Stop();

        Check(recipients.Count == 2, "hai địa chỉ nhận, đã bỏ trùng lặp và chuỗi rỗng");
        Check(recipients.Contains("sv01@huit.local.test") && recipients.Contains("sv02@huit.local.test"),
            "đúng hai địa chỉ sinh viên");
        Check(raw.Contains("From: ", StringComparison.Ordinal), "thư có dòng From");
        Check(raw.Contains("noreply@huit.local.test", StringComparison.Ordinal), "thư gửi từ địa chỉ đã cấu hình");
        Check(!raw.Contains("\nTo: sv01@huit.local.test", StringComparison.Ordinal),
            "địa chỉ sinh viên không nằm ở dòng To, nên người nhận không thấy email của nhau");
        Check(!raw.Contains("Bcc:", StringComparison.OrdinalIgnoreCase),
            "danh sách BCC không bị ghi vào phần đầu thư");
        Check(DecodeSubject(raw).Contains("Cảnh báo chuyên cần", StringComparison.Ordinal),
            "tiêu đề tiếng Việt có dấu được mã hóa đúng");

        Console.WriteLine($"PASS: {_count} checks for the email sender.");
    }

    /// <summary>Tiêu đề phi ASCII được mã hóa theo RFC 2047, phải giải mã mới so sánh được.</summary>
    private static string DecodeSubject(string raw)
    {
        var line = raw.Split("\r\n").FirstOrDefault(x => x.StartsWith("Subject:", StringComparison.Ordinal)) ?? "";
        var match = System.Text.RegularExpressions.Regex.Match(line, @"=\?utf-8\?B\?([^?]+)\?=",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success
            ? Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value))
            : line;
    }

    private static async Task ServeOnceAsync(
        TcpListener listener, TaskCompletionSource<string> received, List<string> recipients)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };

            await writer.WriteLineAsync("220 localhost SMTP kiem thu");
            var body = new StringBuilder();
            var inData = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                if (inData)
                {
                    if (line == ".") { inData = false; await writer.WriteLineAsync("250 OK"); received.TrySetResult(body.ToString()); continue; }
                    body.Append(line).Append("\r\n");
                    continue;
                }
                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                { await writer.WriteLineAsync("250-localhost"); await writer.WriteLineAsync("250 8BITMIME"); }
                else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase)) await writer.WriteLineAsync("250 OK");
                else if (line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, "<([^>]+)>");
                    if (match.Success && match.Groups[1].Value != "noreply@huit.local.test")
                        recipients.Add(match.Groups[1].Value);
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                { inData = true; await writer.WriteLineAsync("354 Gui noi dung, ket thuc bang dau cham"); }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                { await writer.WriteLineAsync("221 Bye"); break; }
                else await writer.WriteLineAsync("250 OK");
            }
        }
        catch (Exception e) { received.TrySetException(e); }
    }
}
