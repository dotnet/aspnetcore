// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.InternalTesting;
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
        var parts = LdapAdapter.DistinguishedNameSeparator().Split("Testing group - City");

        Assert.Equal(new[] { "Testing group - City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparator().Split(@"Testing group\,City");

        Assert.Equal(new[] { @"Testing group\,City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparator().Split("Testing group,City");

        Assert.Equal(new[] { "Testing group", "City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedBackslashAndNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparator().Split(@"Testing group\\,City");

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

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_NoIdentitySid_ReturnsTrue()
        => Assert.True(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: SidToBytes("S-1-5-21-111-222-333-500"),
            identitySid: null,
            user: "Administrator@CONTOSO.COM",
            logger: NullLogger.Instance));

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_NoIdentitySid_RequirePrimarySidClaim_ReturnsFalse()
        => Assert.False(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: SidToBytes("S-1-5-21-111-222-333-500"),
            identitySid: null,
            user: "Administrator@CONTOSO.COM",
            logger: NullLogger.Instance,
            requirePrimarySidClaim: true));

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_NoLdapObjectSid_ReturnsFalse()
        => Assert.False(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: null,
            identitySid: "S-1-5-21-111-222-333-1105",
            user: "Administrator@CONTOSO.COM",
            logger: NullLogger.Instance));

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_MatchingSids_ReturnsTrue()
    {
        // Normal case: the Kerberos-authenticated account and the LDAP entry are the same object.
        const string sid = "S-1-5-21-111-222-333-1105";

        Assert.True(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: SidToBytes(sid),
            identitySid: sid,
            user: "alice@CONTOSO.COM",
            logger: NullLogger.Instance));
    }

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_MatchingSids_CaseInsensitive_ReturnsTrue()
    {
        // SID strings are case-insensitive (S-1-5-... vs s-1-5-...).
        const string sid = "S-1-5-21-111-222-333-1105";

        Assert.True(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: SidToBytes(sid),
            identitySid: sid.ToLowerInvariant(),
            user: "alice@CONTOSO.COM",
            logger: NullLogger.Instance));
    }

    [Fact]
    public void VerifyLdapSidMatchesKerberosPrincipal_MismatchedSids_ReturnsFalse()
        => Assert.False(LdapAdapter.VerifyLdapSidMatchesKerberosPrincipal(
            ldapObjectSidBytes: SidToBytes("S-1-5-21-111-222-333-500"),  // Administrator
            identitySid: "S-1-5-21-111-222-333-1105",                    // non-matching identity sid
            user: "Administrator@CONTOSO.COM",
            logger: NullLogger.Instance));

    [ConditionalTheory]
    [InlineData("S-1-5-21-111-222-333-500")]    // well-known Administrator RID
    [InlineData("S-1-5-21-111-222-333-1105")]   // typical domain user RID
    [InlineData("S-1-5-32-544")]                // BUILTIN\Administrators (short SID)
    [InlineData("S-1-5-18")]                    // NT AUTHORITY\SYSTEM (no sub-domain component)
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "SecurityIdentifier is Windows-only.")]
    public void ParseSidBytesToString_MatchesSecurityIdentifierOnWindows(string sidString)
    {
        // SecurityIdentifier is the authoritative Windows implementation of MS-DTYP §2.4.2.
        var bytes = SidToBytes(sidString);
        var expected = new SecurityIdentifier(bytes, 0).Value;

        Assert.Equal(expected, LdapAdapter.ParseSidBytesToString(bytes), StringComparer.OrdinalIgnoreCase);
    }

    // Builds a binary SID from a canonical string (e.g. "S-1-5-21-111-222-333-500")
    // without relying on the Windows-only SecurityIdentifier class.
    // Format: S-{rev}-{auth}-{sa0}-…-{saN}
    private static byte[] SidToBytes(string sidString)
    {
        var parts = sidString.Split('-');
        // parts: ["S", "{rev}", "{auth}", "{sa0}", ...]
        var revision = byte.Parse(parts[1], CultureInfo.InvariantCulture);
        var authority = long.Parse(parts[2], CultureInfo.InvariantCulture);
        var subAuthorities = parts[3..].Select(p => uint.Parse(p, CultureInfo.InvariantCulture)).ToArray();

        var bytes = new byte[8 + (subAuthorities.Length * 4)];
        bytes[0] = revision;
        bytes[1] = (byte)subAuthorities.Length;

        // Authority as 6-byte big-endian
        var authCopy = authority;
        for (var i = 5; i >= 0; i--)
        {
            bytes[2 + i] = (byte)(authCopy & 0xFF);
            authCopy >>= 8;
        }

        for (var i = 0; i < subAuthorities.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8 + (i * 4)), subAuthorities[i]);
        }

        return bytes;
    }

    [Theory]
    // Valid Security Groups
    [InlineData("-2147483646", true)] // 0x80000000 | 0x2 (Global Security)
    [InlineData("-2147483644", true)] // 0x80000000 | 0x4 (Domain Local Security)
    [InlineData("-2147483640", true)] // 0x80000000 | 0x8 (Universal Security)
    // Valid Distribution Groups
    [InlineData("2", false)]          // 0x2 (Global Distribution)
    [InlineData("4", false)]          // 0x4 (Domain Local Distribution)
    [InlineData("8", false)]          // 0x8 (Universal Distribution)
    // Edge cases and missing attributes fail open
    [InlineData("garbage", true)]
    [InlineData(null, true)]
    public void IsSecurityGroup_EvaluatesGroupTypeAttributeCorrectly(string groupTypeValue, bool expectedIsSecurityGroup)
    {
        // Test parsing from string (common in higher level LDAP wrappers)
        Assert.Equal(expectedIsSecurityGroup, LdapAdapter.IsSecurityGroup(groupTypeValue));

        // Test parsing from byte array (how System.DirectoryServices.Protocols surfaces attributes)
        if (groupTypeValue != null)
        {
            var byteValue = System.Text.Encoding.UTF8.GetBytes(groupTypeValue);
            Assert.Equal(expectedIsSecurityGroup, LdapAdapter.IsSecurityGroup(byteValue));
        }
    }

    [Theory]
    [InlineData("2", false, false)]            // distribution group, switch off -> no role claim
    [InlineData("2", true, true)]              // distribution group, switch on  -> role claim restored
    [InlineData("-2147483646", false, true)]   // security group, switch off     -> role claim
    [InlineData("-2147483646", true, true)]    // security group, switch on      -> role claim
    [InlineData(null, false, true)]            // groupType absent               -> fail open
    public void ShouldEmitRoleClaim_HonoursGroupTypeAndOptOutSwitch(string groupTypeValue, bool allowNonSecurityEnabledGroupClaims, bool expected)
    {
        Assert.Equal(expected, LdapAdapter.ShouldEmitRoleClaim(groupTypeValue, allowNonSecurityEnabledGroupClaims));
    }
}
