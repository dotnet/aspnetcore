// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
/// </summary>
public class TokenResponseReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Creates a <see cref="TokenResponseReceivedContext"/>
    /// </summary>
    public TokenResponseReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal user, AuthenticationProperties properties)
        : base(context, scheme, options, properties)
        => Principal = user;

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/> that contains the tokens received after redeeming the code at the token endpoint.
    /// </summary>
    public OpenIdConnectMessage TokenEndpointResponse { get; set; } = default!;
}
