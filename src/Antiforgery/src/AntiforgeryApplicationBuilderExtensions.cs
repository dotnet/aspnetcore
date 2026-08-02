// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Http;

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
    /// <remarks>
    /// <para>
    /// The middleware validates anti-forgery tokens only for HTTP POST, PUT, and PATCH requests. Requests
    /// using other HTTP methods are skipped.
    /// </para>
    /// <para>
    /// If you need validation for other HTTP methods (for example, DELETE), resolve
    /// <see cref="IAntiforgery"/> and call <see cref="IAntiforgery.ValidateRequestAsync(HttpContext)"/> or
    /// <see cref="IAntiforgery.IsRequestValidAsync(HttpContext)"/> in your handler.
    /// </para>
    /// <para>
    /// When using HTTP method override middleware configured to read the method from a form field before this middleware,
    /// an incoming POST request can be overridden to another method. Anti-forgery validation still runs when the effective
    /// method is POST, PUT, or PATCH, but does not run automatically when the override changes the request to a different
    /// method outside that set (for example, DELETE).
    /// </para>
    /// </remarks>
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
