// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
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
    internal static partial Regex DistinguishedNameSeparator();

    public static async Task RetrieveClaimsAsync(LdapSettings settings, ClaimsIdentity identity, ILogger logger)
    {
        var user = identity.Name!;
        var userAccountNameIndex = user.IndexOf('@');
        var userAccountName = userAccountNameIndex == -1 ? user : user.Substring(0, userAccountNameIndex);

        if (settings.ClaimsCache == null)
        {
            settings.ClaimsCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = settings.ClaimsCacheSize });
        }

        if (settings.ClaimsCache.TryGetValue<IEnumerable<KeyValuePair<string, string>>>(user, out var cachedClaims) && cachedClaims is not null)
        {
            foreach (var claim in cachedClaims)
            {
                identity.AddClaim(new Claim(claim.Key, claim.Value));
            }

            return;
        }

        var distinguishedName = settings.Domain.Split('.').Select(name => $"dc={name}").Aggregate((a, b) => $"{a},{b}");
        var retrievedClaims = new List<KeyValuePair<string, string>>();
        // sAMAccountName is always unique (at least within a forest)
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

            var userFound = searchResponse.Entries[0]; // Get the object that was found on ldap
            var memberof = userFound.Attributes["memberof"]; // You can access ldap Attributes with Attributes property

            // Get the user SID
            if (settings.EnableLdapSIDClaimResolution)
            {
                var userSID = userFound.Attributes["objectsid"];
                if (userSID is { Count: 1 })
                {
                    var usid = ParseSID((byte[])userSID[0]);
                    if (usid is string)
                    {
                        retrievedClaims.Add(new(ClaimTypes.PrimarySid, usid));

                        // Add the primaryGroupID as claim
                        var primaryGID = userFound.Attributes["primarygroupid"];
                        if (primaryGID is { Count: 1 })
                        {
                            if (primaryGID[0] is string)
                            {
                                if (((string)primaryGID[0]).All(Char.IsDigit))
                                {
                                    // The primaryGroupID attribute is a relative ID (RID).
                                    // To construct the SID the authority part needs to be
                                    // copied from the user SID.
                                    int lastIndex = usid.LastIndexOf('-');
                                    if (lastIndex > 0)
                                    {
                                        retrievedClaims.Add(new(ClaimTypes.PrimaryGroupSid, string.Concat(usid.Substring(0,lastIndex + 1), (string)primaryGID[0])));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (memberof is not null)
            {
                var uniqueGroups = !settings.IgnoreNestedGroups ? new HashSet<string>() : null;
                foreach (var group in memberof)
                {
                    // Example distinguished name: CN=TestGroup,DC=KERB,DC=local
                    var groupDN = $"{Encoding.UTF8.GetString((byte[])group)}";
                    var groupCN = DistinguishedNameSeparator().Split(groupDN)[0].Substring("CN=".Length);

                    if (!settings.IgnoreNestedGroups)
                    {
                        // Due to the instantiation condition of uniqueGroups it will not be null here
                        GetNestedGroups(settings.LdapConnection, identity, distinguishedName, groupCN, logger, retrievedClaims, uniqueGroups!, settings.EnableLdapSIDClaimResolution);
                    }
                    else
                    {
                        retrievedClaims.Add(new(identity.RoleClaimType, groupCN));
                        if (settings.EnableLdapSIDClaimResolution)
                        {
                            var groupSID = GetGroupSID(settings.LdapConnection, distinguishedName, groupCN, logger);
                            if (groupSID is not null)
                            {
                                retrievedClaims.Add(new(ClaimTypes.GroupSid, groupSID));
                            }
                        }
                    }
                }
            }

            var entrySize = user.Length * 2; // Approximate the size of stored key in memory cache.
            foreach (var claim in retrievedClaims)
            {
                identity.AddClaim(new Claim(claim.Key, claim.Value));
                // Approximate the size of stored value in memory cache.
                entrySize += claim.Value.Length * 2;
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

    private static void GetNestedGroups(LdapConnection connection, ClaimsIdentity principal, string distinguishedName, string groupCN, ILogger logger, IList<KeyValuePair<string, string>> retrievedClaims, HashSet<string> processedGroups, bool resolveSIDs)
    {
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

            if (processedGroups.Contains(groupDN))
            {
                // No need to continue, this group was resolved before
                return;
            }

            retrievedClaims.Add(new(principal.RoleClaimType, groupCN));
            processedGroups.Add(groupDN);

            if (resolveSIDs)
            {
                // Get the group SID
                var groupSID = group.Attributes["objectsid"];
                if (groupSID is { Count: 1 })
                {
                    // For some reason it is sometimes string and sometimes byte[] when it is returned as a string, then every byte is converted to a char and simply put together as a string
                    switch (groupSID[0])
                    {
                        case string groupSIDstr:
                            // The maximum permitted size of a SID is 1 + 1 + 6 + 4 * MaxSubAuthorities
                            // and to avoid unsafe dynamic stackalloc allocations a max static allocation and
                            // slice method will be used here. The maximum size will be rounded up to the
                            // next power of two to increase allocation speed.
                            // Because this is a recursive function the stack allocation may need to be replaced
                            // by a slower heap allocation.
                            int allocSize = (int)BitOperations.RoundUpToPowerOf2((uint)(1 + 1 + 6 + 4 * MaxSubAuthorities));
                            if (groupSIDstr.Length <= allocSize)
                            {
                                Span<byte> lgroupSIDba = stackalloc byte[allocSize];
                                for (int i = 0; i < groupSIDstr.Length; ++i)
                                {
                                    lgroupSIDba[i] = Convert.ToByte(groupSIDstr[i]);
                                }
                                var lgsid = ParseSID(lgroupSIDba.Slice(0, groupSIDstr.Length));
                                if (lgsid is not null)
                                {
                                    retrievedClaims.Add(new(ClaimTypes.GroupSid, lgsid));
                                }
                            }
                            break;
                        case byte[] groupSIDba:
                            var gsid = ParseSID(groupSIDba);
                            if (gsid is not null)
                            {
                                retrievedClaims.Add(new(ClaimTypes.GroupSid, gsid));
                            }
                            break;
                    }
                }
            }

            var memberof = group.Attributes["memberof"]; // You can access ldap Attributes with Attributes property
            if (memberof is not null)
            {
                foreach (var member in memberof)
                {
                    var nestedGroupDN = $"{Encoding.UTF8.GetString((byte[])member)}";
                    var nestedGroupCN = DistinguishedNameSeparator().Split(nestedGroupDN)[0].Substring("CN=".Length);

                    if (processedGroups.Contains(nestedGroupDN))
                    {
                        // We need to keep track of already processed groups because circular references are possible with AD groups
                        continue;
                    }

                    GetNestedGroups(connection, principal, distinguishedName, nestedGroupCN, logger, retrievedClaims, processedGroups, resolveSIDs);
                }
            }
        }
    }

    private static string? GetGroupSID(LdapConnection connection, string distinguishedName, string groupCN, ILogger logger)
    {
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
            var groupSID = group.Attributes["objectsid"];
            if (groupSID is { Count: 1 })
            {
                switch (groupSID[0])
                {
                    case string groupSIDstr:
                        // The maximum permitted size of a SID is 1 + 1 + 6 + 4 * MaxSubAuthorities
                        // and to avoid unsafe dynamic stackalloc allocations a max static allocation and
                        // slice method will be used here. The maximum size will be rounded up to the
                        // next power of two to increase allocation speed.
                        int allocSize = (int)BitOperations.RoundUpToPowerOf2((uint)(1 + 1 + 6 + 4 * MaxSubAuthorities));
                        if (groupSIDstr.Length <= allocSize)
                        {
                            Span<byte> lgroupSIDba = stackalloc byte[allocSize];
                            for (int i = 0; i < groupSIDstr.Length; ++i)
                            {
                                lgroupSIDba[i] = Convert.ToByte(groupSIDstr[i]);
                            }
                            return ParseSID(lgroupSIDba.Slice(0, groupSIDstr.Length));
                        }
                        break;
                    case byte[] groupSIDba:
                        return ParseSID(groupSIDba);
                }
            }
        }

        return null;
    }

    // Straight from SID.cs of the .NET runtime library
    internal const int MaxSubAuthorities = 15;
    internal const long MaxIdentifierAuthority = 0xFFFFFFFFFFFF;
    private const int MinBinaryLength = 1 + 1 + 6; // Revision (1) + subauth count (1) + identifier authority (6)
    private const byte Revision = 1;
    internal enum IdentifierAuthority : long
    {
        NullAuthority = 0,
        WorldAuthority = 1,
        LocalAuthority = 2,
        CreatorAuthority = 3,
        NonUniqueAuthority = 4,
        NTAuthority = 5,
        SiteServerAuthority = 6,
        InternetSiteAuthority = 7,
        ExchangeAuthority = 8,
        ResourceManagerAuthority = 9,
    }

    private static string? ParseSID(ReadOnlySpan<byte> binaryForm)
    {
        // See SID.cs of the .NET runtime library
        if (binaryForm.Length < MinBinaryLength)
        {
            return null;
        }
        if (binaryForm[0] != Revision)
        {
            return null;
        }
        int subAuthoritiesLength = binaryForm[1];
        if (subAuthoritiesLength > MaxSubAuthorities)
        {
            return null;
        }
        int totalLength = 1 + 1 + 6 + 4 * subAuthoritiesLength;
        if (binaryForm.Length < totalLength)
        {
            return null;
        }
        // Already checked that subAuthoritiesLength <= MaxSubAuthorities
        Span<int> subAuthorities = stackalloc int[subAuthoritiesLength];
        IdentifierAuthority authority = (IdentifierAuthority)(
            (((long)binaryForm[2]) << 40) +
            (((long)binaryForm[3]) << 32) +
            (((long)binaryForm[4]) << 24) +
            (((long)binaryForm[5]) << 16) +
            (((long)binaryForm[6]) << 8) +
            (((long)binaryForm[7]))
        );

        for (int i = 0; i < subAuthoritiesLength; i++)
        {
            subAuthorities[i] =
                (int)(
                (((uint)binaryForm[8 + 4 * i + 0]) << 0) +
                (((uint)binaryForm[8 + 4 * i + 1]) << 8) +
                (((uint)binaryForm[8 + 4 * i + 2]) << 16) +
                (((uint)binaryForm[8 + 4 * i + 3]) << 24)
            );
        }

        if (authority < 0 || (long)authority > MaxIdentifierAuthority)
        {
            return null;
        }

        Span<char> result = stackalloc char[189];
        result[0] = 'S';
        result[1] = '-';
        result[2] = '1';
        result[3] = '-';
        int length = 4;
        ((ulong)authority).TryFormat(result.Slice(length), out int written, provider: CultureInfo.InvariantCulture);
        length += written;
        for (int index = 0; index < subAuthorities.Length; index++)
        {
            result[length] = '-';
            length += 1;
            ((uint)subAuthorities[index]).TryFormat(result.Slice(length), out written, provider: CultureInfo.InvariantCulture);
            length += written;
        }
        return result.Slice(0, length).ToString();
    }
}
