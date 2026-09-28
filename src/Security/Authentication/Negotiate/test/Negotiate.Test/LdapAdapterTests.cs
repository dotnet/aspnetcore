// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Authentication.Negotiate.Test;

public class LdapAdapterTests
{
    [Fact]
    public async Task RetrieveClaimsAsync_ForeignRealm_SkipsLookupAndAddsNoClaims()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp"
        };
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin@foreign.corp")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        Assert.DoesNotContain(identity.Claims, c => c.Type == identity.RoleClaimType);
    }

    [Fact]
    public async Task RetrieveClaimsAsync_ForeignRealm_DifferentCasing_SkipsLookup()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp",
        };
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin@FOREIGN.CORP")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        Assert.DoesNotContain(identity.Claims, c => c.Type == identity.RoleClaimType);
    }

    [Fact]
    public async Task RetrieveClaimsAsync_BareUsernameNoRealm_SkipsLookup()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp",
        };

        // No '@' means we cannot verify which realm "admin" came from.
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        Assert.DoesNotContain(identity.Claims, c => c.Type == identity.RoleClaimType);
    }

    [Fact]
    public async Task RetrieveClaimsAsync_DownLevelDomainUser_SkipsLookup()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp",
        };
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, @"LOCAL\admin")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        Assert.DoesNotContain(identity.Claims, c => c.Type == identity.RoleClaimType);
    }

    [Fact]
    public async Task RetrieveClaimsAsync_MatchingRealm_SameCasing_RetrievesCachedClaims()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp",
        };
        var roles = new[] { "Engineers", "Admins" };
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = settings.ClaimsCacheSize });
        cache.Set("alice@local.corp", (IEnumerable<string>)roles, new MemoryCacheEntryOptions().SetSize(64));
        settings.ClaimsCache = cache;
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice@local.corp")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        var roleClaims = identity.Claims
            .Where(c => c.Type == identity.RoleClaimType)
            .Select(c => c.Value)
            .ToArray();
        Assert.Equal(roles, roleClaims);
    }

    [Fact]
    public async Task RetrieveClaimsAsync_MatchingRealm_DifferentCasing_RetrievesCachedClaims()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            Domain = "local.corp",
        };
        var roles = new[] { "Engineers", "Admins" };
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = settings.ClaimsCacheSize });
        cache.Set("alice@LOCAL.CORP", (IEnumerable<string>)roles, new MemoryCacheEntryOptions().SetSize(64));
        settings.ClaimsCache = cache;
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice@LOCAL.CORP")], "Negotiate");

        await LdapAdapter.RetrieveClaimsAsync(settings, identity, NullLogger.Instance);

        var roleClaims = identity.Claims
            .Where(c => c.Type == identity.RoleClaimType)
            .Select(c => c.Value)
            .ToArray();
        Assert.Equal(roles, roleClaims);
    }

    [Fact]
    public void DistinguishedNameWithoutCommasSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split("Testing group - City");

        Assert.Equal(new[] { "Testing group - City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split(@"Testing group\,City");

        Assert.Equal(new[] { @"Testing group\,City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split("Testing group,City");

        Assert.Equal(new[] { "Testing group", "City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedBackslashAndNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split(@"Testing group\\,City");

        Assert.Equal(new[] { @"Testing group\\", "City" }, parts);
    }

    [Fact]
    public void EscapeLdapFilterValue_PlainValue_ReturnsUnchanged()
    {
        Assert.Equal("JohnDoe", LdapAdapter.EscapeLdapFilterValue("JohnDoe"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Wildcard_IsEscaped()
    {
        Assert.Equal(@"\2a", LdapAdapter.EscapeLdapFilterValue("*"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Parentheses_AreEscaped()
    {
        Assert.Equal(@"John\28Dev\29", LdapAdapter.EscapeLdapFilterValue("John(Dev)"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Backslash_IsEscaped()
    {
        Assert.Equal(@"DOMAIN\5cUser", LdapAdapter.EscapeLdapFilterValue(@"DOMAIN\User"));
    }

    [Fact]
    public void EscapeLdapFilterValue_NullChar_IsEscaped()
    {
        Assert.Equal(@"before\00after", LdapAdapter.EscapeLdapFilterValue("before\0after"));
    }

    [Fact]
    public void EscapeLdapFilterValue_InjectionPayload_IsNeutralized()
    {
        var malicious = "x)(sAMAccountName=*";
        Assert.Equal(@"x\29\28sAMAccountName=\2a", LdapAdapter.EscapeLdapFilterValue(malicious));
    }

    [Fact]
    public void EscapeLdapFilterValue_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LdapAdapter.EscapeLdapFilterValue(string.Empty));
    }
}
