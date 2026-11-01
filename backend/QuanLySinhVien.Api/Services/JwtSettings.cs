namespace QuanLySinhVien.Api.Services;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience) || Key.Length < 32)
            throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience and a Jwt:Key of at least 32 characters are required.");
    }
}
