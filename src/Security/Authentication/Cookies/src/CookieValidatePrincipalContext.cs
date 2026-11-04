// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Context object passed to the CookieAuthenticationEvents ValidatePrincipal method.
/// </summary>
public class CookieValidatePrincipalContext : PrincipalContext<CookieAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of the context object.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="ticket">Contains the initial values for identity and extra data</param>
    /// <param name="options"></param>
    public CookieValidatePrincipalContext(HttpContext context, AuthenticationScheme scheme, CookieAuthenticationOptions options, AuthenticationTicket ticket)
        : base(context, scheme, options, ticket?.Properties)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        Principal = ticket.Principal;
    }

    /// <summary>
    /// If true, the cookie will be renewed
    /// </summary>
    public bool ShouldRenew { get; set; }

    /// <summary>
    /// Called to replace the claims principal. The supplied principal will replace the value of the
    /// Principal property, which determines the identity of the authenticated request.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> used as the replacement</param>
    public void ReplacePrincipal(ClaimsPrincipal principal) => Principal = principal;

    /// <summary>
    /// Called to reject the incoming principal. This may be done if the application has determined the
    /// account is no longer active, and the request should be treated as if it was anonymous.
    /// </summary>
    public void RejectPrincipal() => Principal = null;
}
