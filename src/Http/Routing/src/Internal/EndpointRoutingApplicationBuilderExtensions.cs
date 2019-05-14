// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Internal
{
    public static class EndpointRoutingApplicationBuilderExtensions
    {
        // Property key is used by MVC package to check that routing is registered
        private const string EndpointRoutingRegisteredKey = "__EndpointRoutingMiddlewareRegistered";

        public static IApplicationBuilder UseEndpointRouting(this IApplicationBuilder builder)
        {
            VerifyRoutingIsRegistered(builder);

            builder.Properties[EndpointRoutingRegisteredKey] = true;

            return builder.UseMiddleware<EndpointRoutingMiddleware>();
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
