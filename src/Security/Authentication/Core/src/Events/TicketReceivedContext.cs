// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides context information to handler providers.
/// </summary>
public class TicketReceivedContext : RemoteAuthenticationContext<RemoteAuthenticationOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="TicketReceivedContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The <see cref="AuthenticationScheme"/>.</param>
    /// <param name="options">The <see cref="RemoteAuthenticationOptions"/>.</param>
    /// <param name="ticket">The received ticket.</param>
    public TicketReceivedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        RemoteAuthenticationOptions options,
        AuthenticationTicket ticket)
        : base(context, scheme, options, ticket?.Properties)
        => Principal = ticket?.Principal;

    /// <summary>
    /// Gets or sets the URL to redirect to after signin.
    /// </summary>
    public string? ReturnUri { get; set; }
}
