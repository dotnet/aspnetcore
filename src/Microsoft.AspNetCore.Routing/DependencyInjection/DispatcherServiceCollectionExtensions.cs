// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection
{
    // This whole class is temporary until we can remove its usage from MVC
    public static class DispatcherServiceCollectionExtensions
    {
        // This whole class is temporary until we can remove its usage from MVC
        public static IServiceCollection AddDispatcher(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddRouting();
        }

        // This whole class is temporary until we can remove its usage from MVC
        public static IServiceCollection AddDispatcher(this IServiceCollection services, Action<EndpointOptions> configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddRouting();
            if (configuration != null)
            {
                services.Configure<EndpointOptions>(configuration);
            }

            return services;
        }
    }
}
