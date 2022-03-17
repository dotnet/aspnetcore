// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// A <see cref="ResultContext{TOptions}"/> when authentication has failed.
/// </summary>
public class AuthenticationFailedContext : ResultContext<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationFailedContext"/>.
    /// </summary>
    /// <inheritdoc />
    public AuthenticationFailedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        JwtBearerOptions options)
        : base(context, scheme, options) { }

    /// <summary>
    /// Gets or sets the exception associated with the authentication failure.
    /// </summary>
    public Exception Exception { get; set; } = default!;
}
