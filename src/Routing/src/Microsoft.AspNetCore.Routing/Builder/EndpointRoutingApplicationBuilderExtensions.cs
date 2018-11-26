// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRoutingApplicationBuilderExtensions
    {
        // Property key is used by MVC package to check that routing is registered
        private const string EndpointRoutingRegisteredKey = "__EndpointRoutingMiddlewareRegistered";

        public static IApplicationBuilder UseEndpointRouting(this IApplicationBuilder builder, Action<IEndpointRouteBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            VerifyRoutingIsRegistered(builder);

            var routeOptions = builder.ApplicationServices.GetRequiredService<IOptions<RouteOptions>>();
            EndpointDataSource middlewareEndpointDataSource;

            var endpointRouteBuilder = builder.ApplicationServices.GetRequiredService<IEndpointRouteBuilder>();
            if (endpointRouteBuilder is DefaultEndpointRouteBuilder defaultEndpointRouteBuilder)
            {
                defaultEndpointRouteBuilder.ApplicationBuilder = builder;
            }
            configure(endpointRouteBuilder);

            foreach (var dataSource in endpointRouteBuilder.DataSources)
            {
                routeOptions.Value.EndpointDataSources.Add(dataSource);
            }

            // Create endpoint data source for data sources registered in configure
            middlewareEndpointDataSource = new CompositeEndpointDataSource(endpointRouteBuilder.DataSources);

            builder.Properties[EndpointRoutingRegisteredKey] = true;

            return builder.UseMiddleware<EndpointRoutingMiddleware>(middlewareEndpointDataSource);
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            VerifyRoutingIsRegistered(builder);

            if (!builder.Properties.TryGetValue(EndpointRoutingRegisteredKey, out _))
            {
                var message = $"{nameof(EndpointRoutingMiddleware)} must be added to the request execution pipeline before {nameof(EndpointMiddleware)}. " +
                    $"Please add {nameof(EndpointRoutingMiddleware)} by calling '{nameof(IApplicationBuilder)}.{nameof(UseEndpointRouting)}' inside the call to 'Configure(...)' in the application startup code.";

                throw new InvalidOperationException(message);
            }

            return builder.UseMiddleware<EndpointMiddleware>();
        }

        private static void VerifyRoutingIsRegistered(IApplicationBuilder app)
        {
            // Verify if AddRouting was done before calling UseEndpointRouting/UseEndpoint
            // We use the RoutingMarkerService to make sure if all the services were added.
            if (app.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    nameof(RoutingServiceCollectionExtensions.AddRouting),
                    "ConfigureServices(...)"));
            }
        }
    }
}