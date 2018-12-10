// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class RazorPagesEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapPages(this IEndpointRouteBuilder routeBuilder, string pageName)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            if (pageName == null)
            {
                throw new ArgumentNullException(nameof(pageName));
            }

            var dataSource = GetOrCreateDataSource(routeBuilder);
            return dataSource.AddType(controllerType);
        }

        private static PageEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder routeBuilder)
        {
            var factory = routeBuilder.ServiceProvider
                .GetRequiredService<IEnumerable<ApplicationDataSourceFactory>>()
                .OfType<PageEndpointDataSource>()
                .FirstOrDefault();
            if (factory == null)
            {
                throw new InvalidOperationException("This method cannot be used without calling 'AddMvc()' or on the service collection.");
            }

            return factory.GetOrCreateDataSource(routeBuilder);
        }
    }
}
