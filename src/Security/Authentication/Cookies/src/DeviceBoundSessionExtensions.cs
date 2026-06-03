// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Device Bound Session Credentials (DBSC).
/// </summary>
public static class DeviceBoundSessionExtensions
{
    /// <summary>
    /// Adds the Device Bound Session Credentials middleware to the application pipeline.
    /// This middleware handles DBSC registration and refresh requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware should be added after <c>UseAuthentication()</c> in the pipeline
    /// so that the authentication cookie is available for DBSC operations.
    /// </para>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseAuthentication();
    /// app.UseDeviceBoundSessions();
    /// app.UseAuthorization();
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseDeviceBoundSessions(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DeviceBoundSessionMiddleware>();
    }
}
