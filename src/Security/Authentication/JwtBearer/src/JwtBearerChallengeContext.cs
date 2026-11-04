// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// A <see cref="PropertiesContext{TOptions}"/> when access to a resource authenticated using JWT bearer is challenged.
/// </summary>
public class JwtBearerChallengeContext : PropertiesContext<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="JwtBearerChallengeContext"/>.
    /// </summary>
    /// <inheritdoc />
    public JwtBearerChallengeContext(
        HttpContext context,
        AuthenticationScheme scheme,
        JwtBearerOptions options,
        AuthenticationProperties properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// Any failures encountered during the authentication process.
    /// </summary>
    public Exception? AuthenticateFailure { get; set; }

    /// <summary>
    /// Gets or sets the "error" value returned to the caller as part
    /// of the WWW-Authenticate header. This property may be null when
    /// <see cref="JwtBearerOptions.IncludeErrorDetails"/> is set to <c>false</c>.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the "error_description" value returned to the caller as part
    /// of the WWW-Authenticate header. This property may be null when
    /// <see cref="JwtBearerOptions.IncludeErrorDetails"/> is set to <c>false</c>.
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Gets or sets the "error_uri" value returned to the caller as part of the
    /// WWW-Authenticate header. This property is always null unless explicitly set.
    /// </summary>
    public string? ErrorUri { get; set; }

    /// <summary>
    /// If true, will skip any default logic for this challenge.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Skips any default logic for this challenge.
    /// </summary>
    public void HandleResponse() => Handled = true;
}
