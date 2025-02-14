// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
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
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The app builder.</returns>
    public static IApplicationBuilder UseAntiforgery(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.VerifyAntiforgeryServicesAreRegistered();

        builder.Properties[AntiforgeryMiddlewareSetKey] = true;
        builder.UseMiddleware<AntiforgeryMiddleware>();

        return builder;
    }

    private static void VerifyAntiforgeryServicesAreRegistered(this IApplicationBuilder builder)
    {
        if (builder.ApplicationServices.GetService(typeof(IAntiforgery)) == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddAntiforgery' in the application startup code.");
        }
    }
}
