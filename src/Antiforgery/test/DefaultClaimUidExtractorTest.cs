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

    [Fact]
    public void ExtractClaimUid_MatchesKnownDigest()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "someName")],
            "Test"));
        var claimUid = new byte[SecurityHelper.UserIdentifierSize];

        var result = _claimUidExtractor.TryExtractClaimUidBytes(principal, claimUid);

        Assert.True(result);
        Assert.Equal("yhXE+2v4zSXHtRHmzm4cmrhZca2J0g7yTUwtUerdeF4=", Convert.ToBase64String(claimUid));
    }
}
