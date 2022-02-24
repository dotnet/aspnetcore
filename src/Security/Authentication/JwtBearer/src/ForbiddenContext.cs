// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// A <see cref="ResultContext{TOptions}"/> when access to a resource is forbidden.
/// </summary>
public class ForbiddenContext : ResultContext<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForbiddenContext"/>.
    /// </summary>
    /// <inheritdoc />
    public ForbiddenContext(
        HttpContext context,
        AuthenticationScheme scheme,
        JwtBearerOptions options)
        : base(context, scheme, options) { }
}
