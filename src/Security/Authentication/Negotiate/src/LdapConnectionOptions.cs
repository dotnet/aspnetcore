// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.DirectoryServices.Protocols;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Options class for configuring LDAP connections on Linux
    /// </summary>
    public class LdapConnectionOptions
    {
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
        /// If this is not provided, the machine wide credentials of the
        /// domain joined machine will be used.
        /// </summary>
        public string MachineAccountPassword { get; set; }

        /// <summary>
        /// This option indicates whether nested groups should be examined when
        /// resolving AD Roles. 
        /// </summary>
        public bool ResolveNestedGroups { get; set; } = true;

        /// <summary>
        /// Additional configuration on the created LdapConnection.
        /// </summary>
        public Action<LdapConnection> ConfigureLdapConnection { get; set; }
    }
}
