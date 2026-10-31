// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// A cookie builder that sets <see cref="CookieOptions.Path"/> to the request path base.
/// </summary>
public class RequestPathBaseCookieBuilder : CookieBuilder
{
    /// <summary>
    /// Gets an optional value that is appended to the request path base.
    /// </summary>
    protected virtual string? AdditionalPath { get; }

    /// <summary>
    /// Configures <see cref="CookieOptions.Path"/> if not explicitly configured.
    /// </summary>
    /// <inheritdoc />
    public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
    {
        // check if the user has overridden the default value of path. If so, use that instead of our default value.
        var path = Path;
        if (path == null)
        {
            var originalPathBase = context.Features.Get<IAuthenticationFeature>()?.OriginalPathBase ?? context.Request.PathBase;
            path = originalPathBase + AdditionalPath;
        }

        var options = base.Build(context, expiresFrom);

        options.Path = !string.IsNullOrEmpty(path)
            ? path
            : "/";

        return options;
    }
}
