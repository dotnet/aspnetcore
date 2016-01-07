// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNet.Mvc.Routing
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

            var actionDescriptorProvider = services.GetRequiredService<IActionDescriptorCollectionProvider>();
            var inlineConstraintResolver = services.GetRequiredService<IInlineConstraintResolver>();
            var pool = services.GetRequiredService<ObjectPool<UriBuildingContext>>();
            var urlEncoder = services.GetRequiredService<UrlEncoder>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            return new AttributeRoute(
                target,
                actionDescriptorProvider,
                inlineConstraintResolver,
                pool,
                urlEncoder,
                loggerFactory);
        }
    }
}
