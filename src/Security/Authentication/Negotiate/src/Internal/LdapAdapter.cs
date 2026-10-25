// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal static partial class LdapAdapter
{
    // internal for testing
    internal const string RequirePrimarySidClaimSwitchName = "Microsoft.AspNetCore.Authentication.Negotiate.RequirePrimarySidClaim";
    public static bool RequirePrimarySidClaim { get; } = AppContext.TryGetSwitch(RequirePrimarySidClaimSwitchName, out var enabled) && enabled;

    // Restores pre-fix behaviour: emit role claims for groups that are not security-enabled.
    private const string AllowNonSecurityEnabledGroupClaimsSwitch =
        "Microsoft.AspNetCore.Authentication.Negotiate.AllowNonSecurityEnabledGroupClaims";

    private static readonly bool AllowNonSecurityEnabledGroupClaims =
        AppContext.TryGetSwitch(AllowNonSecurityEnabledGroupClaimsSwitch, out var allow) && allow;

    [GeneratedRegex(@"(?<![^\\]\\),")]
    internal static partial Regex DistinguishedNameSeparatorRegex { get; }

    public static async Task RetrieveClaimsAsync(LdapSettings settings, ClaimsIdentity identity, ILogger logger)
    {
        var user = identity.Name!;
        var userAccountNameIndex = user.IndexOf('@');

        // Without a realm in the name (no '@'), we cannot tell which directory the principal came from.
        // The local part alone is not a safe LDAP lookup key — sAMAccountName is unique only within a single domain,
        // so any same-named account in the configured domain would be returned.
        if (userAccountNameIndex == -1)
        {
            logger.LogDebug(
                "Skipping LDAP claim resolution for '{User}': its realm cannot be verified against the configured LDAP domain '{Domain}'.",
                user, settings.Domain);

            return;
        }

        // With a realm that doesn't match settings.Domain the principal authenticated through a foreign Kerberos realm
        // should NOT be trusted by the application's realm.
        var realm = user.AsSpan(userAccountNameIndex + 1);
        if (!realm.Equals(settings.Domain.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "Skipping LDAP claim resolution for '{User}': principal realm does not match the configured LDAP domain '{Domain}'. Cross-domain claim resolution is not supported.",
                user, settings.Domain);

            return;
        }

        var userAccountName = user[..userAccountNameIndex];

        // cache key should have sAMAccountName and SID to uniquely identify user
        var identitySid = identity.FindFirst(ClaimTypes.PrimarySid)?.Value;
        object cacheKey = identitySid is null ? user : (user, identitySid);

        if (settings.ClaimsCache == null)
        {
            settings.ClaimsCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = settings.ClaimsCacheSize });
        }

        if (settings.ClaimsCache.TryGetValue<IEnumerable<string>>(cacheKey, out var cachedClaims) && cachedClaims is not null)
        {
            foreach (var claim in cachedClaims)
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, claim));
            }

            return;
        }

        var distinguishedName = settings.Domain.Split('.').Select(name => $"dc={name}").Aggregate((a, b) => $"{a},{b}");
        var retrievedClaims = new List<string>();

        var filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdapFilterValue(userAccountName)}))"; // This is using ldap search query language, it is looking on the server for someUser
        var searchRequest = new SearchRequest(distinguishedName, filter, SearchScope.Subtree);

        Debug.Assert(settings.LdapConnection != null);
        var searchResponse = (SearchResponse)await Task<DirectoryResponse>.Factory.FromAsync(
            settings.LdapConnection.BeginSendRequest!,
            settings.LdapConnection.EndSendRequest,
            searchRequest,
            PartialResultProcessing.NoPartialResultSupport,
            null);

        if (searchResponse.Entries.Count > 0)
        {
            if (searchResponse.Entries.Count > 1 && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {distinguishedName}");
            }

            var userFound = searchResponse.Entries[0]; //Get the object that was found on ldap

            // Verify the objectSid from the LDAP entry matches the SID of the Kerberos-authenticated principal
            var ldapObjectSidAttr = userFound.Attributes["objectSid"];
            var ldapObjectSidBytes = ldapObjectSidAttr?.Count > 0 ? (byte[])ldapObjectSidAttr[0] : null;
            if (!VerifyLdapSidMatchesKerberosPrincipal(ldapObjectSidBytes, identitySid, user, logger, RequirePrimarySidClaim))
            {
                return;
            }

            var memberof = userFound.Attributes["memberof"]; // You can access ldap Attributes with Attributes property

            foreach (var group in memberof)
            {
                // Example distinguished name: CN=TestGroup,DC=KERB,DC=local
                var groupDN = $"{Encoding.UTF8.GetString((byte[])group)}";
                var groupCN = DistinguishedNameSeparatorRegex.Split(groupDN)[0].Substring("CN=".Length);

                // Resolved via GetGroups even when nesting is disabled, so the security-enabled
                // check applies to directly held groups too.
                GetGroups(settings.LdapConnection, identity, distinguishedName, groupDN, groupCN, logger, retrievedClaims, new HashSet<string>(), AllowNonSecurityEnabledGroupClaims, !settings.IgnoreNestedGroups);
            }

            var entrySize = user.Length * 2; //Approximate the size of stored key in memory cache.
            foreach (var claim in retrievedClaims)
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, claim));
                entrySize += claim.Length * 2; //Approximate the size of stored value in memory cache.
            }

            settings.ClaimsCache.Set(cacheKey,
                retrievedClaims,
                new MemoryCacheEntryOptions()
                    .SetSize(entrySize)
                    .SetSlidingExpiration(settings.ClaimsCacheSlidingExpiration)
                    .SetAbsoluteExpiration(settings.ClaimsCacheAbsoluteExpiration));
        }
        else if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning($"No response received for query: {filter} with distinguished name: {distinguishedName}");
        }
    }

    private static void GetGroups(LdapConnection connection, ClaimsIdentity principal, string distinguishedName, string groupDN, string groupCN, ILogger logger, IList<string> retrievedClaims, HashSet<string> processedGroups, bool allowNonSecurityEnabledGroupClaims, bool resolveNestedGroups)
    {
        // Look up the group entry by its distinguished name using base scope:
        // a base-scope read against the DN is the only LDAP operation that
        // returns exactly the intended group object.
        var searchRequest = new SearchRequest(groupDN, "(objectClass=group)", SearchScope.Base, "memberof", "groupType");
        SearchResponse searchResponse;
        try
        {
            searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
        }
        catch (DirectoryOperationException ex) when (ex.Response is SearchResponse r && r.ResultCode == ResultCode.NoSuchObject)
        {
            // Stale memberOf reference: group no longer exists. Stop traversal of this branch.
            logger.LogDebug("Stale memberOf reference: group with distinguished name '{GroupDN}' no longer exists; stopping traversal.", groupDN);
            return;
        }

        if (searchResponse.Entries.Count > 0)
        {
            var group = searchResponse.Entries[0]; // Get the object that was found on ldap

            var groupTypeAttr = group.Attributes["groupType"];
            var groupTypeValue = groupTypeAttr is { Count: > 0 } ? groupTypeAttr[0] : null;

            if (ShouldEmitRoleClaim(groupTypeValue, allowNonSecurityEnabledGroupClaims))
            {
                retrievedClaims.Add(groupCN);
            }
            else
            {
                logger.LogDebug("LdapAdapter omitted distribution group '{GroupDN}' from role claims.", groupDN);
            }

            if (!resolveNestedGroups)
            {
                return;
            }

            processedGroups.Add(groupDN);

            var memberof = group.Attributes["memberof"]; // You can access ldap Attributes with Attributes property
            if (memberof != null)
            {
                foreach (var member in memberof)
                {
                    var nestedGroupDN = $"{Encoding.UTF8.GetString((byte[])member)}";
                    var nestedGroupCN = DistinguishedNameSeparatorRegex.Split(nestedGroupDN)[0].Substring("CN=".Length);

                    if (processedGroups.Contains(nestedGroupDN))
                    {
                        // We need to keep track of already processed groups because circular references are possible with AD groups
                        return;
                    }

                    GetGroups(connection, principal, distinguishedName, nestedGroupDN, nestedGroupCN, logger, retrievedClaims, processedGroups, allowNonSecurityEnabledGroupClaims, resolveNestedGroups);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the <paramref name="ldapObjectSidBytes"/> from the LDAP search result
    /// represent the same account as the Kerberos-authenticated principal identified by <paramref name="identitySid"/>.
    /// </summary>
    // internal for testing
    internal static bool VerifyLdapSidMatchesKerberosPrincipal(byte[]? ldapObjectSidBytes, string? identitySid, string user, ILogger logger, bool requirePrimarySidClaim = false)
    {
        if (identitySid is null)
        {
            if (requirePrimarySidClaim)
            {
                logger.LogDebug("Skipping LDAP claim resolution for '{User}': the authenticated identity has no PrimarySid claim.", user);
                return false;
            }

            // No PrimarySid claim: either the runtime doesn't populate it, or the Kerberos ticket legitimately carried no PAC (e.g. non-AD KDC, NTLM).
            return true;
        }

        if (ldapObjectSidBytes is null)
        {
            // Valid AD user object should have objectSid attribute
            logger.LogDebug("LDAP entry for '{User}' has no objectSid attribute; skipping claim resolution to prevent potential name confusion.", user);
            return false;
        }

        var ldapSid = ParseSidBytesToString(ldapObjectSidBytes);
        if (ldapSid is null)
        {
            logger.LogDebug("LDAP entry for '{User}' has an objectSid attribute that could not be parsed; skipping claim resolution to prevent potential name confusion.", user);
            return false;
        }

        if (!string.Equals(ldapSid, identitySid, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("LDAP account SID '{LdapSid}' for query '{User}' does not match the Kerberos-authenticated principal SID '{IdentitySid}'. ", ldapSid, user, identitySid);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a binary-encoded Windows SID into its canonical string form (e.g. <c>S-1-5-21-…-500</c>).
    /// Returns <see langword="null"/> if <paramref name="bytes"/> is malformed.
    /// </summary>
    /// <remarks>
    /// The binary format is defined in MS-DTYP §2.4.2:
    /// <list type="bullet">
    ///   <item>Byte 0: Revision</item>
    ///   <item>Byte 1: SubAuthorityCount (N)</item>
    ///   <item>Bytes 2–7: IdentifierAuthority (6 bytes, big-endian)</item>
    ///   <item>Bytes 8+: N sub-authority values (4 bytes each, little-endian)</item>
    /// </list>
    /// This helper is cross-platform and avoids <c>System.Security.Principal.SecurityIdentifier</c>
    /// which is Windows-only.
    /// </remarks>
    // internal for testing
    internal static string? ParseSidBytesToString(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            return null;
        }

        var revision = bytes[0];
        var subAuthorityCount = bytes[1];

        if (bytes.Length < 8 + (subAuthorityCount * 4))
        {
            return null;
        }

        long authority = 0;
        for (var i = 2; i < 8; i++)
        {
            authority = (authority << 8) | bytes[i];
        }

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"S-{revision}-{authority}");

        for (var i = 0; i < subAuthorityCount; i++)
        {
            var subAuth = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8 + (i * 4)));
            sb.Append(CultureInfo.InvariantCulture, $"-{subAuth}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines whether a group should produce a role claim, honouring the
    /// AllowNonSecurityEnabledGroupClaims opt-out.
    /// </summary>
    // internal for testing
    internal static bool ShouldEmitRoleClaim(object? groupTypeValue, bool allowNonSecurityEnabledGroupClaims)
        => IsSecurityGroup(groupTypeValue) || allowNonSecurityEnabledGroupClaims;

    /// <summary>
    /// Evaluates the groupType attribute value (byte[] or string depending on LDAP client implementation)
    /// to determine whether a group is a Security Group (0x80000000).
    /// </summary>
    // internal for testing
    internal static bool IsSecurityGroup(object? groupTypeVal)
    {
        if (groupTypeVal == null)
        {
            return true;
        }

        var typeString = groupTypeVal is byte[] b ? Encoding.UTF8.GetString(b) : groupTypeVal.ToString();
        if (int.TryParse(typeString, out int groupType))
        {
            const int GROUP_TYPE_SECURITY_ENABLED = unchecked((int)0x80000000);
            return (groupType & GROUP_TYPE_SECURITY_ENABLED) == GROUP_TYPE_SECURITY_ENABLED;
        }

        return true; // fail open if we cannot parse
    }

    /// <summary>
    /// Escapes special characters in a value used in an LDAP search filter per RFC 4515
    /// https://datatracker.ietf.org/doc/html/rfc4515#section-3
    /// </summary>
    // internal for testing
    internal static string EscapeLdapFilterValue(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\5c");
                    break;
                case '*':
                    sb.Append(@"\2a");
                    break;
                case '(':
                    sb.Append(@"\28");
                    break;
                case ')':
                    sb.Append(@"\29");
                    break;
                case '\0':
                    sb.Append(@"\00");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
