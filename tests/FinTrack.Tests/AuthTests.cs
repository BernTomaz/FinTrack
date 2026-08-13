using FinTrack.Api.Auth;

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
    }
}
