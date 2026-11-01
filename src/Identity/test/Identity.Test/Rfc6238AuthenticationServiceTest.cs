// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

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
}
