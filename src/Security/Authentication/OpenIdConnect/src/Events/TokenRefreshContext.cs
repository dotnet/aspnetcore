// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect.Events;

/// <summary>
/// Represents a context for the TokenRefresh and TokenRefreshing events.
/// </summary>
public class TokenRefreshContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Gets or sets a value indicating whether the token should be refreshed by the OpenIdConnectHandler or not.
    /// </summary>
    /// <remarks>
    /// The default value of this property is `true`, which indicates,
    /// that the OpenIdConnectHandler should be responsible for refreshing the token.
    /// However, custom handler can be registered for the <see cref="OpenIdConnectEvents.OnTokenRefreshing"/> event,
    /// which may take the responsibility for updating the token. In that case,
    /// the handler should set the <see cref="ShouldRefresh"/> to `false` to indicate that the token has already
    /// been refreshed and the <see cref="OpenIdConnectHandler"/> shouldn't try to refresh it.
    /// </remarks>
    public bool ShouldRefresh { get; set; } = true;

    /// <summary>
    /// Creates a <see cref="TokenValidatedContext"/>
    /// </summary>
    /// <inheritdoc />
    public TokenRefreshContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
        : base(context, scheme, options, properties)
        => Principal = principal;

    /// <summary>
    /// Called to replace the claims principal. The supplied principal will replace the value of the
    /// Principal property, which determines the identity of the authenticated request.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> used as the replacement</param>
    public void ReplacePrincipal(ClaimsPrincipal principal) => Principal = principal;
}
