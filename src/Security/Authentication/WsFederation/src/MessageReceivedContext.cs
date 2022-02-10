// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// The context object used for <see cref="WsFederationEvents.MessageReceived"/>.
/// </summary>
public class MessageReceivedContext : RemoteAuthenticationContext<WsFederationOptions>
{
    /// <summary>
    /// Creates a new context object.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="options"></param>
    /// <param name="properties"></param>
    public MessageReceivedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        WsFederationOptions options,
        AuthenticationProperties? properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// The <see cref="WsFederationMessage"/> received on this request.
    /// </summary>
    public WsFederationMessage ProtocolMessage { get; set; } = default!;
}
