// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

        // The anti-forgery middleware adds annotations to HttpContext.Items to indicate that it has run
        // that will be validated by the EndpointsRoutingMiddleware later. To do this, we need to ensure
        // that routing has run and set the endpoint feature on the HttpContext associated with the request.
        if (builder.Properties.TryGetValue(RerouteHelper.GlobalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return builder.Use(next =>
            {
                var newNext = RerouteHelper.Reroute(builder, routeBuilder, next);
                var antiforgery = builder.ApplicationServices.GetRequiredService<IAntiforgery>();
                return new AntiforgeryMiddleware(antiforgery, newNext).Invoke;
            });
        }
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
