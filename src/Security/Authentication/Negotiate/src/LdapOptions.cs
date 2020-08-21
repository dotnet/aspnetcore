// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.DirectoryServices.Protocols;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Options class for configuring LDAP connections on Linux
    /// </summary>
    public class LdapOptions
    {
        /// <summary>
        /// Configure whether LDAP connection should be used to resolve role claims.
        /// This is mainly used on Linux.
        /// </summary>
        public bool EnableLdapRoleClaimResolution { get; set; }

        /// <summary>
        /// The domain to use for the LDAP connection. This is a mandatory setting.
        /// </summary>
        /// <example>
        /// DOMAIN.com
        /// </example>
        public string Domain { get; set; }

        /// <summary>
        /// The machine account name to use when opening the LDAP connection.
        /// If this is not provided, the machine wide credentials of the
        /// domain joined machine will be used.
        /// </summary>
        public string MachineAccountName { get; set; }

        /// <summary>
        /// The machine account password to use when opening the LDAP connection.
        /// This must be provided if a <see cref="MachineAccountName"/> is provided.
        /// </summary>
        public string MachineAccountPassword { get; set; }

        /// <summary>
        /// This option indicates whether nested groups should be examined when
        /// resolving AD Roles. 
        /// </summary>
        public bool ResolveNestedGroups { get; set; } = true;

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
        public LdapConnection LdapConnection { get; set; }

        public void Validate()
        {
            if (EnableLdapRoleClaimResolution)
            {
                if (string.IsNullOrEmpty(Domain))
                {
                    throw new ArgumentException($"{nameof(EnableLdapRoleClaimResolution)} is set to true but {nameof(Domain)} is not set.");
                }

                if (string.IsNullOrEmpty(MachineAccountName) && !string.IsNullOrEmpty(MachineAccountPassword))
                {
                    throw new ArgumentException($"{nameof(MachineAccountPassword)} should only be specified when {nameof(MachineAccountName)} is configured.");
                }
            }
        }
    }
}
