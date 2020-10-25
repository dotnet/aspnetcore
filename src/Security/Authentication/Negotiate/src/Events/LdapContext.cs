// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// State for the RetrieveLdapClaims event.
    /// </summary>
    public class LdapContext : ResultContext<NegotiateOptions>
    {
        /// <summary>
        /// Creates a new <see cref="LdapContext"/>.
        /// </summary>
        /// <inheritdoc />
        public LdapContext(
            HttpContext context,
            AuthenticationScheme scheme,
            NegotiateOptions options,
            LdapSettings settings)
            : base(context, scheme, options)
        {
            LdapSettings = settings;
        }

        /// <summary>
        /// The LDAP settings to use for the RetrieveLdapClaims event.
        /// </summary>
        public LdapSettings LdapSettings { get; }
    }
}
