// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// Reconfigures the NegotiateOptions to defer to the integrated server authentication if present.
/// </summary>
public class PostConfigureNegotiateOptions : IPostConfigureOptions<NegotiateOptions>
{
    private readonly IServerIntegratedAuth? _serverAuth;
    private readonly ILogger<NegotiateHandler> _logger;

    /// <summary>
    /// Creates a new <see cref="PostConfigureNegotiateOptions"/>
    /// </summary>
    /// <param name="serverAuthServices"></param>
    /// <param name="logger"></param>
    public PostConfigureNegotiateOptions(IEnumerable<IServerIntegratedAuth> serverAuthServices, ILogger<NegotiateHandler> logger)
    {
        _serverAuth = serverAuthServices.LastOrDefault();
        _logger = logger;
    }

    /// <summary>
    /// Invoked to post configure a TOptions instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, NegotiateOptions options)
    {
        // If the server supports integrated authentication...
        if (_serverAuth != null)
        {
            // And it's on...
            if (_serverAuth.IsEnabled)
            {
                // Forward to the server if something else wasn't already configured.
                if (options.ForwardDefault == null)
                {
                    Debug.Assert(_serverAuth.AuthenticationScheme != null);
                    options.ForwardDefault = _serverAuth.AuthenticationScheme;
                    options.DeferToServer = true;
                    _logger.Deferring();
                }
            }
            // Otherwise fail, you shouldn't be using this auth handler on a server that supports integrated auth.
            else
            {
                throw new InvalidOperationException("The Negotiate Authentication handler cannot be used on a server that directly supports Windows Authentication."
                    + " Enable Windows Authentication for the server and the Negotiate Authentication handler will defer to it.");
            }
        }

        var ldapSettings = options.LdapSettings;

        if (ldapSettings.EnableLdapClaimResolution)
        {
            ldapSettings.Validate();

            if (ldapSettings.LdapConnection == null)
            {
                var di = new LdapDirectoryIdentifier(server: ldapSettings.Domain, fullyQualifiedDnsHostName: true, connectionless: false);

                if (string.IsNullOrEmpty(ldapSettings.MachineAccountName))
                {
                    // Use default credentials
                    ldapSettings.LdapConnection = new LdapConnection(di);
                }
                else
                {
                    // Use specific specific machine account
                    var machineAccount = ldapSettings.MachineAccountName + "@" + ldapSettings.Domain;
                    var credentials = new NetworkCredential(machineAccount, ldapSettings.MachineAccountPassword);
                    ldapSettings.LdapConnection = new LdapConnection(di, credentials);
                }

                ldapSettings.LdapConnection.SessionOptions.ProtocolVersion = 3; //Setting LDAP Protocol to latest version
                ldapSettings.LdapConnection.Timeout = TimeSpan.FromMinutes(1);
            }

            ldapSettings.LdapConnection.Bind(); // This line actually makes the connection.
        }
    }
}
