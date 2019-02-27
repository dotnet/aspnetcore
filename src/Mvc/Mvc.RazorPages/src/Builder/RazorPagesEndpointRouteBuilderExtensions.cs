// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains extension methods for using Razor pages with <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class RazorPagesEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds endpoints for Razor pages to the <see cref="IEndpointRouteBuilder"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with Razor pages.</returns>
        public static IEndpointConventionBuilder MapRazorPages(this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureRazorPagesServices(routes);

            return GetOrCreateDataSource(routes);
        }

        private static void EnsureRazorPagesServices(IEndpointRouteBuilder routes)
        {
            var marker = routes.ServiceProvider.GetService<PageActionEndpointDataSource>();
            if (marker == null)
            {
                throw new InvalidOperationException(Mvc.Core.Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddMvc",
                    "ConfigureServices(...)"));
            }
        }

        private static PageActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder routes)
        {
            var dataSource = routes.DataSources.OfType<PageActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = routes.ServiceProvider.GetRequiredService<PageActionEndpointDataSource>();
                routes.DataSources.Add(dataSource);
            }

            return dataSource;
        }
    }
}
