// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// The context object used in for <see cref="WsFederationEvents.AuthenticationFailed"/>.
/// </summary>
public class AuthenticationFailedContext : RemoteAuthenticationContext<WsFederationOptions>
{
    /// <summary>
    /// Creates a new context object
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="options"></param>
    public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options)
        : base(context, scheme, options, new AuthenticationProperties())
    { }

    /// <summary>
    /// The <see cref="WsFederationMessage"/> from the request, if any.
    /// </summary>
    public WsFederationMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// The <see cref="Exception"/> that triggered this event.
    /// </summary>
    public Exception Exception { get; set; } = default!;
}
