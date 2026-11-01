using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

namespace QuanLySinhVien.Api.Services;

public sealed class EmailSettings
{
    public bool Enabled { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseStartTls { get; init; } = true;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Hệ thống quản lý sinh viên";
    /// <summary>Số địa chỉ tối đa cho một lần gửi, để không vượt hạn mức của nhà cung cấp.</summary>
    public int MaxRecipientsPerMessage { get; init; } = 50;

    public bool IsConfigured =>
        Enabled && Host.Length > 0 && FromAddress.Length > 0;
}

public sealed record EmailMessage(IReadOnlyCollection<string> To, string Subject, string Body);

public interface IEmailSender
{
    /// <summary>Xếp thư vào hàng đợi rồi trả về ngay, không giữ request của người dùng.</summary>
    void Queue(EmailMessage message);
}

/// <summary>
/// Nhận thư vào hàng đợi trong bộ nhớ và gửi dần ở tiến trình nền. Hàng đợi không bền:
/// khởi động lại API thì thư chưa gửi sẽ mất, đổi lại việc gửi không bao giờ làm hỏng
/// giao dịch nghiệp vụ đang chạy.
/// </summary>
public sealed class EmailQueue(ILogger<EmailQueue> logger) : IEmailSender
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public void Queue(EmailMessage message)
    {
        var recipients = message.To
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (recipients.Length == 0) return;
        if (!_channel.Writer.TryWrite(message with { To = recipients }))
            logger.LogWarning("Hàng đợi email đã đầy, bỏ qua thư {Subject}.", message.Subject);
    }
}

public sealed class EmailBackgroundSender(
    EmailQueue queue,
    EmailSettings settings,
    ILogger<EmailBackgroundSender> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(message, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Gửi thư hỏng không được làm sập tiến trình nền; thư sau vẫn phải đi tiếp.
                logger.LogError(e, "Không gửi được email {Subject} tới {Count} địa chỉ.",
                    message.Subject, message.To.Count);
            }
        }
    }

    private async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (!settings.IsConfigured)
        {
            logger.LogInformation("Chưa cấu hình SMTP nên chỉ ghi nhật ký: {Subject} -> {Recipients}",
                message.Subject, string.Join(", ", message.To));
            return;
        }

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseStartTls,
            Credentials = settings.UserName.Length > 0
                ? new NetworkCredential(settings.UserName, settings.Password)
                : null
        };

        // Địa chỉ người nhận đặt ở BCC để sinh viên không thấy email của nhau.
        foreach (var batch in message.To.Chunk(Math.Clamp(settings.MaxRecipientsPerMessage, 1, 200)))
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, settings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = false
            };
            mail.To.Add(new MailAddress(settings.FromAddress, settings.FromName));
            foreach (var recipient in batch) mail.Bcc.Add(recipient);
            await client.SendMailAsync(mail, ct);
            logger.LogInformation("Đã gửi email {Subject} tới {Count} địa chỉ.", message.Subject, batch.Length);
        }
    }
}
