// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains extension methods for using Controllers with <see cref="IEndpointRouteBuilder"/>
    /// </summary>
    public static class ControllerEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> without specifying any routes.
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
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and adds the default route
        /// <c>{controller=Home}/{action=Index}/{id?}</c>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with controller actions.</returns>
        public static IEndpointConventionBuilder MapDefaultControllerRoute(this IEndpointRouteBuilder routes)
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
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="template"/>, 
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        public static void MapControllerRoute(
            this IEndpointRouteBuilder routes,
            string name,
            string template,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureControllerServices(routes);

            var dataSource = GetOrCreateDataSource(routes);
            dataSource.AddRoute(new ConventionalRouteEntry(
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens)));
        }

        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="areaName"/>, <paramref name="template"/>, 
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="areaName">The area name.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        public static void MapAreaControllerRoute(
            this IEndpointRouteBuilder routes,
            string name,
            string areaName,
            string template,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (string.IsNullOrEmpty(areaName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(areaName));
            }

            var defaultsDictionary = new RouteValueDictionary(defaults);
            defaultsDictionary["area"] = defaultsDictionary["area"] ?? areaName;

            var constraintsDictionary = new RouteValueDictionary(constraints);
            constraintsDictionary["area"] = constraintsDictionary["area"] ?? new StringRouteConstraint(areaName);

            routes.MapControllerRoute(name, template, defaultsDictionary, constraintsDictionary, dataTokens);
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
