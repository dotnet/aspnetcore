// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// A context for <see cref="JwtBearerEvents.OnMessageReceived"/>.
/// </summary>
public class MessageReceivedContext : ResultContext<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="MessageReceivedContext"/>.
    /// </summary>
    /// <inheritdoc />
    public MessageReceivedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        JwtBearerOptions options)
        : base(context, scheme, options) { }

    /// <summary>
    /// Bearer Token. This will give the application an opportunity to retrieve a token from an alternative location.
    /// </summary>
    public string? Token { get; set; }
}
