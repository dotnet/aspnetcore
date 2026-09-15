// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultClaimUidExtractorTest
{
    private readonly DefaultClaimUidExtractor _claimUidExtractor = new();

    [Fact]
    public void ExtractClaimUid_Unauthenticated()
    {
        var claimUid = new byte[SecurityHelper.UserIdentifierSize];

        var result = _claimUidExtractor.TryExtractClaimUidBytes(
            new ClaimsPrincipal(new ClaimsIdentity()),
            claimUid);

        Assert.False(result);
    }

    [Fact]
    public void ExtractClaimUid_ClaimsIdentity()
    {
        AssertIdentifier(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "someName")], "Test"),
            "yhXE+2v4zSXHtRHmzm4cmrhZca2J0g7yTUwtUerdeF4=");
    }

    [Fact]
    public void DefaultUniqueClaimTypes_NotPresent_SerializesAllClaimTypes()
    {
        AssertIdentifier(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "someone@antiforgery.com"),
                new Claim(ClaimTypes.GivenName, "some"),
                new Claim(ClaimTypes.Surname, "one"),
                new Claim(ClaimTypes.NameIdentifier, string.Empty),
            ],
            "Test"),
            "lgltW+GHRdEC9LyGmcEUGTPE6pgOrwjSmHktalz657A=");
    }

    [Fact]
    public void DefaultUniqueClaimTypes_Present()
    {
        AssertIdentifier(
            new ClaimsIdentity(
            [
                new Claim("fooClaim", "fooClaimValue"),
                new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"),
            ],
            "Test"),
            "bfRch+srZda3DS/0y7rfGzRNk79V8R+YyAdvpqmiDQE=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersSubClaimOverNameIdentifierAndUpn()
    {
        AssertIdentifier(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"),
                new Claim("sub", "subClaimValue"),
                new Claim(ClaimTypes.Upn, "upnClaimValue"),
            ],
            "Test"),
            "x/mnAdQVHyuS608eNGJN799WFwdSXeUFAze99ZzP6OM=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersNameIdentifierOverUpn()
    {
        AssertIdentifier(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"),
                new Claim(ClaimTypes.Upn, "upnClaimValue"),
            ],
            "Test"),
            "bfRch+srZda3DS/0y7rfGzRNk79V8R+YyAdvpqmiDQE=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_UsesUpnIfPresent()
    {
        AssertIdentifier(
            new ClaimsIdentity(
            [
                new Claim("fooClaim", "fooClaimValue"),
                new Claim(ClaimTypes.Upn, "upnClaimValue"),
            ],
            "Test"),
            "s/rFwYSqylQzjDNq3SRzp4J8rHglttP+7jsvHTuXJM8=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_MultipleIdentities_UsesOnlyAuthenticatedIdentities()
    {
        AssertIdentifier(
        [
            new ClaimsIdentity([new Claim("sub", "subClaimValue")]),
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue")],
                "Test"),
        ],
        "bfRch+srZda3DS/0y7rfGzRNk79V8R+YyAdvpqmiDQE=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_NoKnownClaimTypesFound_SortsAndReturnsAllClaimsFromAuthenticatedIdentities()
    {
        AssertIdentifier(
        [
            new ClaimsIdentity([new Claim("sub", "subClaimValue")]),
            new ClaimsIdentity([new Claim(ClaimTypes.Email, "email@domain.com")], "Test"),
            new ClaimsIdentity([new Claim(ClaimTypes.Country, "countryValue")], "Test"),
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "claimName")], "Test"),
        ],
        "yKkuTG96NnpDyUFUte2OT70CzP9JqrxZfsCQdIE6e/E=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersNameFromFirstIdentity_OverSubFromSecondIdentity()
    {
        AssertIdentifier(
        [
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue")],
                "Test"),
            new ClaimsIdentity([new Claim("sub", "subClaimValue")], "Test"),
        ],
        "bfRch+srZda3DS/0y7rfGzRNk79V8R+YyAdvpqmiDQE=");
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersUpnFromFirstIdentity_OverNameFromSecondIdentity()
    {
        AssertIdentifier(
        [
            new ClaimsIdentity([new Claim(ClaimTypes.Upn, "upnValue")], "Test"),
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue")],
                "Test"),
        ],
        "Iv5FFTtADV8RbrkZ9LLM6kyFwhv32gFAIsqBX73Vas8=");
    }

    [Fact]
    public void ExtractClaimUid_MatchesSharedUserIdentifier()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "name-id"),
            new Claim("sub", "subject", ClaimValueTypes.String, "issuer"),
        ],
        "Test"));
        var claimUid = new byte[SecurityHelper.UserIdentifierSize];
        var sharedIdentifier = new byte[SecurityHelper.UserIdentifierSize];

        var result = _claimUidExtractor.TryExtractClaimUidBytes(principal, claimUid);
        var sharedResult = SecurityHelper.TryGetUserIdentifier(principal, sharedIdentifier);

        Assert.True(result);
        Assert.True(sharedResult);
        Assert.Equal(sharedIdentifier, claimUid);
    }

    private void AssertIdentifier(ClaimsIdentity identity, string expected)
        => AssertIdentifier([identity], expected);

    private void AssertIdentifier(IEnumerable<ClaimsIdentity> identities, string expected)
    {
        var claimUid = new byte[SecurityHelper.UserIdentifierSize];

        var result = _claimUidExtractor.TryExtractClaimUidBytes(new ClaimsPrincipal(identities), claimUid);

        Assert.True(result);
        Assert.Equal(expected, Convert.ToBase64String(claimUid));
    }
}
