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
        public static IEndpointConventionBuilder MapControllers(this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureControllerServices(routes);

            return GetOrCreateDataSource(routes);
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
