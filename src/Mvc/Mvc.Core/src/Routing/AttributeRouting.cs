// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal static class AttributeRouting
{
    /// <summary>
    /// Creates an attribute route using the provided services and provided target router.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <returns>An attribute route.</returns>
    public static IRouter CreateAttributeMegaRoute(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new AttributeRoute(
            services.GetRequiredService<IActionDescriptorCollectionProvider>(),
            services,
            actions =>
            {
                var handler = services.GetRequiredService<MvcAttributeRouteHandler>();
                handler.Actions = actions;
                return handler;
            });
    }
}
