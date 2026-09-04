// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal static partial class LdapAdapter
{
    // Restores pre-fix behaviour: emit role claims for groups that are not security-enabled.
    private const string AllowNonSecurityEnabledGroupClaimsSwitch =
        "Microsoft.AspNetCore.Authentication.Negotiate.AllowNonSecurityEnabledGroupClaims";

    private static readonly bool AllowNonSecurityEnabledGroupClaims =
        AppContext.TryGetSwitch(AllowNonSecurityEnabledGroupClaimsSwitch, out var allow) && allow;

    [GeneratedRegex(@"(?<![^\\]\\),")]
    internal static partial Regex DistinguishedNameSeparator();
    
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

        if (settings.ClaimsCache == null)
        {
            settings.ClaimsCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = settings.ClaimsCacheSize });
        }

        if (settings.ClaimsCache.TryGetValue<IEnumerable<string>>(user, out var cachedClaims) && cachedClaims is not null)
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
            var memberof = userFound.Attributes["memberof"]; // You can access ldap Attributes with Attributes property

            foreach (var group in memberof)
            {
                // Example distinguished name: CN=TestGroup,DC=KERB,DC=local
                var groupDN = $"{Encoding.UTF8.GetString((byte[])group)}";
                var groupCN = DistinguishedNameSeparator().Split(groupDN)[0].Substring("CN=".Length);

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

            settings.ClaimsCache.Set(user,
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
            logger.LogDebug("Stale memberOf reference: group with distinguished name '{GroupDN}' no longer exists; stopping traversal.", groupDN);;
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
                    var nestedGroupCN = DistinguishedNameSeparator().Split(nestedGroupDN)[0].Substring("CN=".Length);

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
