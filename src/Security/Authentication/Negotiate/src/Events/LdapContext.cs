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
    /// <param name="context">The HTTP request context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="options">The negotiate authentication options.</param>
    /// <param name="settings">The LDAP settings to apply.</param>
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
