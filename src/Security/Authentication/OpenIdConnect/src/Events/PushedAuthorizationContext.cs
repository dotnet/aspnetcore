// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.PushAuthorization(PushedAuthorizationContext)"/>.
/// </summary>
public sealed class PushedAuthorizationContext : PropertiesContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PushedAuthorizationContext"/>.
    /// </summary>
    /// <inheritdoc />
    public PushedAuthorizationContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage parRequest, AuthenticationProperties properties)
        : base(context, scheme, options, properties)
    {
        ProtocolMessage = parRequest;
    }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/> that will be sent to the PAR endpoint.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; }

    /// <summary>
    /// Indicates if the OnPushAuthorization event chose to handle pushing the
    /// authorization request. If true, the handler will not attempt to push the
    /// authorization request, and will instead use the RequestUri from this
    /// event in the subsequent authorize request.
    /// </summary>
    public bool HandledPush { [MemberNotNull("RequestUri")] get; private set; }

    /// <summary>
    /// Tells the handler that the OnPushAuthorization event has handled the process of pushing
    /// authorization, and that the handler should use the provided request_uri
    /// on the subsequent authorize call.
    /// </summary>
    public void HandlePush(string requestUri)
    {
        if (SkippedPush || HandledClientAuthentication)
        {
            throw new InvalidOperationException("Only one of HandlePush, SkipPush, and HandledClientAuthentication may be called in the OnPushAuthorization event.");
        }
        HandledPush = true;
        RequestUri = requestUri;
    }

    /// <summary>
    /// Indicates if the OnPushAuthorization event chose to skip pushing the
    /// authorization request. If true, the handler will not attempt to push the
    /// authorization request, and will not use pushed authorization in the
    /// subsequent authorize request.
    /// </summary>
    public bool SkippedPush { get; private set; }

    /// <summary>
    /// The request_uri parameter to use in the subsequent authorize call, if
    /// the OnPushAuthorization event chose to handle pushing the authorization
    /// request, and null otherwise.
    /// </summary>
    public string? RequestUri { get; private set; }

    /// <summary>
    /// Tells the handler to skip pushing authorization entirely. If this is
    /// called, the handler will not use pushed authorization on the subsequent
    /// authorize call.
    /// </summary>
    public void SkipPush()
    {
        if (HandledPush || HandledClientAuthentication)
        {
            throw new InvalidOperationException("Only one of HandlePush, SkipPush, and HandledClientAuthentication may be called in the OnPushAuthorization event.");
        }
        SkippedPush = true;
    }

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
    public void HandleClientAuthentication()
    {
        if (SkippedPush || HandledPush)
        {
            throw new InvalidOperationException("Only one of HandlePush, SkipPush, and HandledClientAuthentication may be called in the OnPushAuthorization event.");
        }
        HandledClientAuthentication = true;
    }
}

