// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcServiceCollectionExtensions
    {
        public static IServiceCollection AddMvc([NotNull] this IServiceCollection services)
        {
            return AddMvc(services, configuration: null);
        }

        public static IServiceCollection AddMvc(
            [NotNull] this IServiceCollection services,
            IConfiguration configuration)
        {
            ConfigureDefaultServices(services, configuration);
            services.TryAdd(MvcServices.GetDefaultServices(configuration));
            return services;
        }

        /// <summary>
        /// Configures a set of <see cref="MvcOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">The <see cref="MvcOptions"/> which need to be configured.</param>
        public static void ConfigureMvcOptions(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<MvcOptions> setupAction)
        {
            services.Configure(setupAction);
        }

        private static void ConfigureDefaultServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions(configuration);
            services.AddDataProtection(configuration);
            services.AddRouting(configuration);
            services.AddScopedInstance(configuration);
            services.AddAuthorization(configuration);
            services.Configure<RouteOptions>(routeOptions =>
                                                    routeOptions.ConstraintMap
                                                         .Add("exists",
                                                              typeof(KnownRouteValueConstraint)));

        }
    }
}
