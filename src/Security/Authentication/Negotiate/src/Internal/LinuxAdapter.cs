// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal class LinuxAdapter
    {
        private readonly string _distinguishedName;
        private readonly LdapConnection _connection;
        private readonly ILogger _logger;
        private readonly LdapConnectionOptions _options;

        public LinuxAdapter(NegotiateOptions options, ILogger logger)
        {
            _logger = logger;
            _options = options.LdapConnectionOptions;
            _distinguishedName = _options.Domain.Split('.').Select(name => $"dc={name}").Aggregate((a, b) => $"{a},{b}");

            var di = new LdapDirectoryIdentifier(server: _options.Domain, fullyQualifiedDnsHostName: true, connectionless: false);

            if (string.IsNullOrEmpty(_options.MachineAccountName))
            {
                // Use default credentials
                _connection = new LdapConnection(di);
            }
            else
            {
                // Use specific specific machine account
                var machineAccount = _options.MachineAccountName + "@" + _options.Domain;
                var credentials = new NetworkCredential(machineAccount, _options.MachineAccountPassword);
                _connection = new LdapConnection(di, credentials);
            }

            _connection.SessionOptions.ProtocolVersion = 3; //Setting LDAP Protocol to latest version
            _connection.Timeout = TimeSpan.FromMinutes(1);
            _connection.Bind(); // This line actually makes the connection.
        }

        public Task OnAuthenticatedAsync(AuthenticatedContext context)
        {
            var user = context.Principal.Identity.Name;
            var userAccountName = user.Substring(0, user.IndexOf('@'));

            var filter = $"(&(objectClass=user)(sAMAccountName={userAccountName}))"; // This is using ldap search query language, it is looking on the server for someUser
            var searchRequest = new SearchRequest(_distinguishedName, filter, SearchScope.Subtree, null);
            var searchResponse = (SearchResponse)_connection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
            {
                if (searchResponse.Entries.Count > 1)
                {
                    _logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {_distinguishedName}");
                }

                var userFound = searchResponse.Entries[0]; //Get the object that was found on ldap
                var memberof = userFound.Attributes["memberof"]; // You can access ldap Attributes with Attributes property

                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

                foreach (var group in memberof)
                {
                    // Example distinguished name: CN=TestGroup,DC=KERB,DC=local
                    var groupDN = $"{Encoding.UTF8.GetString((byte[])group)}";
                    var groupCN = groupDN.Split(',')[0].Substring("CN=".Length);

                    if (_options.ResolveNestedGroups)
                    {
                        GetNestedGroups(claimsIdentity, groupCN);
                    }
                    else
                    {
                        AddRole(claimsIdentity, groupCN);
                    }
                }
            }
            else
            {
                _logger.LogWarning($"No response received for query: {filter} with distinguished name: {_distinguishedName}");
            }

            return Task.CompletedTask;
        }

        private void GetNestedGroups(ClaimsIdentity principal, string groupCN)
        {
            var filter = $"(&(objectClass=group)(sAMAccountName={groupCN}))"; // This is using ldap search query language, it is looking on the server for someUser
            var searchRequest = new SearchRequest(_distinguishedName, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, null);
            var searchResponse = (SearchResponse)_connection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
            {
                if (searchResponse.Entries.Count > 1)
                {
                    _logger.LogWarning($"More than one response received for query: {filter} with distinguished name: {_distinguishedName}");
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
                        GetNestedGroups(principal, nestedGroupCN);
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
