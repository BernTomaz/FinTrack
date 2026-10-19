namespace FinTrack.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "FinTrack";
    public string Audience { get; init; } = "FinTrack";
    public string Key { get; init; } = "";
    public int ExpirationMinutes { get; init; } = 120;

    public void Validate(bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience) ||
            string.IsNullOrWhiteSpace(Key) || System.Text.Encoding.UTF8.GetByteCount(Key) < 32 || ExpirationMinutes <= 0 ||
            (!isDevelopment && Key.StartsWith("dev-only-", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Configure Jwt com issuer, audience, chave de pelo menos 32 bytes (exclusiva em produção) e expiração positiva.");
        }
    }
}
