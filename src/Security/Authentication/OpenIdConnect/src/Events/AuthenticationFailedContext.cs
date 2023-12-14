// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.AuthenticationFailed"/>.
/// </summary>
public class AuthenticationFailedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationFailedContext"/>.
    /// </summary>
    /// <inheritdoc />
    public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options)
        : base(context, scheme, options, new AuthenticationProperties())
    { }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// Gets or sets the exception associated with the failure.
    /// </summary>
    public Exception Exception { get; set; } = default!;
}
