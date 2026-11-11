// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class Rfc6238AuthenticationServiceTest
{
    private static readonly byte[] SecurityToken = Encoding.Unicode.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Totp:TwoFactor:userId")]
    [InlineData("Totp:ResetPassword:\u00e9\u4e2d\U0001F600")]
    public void GenerateCode_MatchesComputeTotpForCurrentTimestep(string modifier)
    {
        var modifierBytes = modifier is null ? null : Encoding.UTF8.GetBytes(modifier);

        int code;
        long timestep;
        long timestepAfter;
        do
        {
            timestep = GetCurrentTimeStepNumber();
            code = Rfc6238AuthenticationService.GenerateCode(SecurityToken, modifier);
            timestepAfter = GetCurrentTimeStepNumber();
        }
        while (timestep != timestepAfter);

        Assert.Equal(Rfc6238AuthenticationService.ComputeTotp(SecurityToken, (ulong)timestep, modifierBytes), code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Totp:TwoFactor:userId")]
    [InlineData("Totp:ResetPassword:\u00e9\u4e2d\U0001F600")]
    public void ValidateCode_AcceptsCodesWithinAllowedTimestepWindow(string modifier)
    {
        var modifierBytes = modifier is null ? null : Encoding.UTF8.GetBytes(modifier);
        var timestep = GetCurrentTimeStepNumber();

        // Offsets -1..1 stay inside the +/-2 validation window even if the timestep advances during the test.
        for (var offset = -1; offset <= 1; offset++)
        {
            var code = Rfc6238AuthenticationService.ComputeTotp(SecurityToken, (ulong)(timestep + offset), modifierBytes);
            Assert.True(Rfc6238AuthenticationService.ValidateCode(SecurityToken, code, modifier));
        }
    }

    private static long GetCurrentTimeStepNumber()
        => (DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).Ticks / TimeSpan.FromMinutes(3).Ticks;

    public static TheoryData<string, int> WindowKeysAndModifiers => new()
    {
        { null, 0 },
        { null, 10 },
        { null, 64 },
        { null, 100 },
        { "", 20 },
        { "Totp:TwoFactor:userId", 64 },
        { "Totp:ResetPassword:\u00e9\u4e2d\U0001F600", 20 },
    };

    [Theory]
    [MemberData(nameof(WindowKeysAndModifiers))]
    public void ValidateCodeInWindow_MatchesReferenceImplementation(string modifier, int keyLength)
    {
        var key = new byte[keyLength];
        new Random(keyLength).NextBytes(key);
        var modifierBytes = modifier is null ? null : Encoding.UTF8.GetBytes(modifier);
        const long timestep = 9_876_543;

        var candidates = new List<int> { -1, 0, 999_999 };
        for (var offset = -4; offset <= 4; offset++)
        {
            candidates.Add(Rfc6238AuthenticationService.ComputeTotp(key, (ulong)(timestep + offset), modifierBytes));
        }

        foreach (var code in candidates)
        {
            var expected = false;
            for (var offset = -2; offset <= 2; offset++)
            {
                expected |= Rfc6238AuthenticationService.ComputeTotp(key, (ulong)(timestep + offset), modifierBytes) == code;
            }

            Assert.Equal(expected, Rfc6238AuthenticationService.ValidateCodeInWindow(key, code, timestep, modifier));
        }

        for (var offset = -2; offset <= 2; offset++)
        {
            Assert.True(Rfc6238AuthenticationService.ValidateCodeInWindow(
                key, Rfc6238AuthenticationService.ComputeTotp(key, (ulong)(timestep + offset), modifierBytes), timestep, modifier));
        }
    }

    [Fact]
    public async Task AuthenticatorTokenProvider_ValidatesCodesWithinWindow()
    {
        var key = Base32.GenerateBase32();
        var keyBytes = Base32.FromBase32(key);
        var user = new PocoUser();
        var store = new Mock<IUserAuthenticatorKeyStore<PocoUser>>();
        store.Setup(s => s.GetAuthenticatorKeyAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(key);
        var manager = MockHelpers.TestUserManager<PocoUser>(store.Object);
        var provider = new AuthenticatorTokenProvider<PocoUser>();

        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Offsets -1..1 stay inside the +/-2 validation window even if the timestep advances during the test.
        for (var offset = -1; offset <= 1; offset++)
        {
            var code = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)(timestep + offset), modifierBytes: null);
            Assert.True(await provider.ValidateAsync("purpose", code.ToString("D6", CultureInfo.InvariantCulture), manager, user));
        }

        Assert.False(await provider.ValidateAsync("purpose", "not-a-code", manager, user));
    }
}
