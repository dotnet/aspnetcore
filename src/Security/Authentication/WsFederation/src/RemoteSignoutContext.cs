// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// An event context for RemoteSignOut.
/// </summary>
public class RemoteSignOutContext : RemoteAuthenticationContext<WsFederationOptions>
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="options"></param>
    /// <param name="message"></param>
    public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, WsFederationMessage message)
        : base(context, scheme, options, new AuthenticationProperties())
        => ProtocolMessage = message;

    /// <summary>
    /// The signout message.
    /// </summary>
    public WsFederationMessage ProtocolMessage { get; set; }
}
