// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides failure context information to handler providers.
/// </summary>
public class RemoteFailureContext : HandleRequestContext<RemoteAuthenticationOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RemoteFailureContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The <see cref="AuthenticationScheme"/>.</param>
    /// <param name="options">The <see cref="RemoteAuthenticationOptions"/>.</param>
    /// <param name="failure">User friendly error message for the error.</param>
    public RemoteFailureContext(
        HttpContext context,
        AuthenticationScheme scheme,
        RemoteAuthenticationOptions options,
        Exception failure)
        : base(context, scheme, options)
    {
        Failure = failure;
    }

    /// <summary>
    /// User friendly error message for the error.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// Additional state values for the authentication session.
    /// </summary>
    public AuthenticationProperties? Properties { get; set; }
}
