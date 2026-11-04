// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// Options class for configuring LDAP connections on Linux
/// </summary>
public class LdapSettings
{
    /// <summary>
    /// Configure whether LDAP connection should be used to resolve claims.
    /// This is mainly used on Linux.
    /// </summary>
    public bool EnableLdapClaimResolution { get; set; }

    /// <summary>
    /// The domain to use for the LDAP connection. This is a mandatory setting.
    /// </summary>
    /// <example>
    /// DOMAIN.com
    /// </example>
    public string Domain { get; set; } = default!;

    /// <summary>
    /// The machine account name to use when opening the LDAP connection.
    /// If this is not provided, the machine wide credentials of the
    /// domain joined machine will be used.
    /// </summary>
    public string? MachineAccountName { get; set; }

    /// <summary>
    /// The machine account password to use when opening the LDAP connection.
    /// This must be provided if a <see cref="MachineAccountName"/> is provided.
    /// </summary>
    public string? MachineAccountPassword { get; set; }

    /// <summary>
    /// This option indicates whether nested groups should be ignored when
    /// resolving Roles. The default is false.
    /// </summary>
    public bool IgnoreNestedGroups { get; set; }

    /// <summary>
    /// The <see cref="LdapConnection"/> to be used to retrieve role claims.
    /// If no explicit connection is provided, an LDAP connection will be
    /// automatically created based on the <see cref="Domain"/>,
    /// <see cref="MachineAccountName"/> and <see cref="MachineAccountPassword"/>
    /// options. If provided, this connection will be used and the
    /// <see cref="Domain"/>, <see cref="MachineAccountName"/> and
    /// <see cref="MachineAccountPassword"/>  options will not be used to create
    /// the <see cref="LdapConnection"/>.
    /// </summary>
    public LdapConnection? LdapConnection { get; set; }

    /// <summary>
    /// The sliding expiration that should be used for entries in the cache for user claims, defaults to 10 minutes.
    /// This is a sliding expiration that will extend each time claims for a user is retrieved.
    /// </summary>
    public TimeSpan ClaimsCacheSlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The absolute expiration that should be used for entries in the cache for user claims, defaults to 60 minutes.
    /// This is an absolute expiration that starts when a claims for a user is retrieved for the first time.
    /// </summary>
    public TimeSpan ClaimsCacheAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// The maximum size of the claim results cache, defaults to 100 MB.
    /// </summary>
    public int ClaimsCacheSize { get; set; } = 100 * 1024 * 1024;

    internal MemoryCache? ClaimsCache { get; set; }

    /// <summary>
    /// Validates the <see cref="LdapSettings"/>.
    /// </summary>
    public void Validate()
    {
        if (EnableLdapClaimResolution)
        {
            ArgumentException.ThrowIfNullOrEmpty(Domain);

            if (string.IsNullOrEmpty(MachineAccountName) && !string.IsNullOrEmpty(MachineAccountPassword))
            {
                throw new ArgumentException($"{nameof(MachineAccountPassword)} should only be specified when {nameof(MachineAccountName)} is configured.");
            }
        }
    }
}
