// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// A context for <see cref="OpenIdConnectEvents.PushAuthorization(PushedAuthorizationContext)"/>.
/// </summary>
public sealed class PushedAuthorizationFailedContext : PropertiesContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PushedAuthorizationFailedContext"/>.
    /// </summary>
    /// <inheritdoc />
    public PushedAuthorizationFailedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options,
        AuthenticationProperties properties, Exception exception)
        : base(context, scheme, options, properties)
    {
        Exception = exception;
    }

    /// <summary>
    /// Gets or sets the exception associated with the failure.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Tells the handler that the OnPushAuthorizationFailed event has handled the process of the
    /// error and the handler does not need to throw an exception.
    /// </summary>
    public bool Handled { get; set; }
}

