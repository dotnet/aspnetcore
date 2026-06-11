// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionChallengeProtectorTests
{
    private static readonly TimeSpan s_lifetime = TimeSpan.FromMinutes(5);

    private static DeviceBoundSessionChallengeProtector CreateProtector()
        => new(new EphemeralDataProtectionProvider());

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(authenticationType: "test");
        foreach (var (type, value) in claims)
        {
            identity.AddClaim(new Claim(type, value));
        }
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void RegistrationChallenge_RoundTrips_ForSamePrincipal()
    {
        var protector = CreateProtector();
        var principal = Principal(("sub", "alice"));

        var challenge = protector.GenerateRegistrationChallenge(principal, s_lifetime);

        Assert.True(protector.TryValidateRegistrationChallenge(challenge, principal));
    }

    [Fact]
    public void RegistrationChallenge_Fails_ForDifferentPrincipal()
    {
        var protector = CreateProtector();
        var challenge = protector.GenerateRegistrationChallenge(Principal(("sub", "alice")), s_lifetime);

        Assert.False(protector.TryValidateRegistrationChallenge(challenge, Principal(("sub", "bob"))));
    }

    [Fact]
    public void RegistrationChallenge_Fails_WhenTampered()
    {
        var protector = CreateProtector();
        var principal = Principal(("sub", "alice"));
        var challenge = protector.GenerateRegistrationChallenge(principal, s_lifetime);

        var tampered = challenge[0] == 'A' ? "B" + challenge[1..] : "A" + challenge[1..];

        Assert.False(protector.TryValidateRegistrationChallenge(tampered, principal));
    }

    [Fact]
    public void RefreshChallenge_RoundTrips_ForSamePrincipalAndSession()
    {
        var protector = CreateProtector();
        var principal = Principal(("sub", "alice"));

        var challenge = protector.GenerateRefreshChallenge(principal, "session-1", s_lifetime);

        Assert.True(protector.TryValidateRefreshChallenge(challenge, principal, "session-1"));
    }

    [Fact]
    public void RefreshChallenge_Fails_ForWrongSessionId()
    {
        var protector = CreateProtector();
        var principal = Principal(("sub", "alice"));
        var challenge = protector.GenerateRefreshChallenge(principal, "session-1", s_lifetime);

        Assert.False(protector.TryValidateRefreshChallenge(challenge, principal, "session-2"));
    }

    [Fact]
    public void RefreshChallenge_Fails_ForDifferentPrincipal()
    {
        var protector = CreateProtector();
        var challenge = protector.GenerateRefreshChallenge(Principal(("sub", "alice")), "session-1", s_lifetime);

        Assert.False(protector.TryValidateRefreshChallenge(challenge, Principal(("sub", "bob")), "session-1"));
    }

    [Fact]
    public void ComputeClaimUid_PrefersSub_OverNameIdentifier()
    {
        var withSub = Principal(("sub", "the-id"), (ClaimTypes.NameIdentifier, "other"));
        var onlySub = Principal(("sub", "the-id"));

        Assert.Equal(DeviceBoundSessionChallengeProtector.ComputeClaimUid(onlySub),
            DeviceBoundSessionChallengeProtector.ComputeClaimUid(withSub));
    }

    [Fact]
    public void ComputeClaimUid_UsesNameIdentifier_WhenNoSub()
    {
        var withNameId = Principal((ClaimTypes.NameIdentifier, "the-id"), (ClaimTypes.Upn, "other"));
        var onlyNameId = Principal((ClaimTypes.NameIdentifier, "the-id"));

        Assert.Equal(DeviceBoundSessionChallengeProtector.ComputeClaimUid(onlyNameId),
            DeviceBoundSessionChallengeProtector.ComputeClaimUid(withNameId));
    }

    [Fact]
    public void ComputeClaimUid_UsesUpn_WhenNoSubOrNameIdentifier()
    {
        var onlyUpn = Principal((ClaimTypes.Upn, "alice@contoso.com"));

        Assert.False(string.IsNullOrEmpty(DeviceBoundSessionChallengeProtector.ComputeClaimUid(onlyUpn)));
    }
}
