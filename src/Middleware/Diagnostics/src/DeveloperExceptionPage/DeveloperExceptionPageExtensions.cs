// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extension methods for the <see cref="DeveloperExceptionPageMiddleware"/>.
/// </summary>
public static class DeveloperExceptionPageExtensions
{
    /// <summary>
    /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
    /// <remarks>
    /// This should only be enabled in the Development environment.
    /// </remarks>
    public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Properties["analysis.NextMiddlewareName"] = "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware";
        return app.UseMiddleware<DeveloperExceptionPageMiddlewareImpl>();
    }

    /// <summary>
    /// Captures synchronous and asynchronous <see cref="Exception"/> instances from the pipeline and generates HTML error responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="options">A <see cref="DeveloperExceptionPageOptions"/> that specifies options for the middleware.</param>
    /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
    /// <remarks>
    /// This should only be enabled in the Development environment.
    /// </remarks>
    public static IApplicationBuilder UseDeveloperExceptionPage(
        this IApplicationBuilder app,
        DeveloperExceptionPageOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        app.Properties["analysis.NextMiddlewareName"] = "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware";
        return app.UseMiddleware<DeveloperExceptionPageMiddlewareImpl>(Options.Create(options));
    }
}
