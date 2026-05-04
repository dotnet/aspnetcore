// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.TokenValidated(TokenValidatedContext)"/>.
/// </summary>
public class TokenValidatedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Creates a <see cref="TokenValidatedContext"/>.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="options">The OpenID Connect authentication options.</param>
    /// <param name="principal">The authenticated user principal.</param>
    /// <param name="properties">The authentication properties for the request.</param>
    public TokenValidatedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
        : base(context, scheme, options, properties)
        => Principal = principal;

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// Gets or sets the validated security token.
    /// </summary>
    public JwtSecurityToken SecurityToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the token endpoint response.
    /// </summary>
    public OpenIdConnectMessage? TokenEndpointResponse { get; set; }

    /// <summary>
    /// Gets or sets the protocol nonce.
    /// </summary>
    public string? Nonce { get; set; }
}
