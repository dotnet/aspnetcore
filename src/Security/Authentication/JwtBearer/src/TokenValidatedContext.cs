// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// A context for <see cref="JwtBearerEvents.OnTokenValidated"/>.
/// </summary>
public class TokenValidatedContext : ResultContext<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="TokenValidatedContext"/>.
    /// </summary>
    /// <inheritdoc />
    public TokenValidatedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        JwtBearerOptions options)
        : base(context, scheme, options) { }

    /// <summary>
    /// Gets or sets the validated security token.
    /// </summary>
    public SecurityToken SecurityToken { get; set; } = default!;
}
