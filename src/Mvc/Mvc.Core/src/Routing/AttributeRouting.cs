// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal static class AttributeRouting
    {
        /// <summary>
        /// Creates an attribute route using the provided services and provided target router.
        /// </summary>
        /// <param name="services">The application services.</param>
        /// <returns>An attribute route.</returns>
        public static IRouter CreateAttributeMegaRoute(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

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
}
