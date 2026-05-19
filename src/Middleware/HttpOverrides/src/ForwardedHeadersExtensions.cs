// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for enabling <see cref="ForwardedHeadersMiddleware"/>.
/// </summary>
public static class ForwardedHeadersExtensions
{
    private const string ForwardedHeadersAdded = "ForwardedHeadersAdded";

    /// <summary>
    /// Applies forwarded headers to their matching fields on the current request.
    /// <para>
    /// By convention, HTTP proxies forward information from the client in well-known HTTP headers.
    /// The <see cref="ForwardedHeadersMiddleware"/> reads these headers and fills in the associated fields on HttpContext.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder" />.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Don't add more than one instance of this middleware to the pipeline using the options from the DI container.
        // Doing so could cause a request to be processed multiple times and the ForwardLimit to be exceeded.
        if (!builder.Properties.ContainsKey(ForwardedHeadersAdded))
        {
            builder.Properties[ForwardedHeadersAdded] = true;
            return builder.UseMiddleware<ForwardedHeadersMiddleware>();
        }

        return builder;
    }

    /// <summary>
    /// Applies forwarded headers to their matching fields on the current request.
    /// <para>
    /// By convention, HTTP proxies forward information from the client in well-known HTTP headers.
    /// The <see cref="ForwardedHeadersMiddleware"/> reads these headers and fills in the associated fields on HttpContext.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder" />.</param>
    /// <param name="options">Enables the different forwarding options.</param>
    /// <returns>A reference to <paramref name="builder" /> after the operation has completed.</returns>
    public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder, ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.UseMiddleware<ForwardedHeadersMiddleware>(Options.Create(options));
    }
}
