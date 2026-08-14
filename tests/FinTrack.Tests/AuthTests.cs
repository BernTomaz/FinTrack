using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinTrack.Api.Auth;
using FinTrack.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FinTrack.Tests;

public sealed class AuthTests
{
    [Fact]
    public void PasswordHasherVerifiesOnlyTheOriginalPassword()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("Senha@123");

        Assert.True(hasher.Verify("Senha@123", hash));
        Assert.False(hasher.Verify("outra-senha", hash));
        Assert.False(hasher.Verify("Senha@123", "hash-invalido"));
        Assert.False(hasher.Verify("Senha@123", "100000.salt-invalido.hash-invalido"));
    }

    [Fact]
    public void PasswordHasherRejectsEmptyValues()
    {
        var hasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(() => hasher.Hash(""));
        Assert.False(hasher.Verify("", "hash"));
        Assert.False(hasher.Verify("Senha@123", ""));
    }

    [Fact]
    public void JwtTokenServiceCreatesTokenWithUserClaims()
    {
        var user = new User("Bernardo", "bernardo@email.com", "hash");
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "FinTrack.Tests",
            Audience = "FinTrack.Tests",
            Key = "test-key-with-more-than-32-characters",
            ExpirationMinutes = 30
        }));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(service.Create(user));

        Assert.Equal("FinTrack.Tests", token.Issuer);
        Assert.Contains("FinTrack.Tests", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == user.Email);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Name && claim.Value == user.Name);
    }

    [Fact]
    public void ClaimsPrincipalReturnsAuthenticatedUserId()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

        Assert.Equal(userId, principal.GetUserId());
    }

    [Fact]
    public void ClaimsPrincipalRejectsInvalidUserId()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "invalid")
        ]));

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }
}
