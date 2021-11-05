// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.RemoteSignOut(RemoteSignOutContext)"/> event.
/// </summary>
public class RemoteSignOutContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RemoteSignOutContext"/>.
    /// </summary>
    /// <inheritdoc />
    public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage? message)
        : base(context, scheme, options, new AuthenticationProperties())
        => ProtocolMessage = message;

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage? ProtocolMessage { get; set; }
}
