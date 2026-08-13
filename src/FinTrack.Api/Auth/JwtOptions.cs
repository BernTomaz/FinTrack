namespace FinTrack.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "FinTrack";
    public string Audience { get; init; } = "FinTrack";
    public string Key { get; init; } = "";
    public int ExpirationMinutes { get; init; } = 120;
}
