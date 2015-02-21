// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Routing
{
    public static class AttributeRouting
    {
        // Key used by routing and action selection to match an attribute route entry to a
        // group of action descriptors.
        public static readonly string RouteGroupKey = "!__route_group";

        /// <summary>
        /// Creates an attribute route using the provided services and provided target router.
        /// </summary>
        /// <param name="target">The router to invoke when a route entry matches.</param>
        /// <param name="services">The application services.</param>
        /// <returns>An attribute route.</returns>
        public static IRouter CreateAttributeMegaRoute([NotNull] IRouter target, [NotNull] IServiceProvider services)
        {
            var actionDescriptorProvider = services.GetRequiredService<IActionDescriptorsCollectionProvider>();
            var inlineConstraintResolver = services.GetRequiredService<IInlineConstraintResolver>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            return new AttributeRoute(target, actionDescriptorProvider, inlineConstraintResolver, loggerFactory);
        }
    }
}
