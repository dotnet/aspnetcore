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
    [GeneratedRegex(@"(?<![^\\]\\),")]
    internal static partial Regex DistinguishedNameSeparatorRegex { get; }

    public static async Task RetrieveClaimsAsync(LdapSettings settings, ClaimsIdentity identity, ILogger logger)
    {
        var user = identity.Name!;
        var userAccountNameIndex = user.IndexOf('@');
        var userAccountName = userAccountNameIndex == -1 ? user : user.Substring(0, userAccountNameIndex);

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

        var filter = $"(&(objectClass=user)(sAMAccountName={userAccountName}))"; // This is using ldap search query language, it is looking on the server for someUser
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
                var groupCN = DistinguishedNameSeparatorRegex.Split(groupDN)[0].Substring("CN=".Length);

                if (!settings.IgnoreNestedGroups)
                {
                    GetNestedGroups(settings.LdapConnection, identity, distinguishedName, groupCN, logger, retrievedClaims, new HashSet<string>());
                }
                else
                {
                    retrievedClaims.Add(groupCN);
                }
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

    private static void GetNestedGroups(LdapConnection connection, ClaimsIdentity principal, string distinguishedName, string groupCN, ILogger logger, IList<string> retrievedClaims, HashSet<string> processedGroups)
    {
        retrievedClaims.Add(groupCN);

        var filter = $"(&(objectClass=group)(sAMAccountName={groupCN}))"; // This is using ldap search query language, it is looking on the server for someUser
        var searchRequest = new SearchRequest(distinguishedName, filter, SearchScope.Subtree);
        var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

        if (searchResponse.Entries.Count > 0)
        {
            if (searchResponse.Entries.Count > 1 && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {distinguishedName}");
            }

            var group = searchResponse.Entries[0]; // Get the object that was found on ldap
            var groupDN = group.DistinguishedName;

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

                    GetNestedGroups(connection, principal, distinguishedName, nestedGroupCN, logger, retrievedClaims, processedGroups);
                }
            }
        }
    }
}
