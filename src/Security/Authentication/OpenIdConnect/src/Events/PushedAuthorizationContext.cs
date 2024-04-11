// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.PushAuthorization(PushedAuthorizationContext)"/>.
/// </summary>
public class PushedAuthorizationContext : PropertiesContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PushedAuthorizationContext"/>.
    /// </summary>
    /// <inheritdoc />
    public PushedAuthorizationContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage message, HttpRequestMessage parRequest, AuthenticationProperties properties)
        : base(context, scheme, options, properties)
        => ProtocolMessage = message;

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; }

    /// <summary>
    /// Indicates if the OnPushAuthorization event chose to handle pushing the
    /// authorization request. If true, the handler will not attempt to push the
    /// authorization request.
    /// </summary>
    public bool HandledPush { get; private set; }

    /// <summary>
    /// Tells the handler to skip the process of pushing authorization. The
    /// OnPushAuthorization event may have pushed the request or decided that
    /// pushing the request was not required.
    /// </summary>
    public void HandlePush() => HandledPush = true;

    /// <summary>
    /// Indicates if the OnPushAuthorization event chose to handle client
    /// authentication for the pushed authorization request. If true, the
    /// handler will not attempt to set authentication parameters for the pushed
    /// authorization request.
    /// </summary>
    public bool HandledClientAuthentication { get; private set; }

    /// <summary>
    /// Tells the handler to skip setting client authentication properties for
    /// pushed authorization. The handler uses the client_secret_basic
    /// authentication mode by default, but the OnPushAuthorization event may
    /// replace that with an alternative authentication mode, such as
    /// private_key_jwt.
    /// </summary>
    public void HandleClientAuthentication() => HandledClientAuthentication = true;
}

