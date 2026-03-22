// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Antiforgery.Internal;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Anti-forgery extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class AntiforgeryApplicationBuilderExtensions
{
    private const string AntiforgeryMiddlewareSetKey = "__AntiforgeryMiddlewareSet";

    /// <summary>
    /// Adds the anti-forgery middleware to the pipeline.
    /// Uses cross-origin validation (Sec-Fetch-Site / Origin) first, then falls back
    /// to token-based validation when cross-origin checks are inconclusive.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The app builder.</returns>
    public static IApplicationBuilder UseAntiforgery(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.VerifyAntiforgeryServicesAreRegistered();

        builder.Properties[AntiforgeryMiddlewareSetKey] = true;
        builder.UseMiddleware<TokenBasedAntiforgeryMiddleware>();

        return builder;
    }

    /// <summary>
    /// Adds token-based-only anti-forgery middleware to the pipeline.
    /// Cross-origin header checks are skipped. This is the legacy behavior prior to .NET 11.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The app builder.</returns>
    public static IApplicationBuilder UseTokenBasedAntiforgery(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.VerifyAntiforgeryServicesAreRegistered();

        builder.Properties[AntiforgeryMiddlewareSetKey] = true;
        builder.UseMiddleware<TokenBasedAntiforgeryMiddleware>();

        return builder;
    }

    /// <summary>
    /// Adds cross-origin-only anti-forgery middleware to the pipeline.
    /// Uses Sec-Fetch-Site and Origin headers. Requests where cross-origin validation
    /// is inconclusive (e.g., missing headers) are denied.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The app builder.</returns>
    public static IApplicationBuilder UseCrossOriginAntiforgery(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.VerifyCrossOriginAntiforgeryServicesAreRegistered();

        builder.Properties[AntiforgeryMiddlewareSetKey] = true;
        builder.UseMiddleware<CrossOriginAntiforgeryMiddleware>();

        return builder;
    }

    private static void VerifyAntiforgeryServicesAreRegistered(this IApplicationBuilder builder)
    {
        if (builder.ApplicationServices.GetService(typeof(IAntiforgery)) == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddAntiforgery' in the application startup code.");
        }
    }

    private static void VerifyCrossOriginAntiforgeryServicesAreRegistered(this IApplicationBuilder builder)
    {
        if (builder.ApplicationServices.GetService(typeof(ICrossOriginAntiforgery)) == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddCrossOriginAntiforgery' or 'IServiceCollection.AddAntiforgery' in the application startup code.");
        }
    }
}
