// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// The <see cref="IApplicationBuilder"/> extensions for adding Antiforgery middleware support.
/// </summary>
public static class AntiforgeryMiddlewareExtensions
{
    /// <summary>
    /// Adds the Antiforgery middleware to the middleware pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
    public static IApplicationBuilder UseAntiforgery(this IApplicationBuilder app)
        => app.UseMiddleware<AntiforgeryMiddleware>();
}
