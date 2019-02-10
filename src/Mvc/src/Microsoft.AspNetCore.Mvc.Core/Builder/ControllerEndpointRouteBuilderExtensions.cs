// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains extension methods for using Controllers with <see cref="IEndpointRouteBuilder"/>
    /// </summary>
    public static class ControllerEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds controller actions to the route builder.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with controller actions.</returns>
        public static IEndpointConventionBuilder MapControllers(this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureControllerServices(routes);

            return GetOrCreateDataSource(routes);
        }

        /// <summary>
        /// Adds controller actions to the route builder and adds the default conventional route
        /// <c>{controller=Home}/{action=Index}/{id?}</c>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with controller actions.</returns>
        public static IEndpointConventionBuilder MapControllersWithDefaultRoute(this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureControllerServices(routes);

            var dataSource = GetOrCreateDataSource(routes);
            dataSource.AddRoute(new ConventionalRouteEntry(
                "default",
                "{controller=Home}/{action=Index}/{id?}",
                defaults: null,
                constraints: null,
                dataTokens: null));

            return dataSource;
        }

        /// <summary>
        /// Adds a conventional route that can be used to route to conventionally-route controller actions.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="name">The route name.</param>
        /// <param name="template">The route template.</param>
        /// <param name="defaults">The route default values.</param>
        /// <param name="constraints">The route constraints.</param>
        /// <param name="dataTokens">The route data tokens.</param>
        public static void MapControllerRoute(
            this IEndpointRouteBuilder routes,
            string name,
            string template,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            EnsureControllerServices(routes);

            var dataSource = GetOrCreateDataSource(routes);
            dataSource.AddRoute(new ConventionalRouteEntry(
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens)));
        }

        private static void EnsureControllerServices(IEndpointRouteBuilder routes)
        {
            var marker = routes.ServiceProvider.GetService<MvcMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddMvc",
                    "ConfigureServices(...)"));
            }
        }

        private static ControllerActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder routes)
        {
            var dataSource = routes.DataSources.OfType<ControllerActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = routes.ServiceProvider.GetRequiredService<ControllerActionEndpointDataSource>();
                routes.DataSources.Add(dataSource);
            }

            return dataSource;
        }
    }
}
