// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class AttributeRouting
    {
        /// <summary>
        /// Creates an attribute route using the provided services and provided target router.
        /// </summary>
        /// <param name="target">The router to invoke when a route entry matches.</param>
        /// <param name="services">The application services.</param>
        /// <returns>An attribute route.</returns>
        public static IRouter CreateAttributeMegaRoute(IRouter target, IServiceProvider services)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return new AttributeRoute(
                target,
                services.GetRequiredService<IActionDescriptorCollectionProvider>(),
                services);
        }
    }
}
