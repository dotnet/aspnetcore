// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.Extensions.Internal;

public class SecurityHelperTests
{
    [Fact]
    public void AddingToAnonymousIdentityDoesNotKeepAnonymousIdentity()
    {
        var user = SecurityHelper.MergeUserPrincipal(new ClaimsPrincipal(), new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), new string[0]));

        Assert.NotNull(user);
        Assert.Equal("Alpha", user.Identity.AuthenticationType);
        Assert.Equal("Test1", user.Identity.Name);
        Assert.IsAssignableFrom<ClaimsPrincipal>(user);
        Assert.IsAssignableFrom<ClaimsIdentity>(user.Identity);
        Assert.Single(user.Identities);
    }

    [Fact]
    public void AddingExistingIdentityChangesDefaultButPreservesPrior()
    {
        ClaimsPrincipal user = new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), null);

        Assert.Equal("Alpha", user.Identity.AuthenticationType);
        Assert.Equal("Test1", user.Identity.Name);

        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test2", "Beta"), new string[0]));

        Assert.Equal("Beta", user.Identity.AuthenticationType);
        Assert.Equal("Test2", user.Identity.Name);

        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

        Assert.Equal("Gamma", user.Identity.AuthenticationType);
        Assert.Equal("Test3", user.Identity.Name);

        Assert.Equal(3, user.Identities.Count());
        Assert.Equal("Test3", user.Identities.Skip(0).First().Name);
        Assert.Equal("Test2", user.Identities.Skip(1).First().Name);
        Assert.Equal("Test1", user.Identities.Skip(2).First().Name);
    }

    [Fact]
    public void AddingPreservesNewIdentitiesAndDropsEmpty()
    {
        var existingPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var identityNoAuthTypeWithClaim = new ClaimsIdentity();
        identityNoAuthTypeWithClaim.AddClaim(new Claim("identityNoAuthTypeWithClaim", "yes"));
        existingPrincipal.AddIdentity(identityNoAuthTypeWithClaim);
        var identityEmptyWithAuthType = new ClaimsIdentity("empty");
        existingPrincipal.AddIdentity(identityEmptyWithAuthType);

        Assert.False(existingPrincipal.Identity.IsAuthenticated);

        var newPrincipal = new ClaimsPrincipal();
        var newEmptyIdentity = new ClaimsIdentity();
        var identityTwo = new ClaimsIdentity("yep");
        newPrincipal.AddIdentity(newEmptyIdentity);
        newPrincipal.AddIdentity(identityTwo);

        var user = SecurityHelper.MergeUserPrincipal(existingPrincipal, newPrincipal);

        // Preserve newPrincipal order
        Assert.False(user.Identity.IsAuthenticated);
        Assert.Null(user.Identity.Name);

        Assert.Equal(4, user.Identities.Count());
        Assert.Equal(newEmptyIdentity, user.Identities.Skip(0).First());
        Assert.Equal(identityTwo, user.Identities.Skip(1).First());
        Assert.Equal(identityNoAuthTypeWithClaim, user.Identities.Skip(2).First());
        Assert.Equal(identityEmptyWithAuthType, user.Identities.Skip(3).First());

        // This merge should drop newEmptyIdentity since its empty
        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

        Assert.Equal("Gamma", user.Identity.AuthenticationType);
        Assert.Equal("Test3", user.Identity.Name);

        Assert.Equal(4, user.Identities.Count());
        Assert.Equal("Test3", user.Identities.Skip(0).First().Name);
        Assert.Equal(identityTwo, user.Identities.Skip(1).First());
        Assert.Equal(identityNoAuthTypeWithClaim, user.Identities.Skip(2).First());
        Assert.Equal(identityEmptyWithAuthType, user.Identities.Skip(3).First());
    }

    [Fact]
    public void IsAuthenticated_NullUser_ReturnsFalse()
    {
        Assert.False(SecurityHelper.IsAuthenticated(null));
    }

    [Fact]
    public void IsAuthenticated_NoIdentities_ReturnsFalse()
    {
        Assert.False(SecurityHelper.IsAuthenticated(new ClaimsPrincipal()));
    }

    [Fact]
    public void IsAuthenticated_UnauthenticatedIdentity_ReturnsFalse()
    {
        Assert.False(SecurityHelper.IsAuthenticated(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void IsAuthenticated_AuthenticatedIdentity_ReturnsTrue()
    {
        Assert.True(SecurityHelper.IsAuthenticated(new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "custom"))));
    }

    [Fact]
    public void IsAuthenticated_AuthenticatedByNonPrimaryIdentity_ReturnsTrue()
    {
        // The primary identity is unauthenticated, but a later identity is authenticated.
        var user = new ClaimsPrincipal(new[]
        {
            new ClaimsIdentity(),
            new ClaimsIdentity(authenticationType: "custom"),
        });

        Assert.False(user.Identity.IsAuthenticated);
        Assert.True(SecurityHelper.IsAuthenticated(user));
    }

    [Fact]
    public void TryGetUserIdentifier_NullPrincipal_ReturnsFalse()
    {
        Assert.False(TryGetUserIdentifier(null, out _));
    }

    [Fact]
    public void TryGetUserIdentifier_AnonymousPrincipal_ReturnsFalse()
    {
        Assert.False(TryGetUserIdentifier(new ClaimsPrincipal(new ClaimsIdentity()), out _));
    }

    [Fact]
    public void TryGetUserIdentifier_AuthenticatedIdentityWithoutClaims_ReturnsFalse()
    {
        Assert.False(TryGetUserIdentifier(
            new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test")),
            out _));
    }

    [Theory]
    [InlineData("sub")]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData(ClaimTypes.Upn)]
    public void TryGetUserIdentifier_RecognizedClaim_ReturnsTrue(string claimType)
    {
        var principal = CreatePrincipal(new Claim(claimType, "identifier"));

        Assert.True(TryGetUserIdentifier(principal, out _));
    }

    [Fact]
    public void TryGetUserIdentifier_PrefersSubThenNameIdentifierThenUpn()
    {
        var allClaims = CreatePrincipal(
            new Claim(ClaimTypes.Upn, "upn"),
            new Claim(ClaimTypes.NameIdentifier, "name-id"),
            new Claim("sub", "subject"));
        var subjectOnly = CreatePrincipal(new Claim("sub", "subject"));

        Assert.Equal(GetUserIdentifier(subjectOnly), GetUserIdentifier(allClaims));

        var nameAndUpn = CreatePrincipal(
            new Claim(ClaimTypes.Upn, "upn"),
            new Claim(ClaimTypes.NameIdentifier, "name-id"));
        var nameOnly = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "name-id"));

        Assert.Equal(GetUserIdentifier(nameOnly), GetUserIdentifier(nameAndUpn));
    }

    [Fact]
    public void TryGetUserIdentifier_UsesFirstAuthenticatedIdentityWithRecognizedClaim()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim(ClaimTypes.Upn, "first")], "Test"),
            new ClaimsIdentity([new Claim("sub", "second")], "Test"),
        ]);
        var firstIdentityOnly = CreatePrincipal(new Claim(ClaimTypes.Upn, "first"));

        Assert.Equal(GetUserIdentifier(firstIdentityOnly), GetUserIdentifier(principal));
    }

    [Fact]
    public void TryGetUserIdentifier_IgnoresUnauthenticatedIdentities()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "ignored")]),
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "used")], "Test"),
        ]);
        var authenticatedIdentityOnly = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "used"));

        Assert.Equal(GetUserIdentifier(authenticatedIdentityOnly), GetUserIdentifier(principal));
    }

    [Fact]
    public void TryGetUserIdentifier_SkipsEmptyRecognizedClaims()
    {
        var principal = CreatePrincipal(
            new Claim("sub", string.Empty),
            new Claim(ClaimTypes.NameIdentifier, string.Empty),
            new Claim(ClaimTypes.Upn, "upn"));
        var upnOnly = CreatePrincipal(new Claim(ClaimTypes.Upn, "upn"));

        Assert.Equal(GetUserIdentifier(upnOnly), GetUserIdentifier(principal));
    }

    [Fact]
    public void TryGetUserIdentifier_EmptyRecognizedClaimUsesClaimsFallback()
    {
        var principal = CreatePrincipal(new Claim("sub", string.Empty));

        Assert.True(TryGetUserIdentifier(principal, out _));
    }

    [Fact]
    public void TryGetUserIdentifier_ClaimTypeValueAndIssuerAffectOutput()
    {
        var subject = GetUserIdentifier(CreatePrincipal(new Claim("sub", "identifier", ClaimValueTypes.String, "issuer")));
        var nameIdentifier = GetUserIdentifier(CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, "identifier", ClaimValueTypes.String, "issuer")));
        var differentValue = GetUserIdentifier(CreatePrincipal(new Claim("sub", "different", ClaimValueTypes.String, "issuer")));
        var differentIssuer = GetUserIdentifier(CreatePrincipal(new Claim("sub", "identifier", ClaimValueTypes.String, "other-issuer")));

        Assert.NotEqual(subject, nameIdentifier);
        Assert.NotEqual(subject, differentValue);
        Assert.NotEqual(subject, differentIssuer);
    }

    [Fact]
    public void TryGetUserIdentifier_FallsBackToAllClaimsOnAuthenticatedIdentities()
    {
        var first = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("type-b", "value-b")], "Test"),
            new ClaimsIdentity([new Claim("type-a", "value-a")], "Test"),
            new ClaimsIdentity([new Claim("type-c", "ignored")]),
        ]);
        var reordered = new ClaimsPrincipal(
        [
            new ClaimsIdentity(
            [
                new Claim("type-a", "value-a"),
                new Claim("type-b", "value-b"),
            ],
            "Test"),
        ]);

        Assert.Equal(GetUserIdentifier(first), GetUserIdentifier(reordered));
    }

    [Fact]
    public void TryGetUserIdentifier_FallbackPreservesSameTypeClaimOrder()
    {
        var first = CreatePrincipal(
            new Claim("custom", "first"),
            new Claim("custom", "second"));
        var reversed = CreatePrincipal(
            new Claim("custom", "second"),
            new Claim("custom", "first"));

        Assert.NotEqual(GetUserIdentifier(first), GetUserIdentifier(reversed));
    }

    [Fact]
    public void TryGetUserIdentifier_MatchesKnownAntiforgeryDigest()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.Name, "someName"));

        Assert.Equal("yhXE+2v4zSXHtRHmzm4cmrhZca2J0g7yTUwtUerdeF4=", Convert.ToBase64String(GetUserIdentifier(principal)));
    }

    [Fact]
    public void TryGetUserIdentifier_MatchesKnownAntiforgeryDigestForLongUtf8Values()
    {
        var principal = CreatePrincipal(new Claim(
            "sub",
            new string('ü', 200),
            ClaimValueTypes.String,
            "发行者"));

        Assert.Equal("n68b/ma1cEcRL2AyEiy3YE4zE+qGpgMJda+FFN+oqAk=", Convert.ToBase64String(GetUserIdentifier(principal)));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "Test"));

    private static byte[] GetUserIdentifier(ClaimsPrincipal principal)
    {
        Assert.True(TryGetUserIdentifier(principal, out var identifier));
        return identifier;
    }

    private static bool TryGetUserIdentifier(ClaimsPrincipal? principal, out byte[] identifier)
    {
        identifier = new byte[SecurityHelper.UserIdentifierSize];
        return SecurityHelper.TryGetUserIdentifier(principal, identifier);
    }
}
