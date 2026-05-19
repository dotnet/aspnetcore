// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices.AngularCli;

/// <summary>
/// Extension methods for enabling Angular CLI middleware support.
/// </summary>
public static class AngularCliMiddlewareExtensions
{
    /// <summary>
    /// Handles requests by passing them through to an instance of the Angular CLI server.
    /// This means you can always serve up-to-date CLI-built resources without having
    /// to run the Angular CLI server manually.
    ///
    /// This feature should only be used in development. For production deployments, be
    /// sure not to enable the Angular CLI server.
    /// </summary>
    /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
    /// <param name="npmScript">The name of the script in your package.json file that launches the Angular CLI process.</param>
    public static void UseAngularCliServer(
        this ISpaBuilder spaBuilder,
        string npmScript)
    {
        ArgumentNullException.ThrowIfNull(spaBuilder);

        var spaOptions = spaBuilder.Options;

        if (string.IsNullOrEmpty(spaOptions.SourcePath))
        {
            throw new InvalidOperationException($"To use {nameof(UseAngularCliServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
        }

        AngularCliMiddleware.Attach(spaBuilder, npmScript);
    }
}
