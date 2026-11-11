// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class AuthenticatorTokenProviderTest
{
    [Theory]
    [InlineData(20)]
    [InlineData(200)]
    public async Task ValidateAsyncAcceptsCurrentCode(int keyByteCount)
    {
        var keyBytes = new byte[keyByteCount];
        Random.Shared.NextBytes(keyBytes);
        var key = Base32.ToBase32(keyBytes);
        var user = new PocoUser();
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync(key);
        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var code = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null);
        var provider = new AuthenticatorTokenProvider<PocoUser>();

        Assert.True(await provider.ValidateAsync("purpose", code.ToString(CultureInfo.InvariantCulture), manager.Object, user));
    }

    [Fact]
    public async Task ValidateAsyncThrowsForInvalidKey()
    {
        var user = new PocoUser();
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("MZXW6YT!");
        var provider = new AuthenticatorTokenProvider<PocoUser>();

        await Assert.ThrowsAsync<FormatException>(() => provider.ValidateAsync("purpose", "123456", manager.Object, user));
    }
}
