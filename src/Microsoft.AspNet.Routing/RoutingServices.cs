// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection.Extensions;

namespace Microsoft.Framework.DependencyInjection
{
    public static class RoutingServices
    {
        public static IServiceCollection AddRouting(this IServiceCollection services)
        {
            return AddRouting(services, null);
        }

        public static IServiceCollection AddRouting(
            this IServiceCollection services,
            Action<RouteOptions> configureOptions)
        {
            services.AddOptions();
            services.TryAddTransient<IInlineConstraintResolver, DefaultInlineConstraintResolver>();

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}
