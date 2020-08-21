// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.DirectoryServices.Protocols;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal static class LdapAdapter
    {
        public static async Task RetrieveClaimsAsync(LdapOptions options, ClaimsIdentity identity, ILogger logger)
        {
            if (!options.EnableLdapRoleClaimResolution)
            {
                return;
            }

            var user = identity.Name;
            var userAccountName = user.Substring(0, user.IndexOf('@'));
            var distinguishedName = options.Domain.Split('.').Select(name => $"dc={name}").Aggregate((a, b) => $"{a},{b}");

            // TODO: extensible search queries
            var filter = $"(&(objectClass=user)(sAMAccountName={userAccountName}))"; // This is using ldap search query language, it is looking on the server for someUser
            var searchRequest = new SearchRequest(distinguishedName, filter, SearchScope.Subtree, null);
            var searchResponse = (SearchResponse) await Task<DirectoryResponse>.Factory.FromAsync(
                options.LdapConnection.BeginSendRequest,
                options.LdapConnection.EndSendRequest,
                searchRequest,
                PartialResultProcessing.NoPartialResultSupport,
                null);

            if (searchResponse.Entries.Count > 0)
            {
                if (searchResponse.Entries.Count > 1)
                {
                    logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {distinguishedName}");
                }

                var userFound = searchResponse.Entries[0]; //Get the object that was found on ldap
                var memberof = userFound.Attributes["memberof"]; // You can access ldap Attributes with Attributes property

                foreach (var group in memberof)
                {
                    // Example distinguished name: CN=TestGroup,DC=KERB,DC=local
                    var groupDN = $"{Encoding.UTF8.GetString((byte[])group)}";
                    var groupCN = groupDN.Split(',')[0].Substring("CN=".Length);

                    if (options.ResolveNestedGroups)
                    {
                        GetNestedGroups(options.LdapConnection, identity, distinguishedName, groupCN, logger);
                    }
                    else
                    {
                        AddRole(identity, groupCN);
                    }
                }
            }
            else
            {
                logger.LogWarning($"No response received for query: {filter} with distinguished name: {distinguishedName}");
            }
        }

        private static void GetNestedGroups(LdapConnection connection, ClaimsIdentity principal, string distinguishedName, string groupCN, ILogger logger)
        {
            var filter = $"(&(objectClass=group)(sAMAccountName={groupCN}))"; // This is using ldap search query language, it is looking on the server for someUser
            var searchRequest = new SearchRequest(distinguishedName, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, null);
            var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
            {
                if (searchResponse.Entries.Count > 1)
                {
                    logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {distinguishedName}");
                }

                var group = searchResponse.Entries[0]; //Get the object that was found on ldap
                string name = group.DistinguishedName;
                AddRole(principal, name);

                var memberof = group.Attributes["memberof"]; // You can access ldap Attributes with Attributes property
                if (memberof != null)
                {
                    foreach (var member in memberof)
                    {
                        var groupDN = $"{Encoding.UTF8.GetString((byte[])member)}";
                        var nestedGroupCN = groupDN.Split(',')[0].Substring("CN=".Length);
                        GetNestedGroups(connection, principal, distinguishedName, nestedGroupCN, logger);
                    }
                }
            }
        }

        private static void AddRole(ClaimsIdentity identity, string role)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, role));
        }
    }
}
