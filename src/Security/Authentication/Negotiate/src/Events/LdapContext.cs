// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

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
