using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace QuanLySinhVien.Api.Services;

public sealed record AttendanceQrPayload(Guid SessionId, string SessionStamp, DateTime ExpiresAtUtc);
public sealed class AttendanceQrTokens(IDataProtectionProvider protection)
{
    private readonly IDataProtector _protector = protection.CreateProtector("EduFace.AttendanceQR.v1");
    public string Issue(Guid sessionId, string stamp, DateTime expires) =>
        "eduface:attendance:" + _protector.Protect(JsonSerializer.Serialize(new AttendanceQrPayload(sessionId, stamp, expires)));
    public AttendanceQrPayload? Read(string token, DateTime now)
    {
        try
        {
            const string prefix = "eduface:attendance:";
            if (string.IsNullOrEmpty(token) || token.Length > 4096 || !token.StartsWith(prefix, StringComparison.Ordinal)) return null;
            var value = JsonSerializer.Deserialize<AttendanceQrPayload>(_protector.Unprotect(token[prefix.Length..]));
            return value is not null && value.ExpiresAtUtc > now && value.ExpiresAtUtc <= now.AddSeconds(35) ? value : null;
        }
        catch (Exception e) when (e is System.Security.Cryptography.CryptographicException or JsonException or FormatException) { return null; }
    }
}
