// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.DirectoryServices.Protocols;
using System.Reflection;
using System.Security.Claims;
using System.Text;
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

    [Fact]
    public void GetGroups_DistributionGroup_IsNotEmittedAndItsParentsAreNotTraversed()
    {
        var claims = ResolveGroups(
            "CN=Distro,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=Distro,DC=x"] = (DistributionGroupType, "CN=Secure,DC=x"),
                ["CN=Secure,DC=x"] = (SecurityGroupType, null),
            });

        Assert.Empty(claims);
    }

    [Fact]
    public void GetGroups_SecurityGroup_IsEmittedAndItsParentsAreTraversed()
    {
        var claims = ResolveGroups(
            "CN=Lower,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=Lower,DC=x"] = (SecurityGroupType, "CN=Upper,DC=x"),
                ["CN=Upper,DC=x"] = (SecurityGroupType, null),
            });

        Assert.Equal(new[] { "Lower", "Upper" }, claims);
    }

    // A distribution list built from a security group, itself nested into another security group.
    // Active Directory does not expand through the distribution group, so neither may we.
    [Fact]
    public void GetGroups_SecurityGroupBehindADistributionGroup_IsNotReached()
    {
        var claims = ResolveGroups(
            "CN=SalesTeam,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=SalesTeam,DC=x"] = (SecurityGroupType, "CN=SalesNews,DC=x"),
                ["CN=SalesNews,DC=x"] = (DistributionGroupType, "CN=FinanceAdmins,DC=x"),
                ["CN=FinanceAdmins,DC=x"] = (SecurityGroupType, null),
            });

        Assert.Equal(new[] { "SalesTeam" }, claims);
    }

    [Fact]
    public void GetGroups_OptOutSwitch_RestoresEmittingAndTraversingDistributionGroups()
    {
        var claims = ResolveGroups(
            "CN=Distro,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=Distro,DC=x"] = (DistributionGroupType, "CN=Secure,DC=x"),
                ["CN=Secure,DC=x"] = (SecurityGroupType, null),
            },
            allowNonSecurityEnabledGroupClaims: true);

        Assert.Equal(new[] { "Distro", "Secure" }, claims);
    }

    [Fact]
    public void GetGroups_MissingGroupType_FailsOpenAndKeepsTraversing()
    {
        var claims = ResolveGroups(
            "CN=Unknown,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=Unknown,DC=x"] = (null, "CN=Secure,DC=x"),
                ["CN=Secure,DC=x"] = (SecurityGroupType, null),
            });

        Assert.Equal(new[] { "Unknown", "Secure" }, claims);
    }

    [Theory]
    [InlineData(SecurityGroupType, new[] { "Direct" })]
    [InlineData(DistributionGroupType, new string[0])]
    public void GetGroups_NestingDisabled_StillAppliesTheSecurityEnabledCheck(string groupType, string[] expected)
    {
        var claims = ResolveGroups(
            "CN=Direct,DC=x",
            new Dictionary<string, (string, string)>
            {
                ["CN=Direct,DC=x"] = (groupType, "CN=Parent,DC=x"),
                ["CN=Parent,DC=x"] = (SecurityGroupType, null),
            },
            resolveNestedGroups: false);

        Assert.Equal(expected, claims);
    }

    private const string SecurityGroupType = "-2147483646";   // GROUP_TYPE_SECURITY_ENABLED | ACCOUNT_GROUP
    private const string DistributionGroupType = "2";         // ACCOUNT_GROUP only

    private static List<string> ResolveGroups(
        string startDN,
        Dictionary<string, (string GroupType, string MemberOf)> directory,
        bool allowNonSecurityEnabledGroupClaims = false,
        bool resolveNestedGroups = true)
    {
        var claims = new List<string>();
        using var connection = new FakeLdapConnection(dn =>
        {
            Assert.True(directory.ContainsKey(dn), $"Unexpected LDAP read for '{dn}'.");
            var entry = directory[dn];
            return CreateGroupResponse(dn, entry.GroupType, entry.MemberOf);
        });

        LdapAdapter.GetGroups(
            connection,
            new ClaimsIdentity(),
            "DC=x",
            startDN,
            startDN.Split(',')[0].Substring("CN=".Length),
            NullLogger.Instance,
            claims,
            new HashSet<string>(),
            allowNonSecurityEnabledGroupClaims,
            resolveNestedGroups);

        return claims;
    }

    private sealed class FakeLdapConnection : LdapConnection
    {
        private readonly Func<string, SearchResponse> _read;

        public FakeLdapConnection(Func<string, SearchResponse> read)
            : base(new LdapDirectoryIdentifier("unused.invalid"))
        {
            _read = read;
        }

        public override DirectoryResponse SendRequest(DirectoryRequest request)
            => _read(((SearchRequest)request).DistinguishedName);
    }

    // The LDAP response types only have internal constructors, so responses are built reflectively.
    // This helper is the single place that depends on those internals.
    private static SearchResponse CreateGroupResponse(string distinguishedName, string groupType, string memberOf)
    {
        const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        var attributes = (SearchResultAttributeCollection)Activator.CreateInstance(
            typeof(SearchResultAttributeCollection), NonPublicInstance, null, null, null);
        var addAttribute = typeof(SearchResultAttributeCollection).GetMethod("Add", NonPublicInstance);

        if (groupType != null)
        {
            addAttribute.Invoke(attributes, new object[] { "groupType", new DirectoryAttribute("groupType", groupType) });
        }

        if (memberOf != null)
        {
            addAttribute.Invoke(attributes, new object[] { "memberof", new DirectoryAttribute("memberof", Encoding.UTF8.GetBytes(memberOf)) });
        }

        var entry = (SearchResultEntry)Activator.CreateInstance(
            typeof(SearchResultEntry), NonPublicInstance, null, new object[] { distinguishedName, attributes }, null);

        var entries = (SearchResultEntryCollection)Activator.CreateInstance(
            typeof(SearchResultEntryCollection), NonPublicInstance, null, null, null);
        typeof(SearchResultEntryCollection).GetMethod("Add", NonPublicInstance).Invoke(entries, new object[] { entry });

        var constructor = typeof(SearchResponse).GetConstructors(NonPublicInstance)[0];
        var arguments = constructor.GetParameters()
            .Select(p => p.ParameterType == typeof(ResultCode) ? (object)ResultCode.Success : null)
            .ToArray();
        var response = (SearchResponse)constructor.Invoke(arguments);

        typeof(SearchResponse)
            .GetProperty("Entries", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .GetSetMethod(nonPublic: true)
            .Invoke(response, new object[] { entries });

        return response;
    }
}
