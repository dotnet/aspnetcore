// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcServiceCollectionExtensions
    {
        public static IServiceCollection AddMvc(this IServiceCollection services)
        {
            services.Add(RoutingServices.GetDefaultServices());
            AddMvcRouteOptions(services);
            return services.Add(MvcServices.GetDefaultServices());
        }

        public static IServiceCollection AddMvc(this IServiceCollection services, IConfiguration configuration)
        {
            services.Add(RoutingServices.GetDefaultServices());
            AddMvcRouteOptions(services);
            return services.Add(MvcServices.GetDefaultServices(configuration));
        }

        private static void AddMvcRouteOptions(IServiceCollection services)
        {
            services.Configure<RouteOptions>(routeOptions =>
                                                    routeOptions.ConstraintMap
                                                         .Add("exists",
                                                              typeof(KnownRouteValueConstraint)));
        }
    }
}
