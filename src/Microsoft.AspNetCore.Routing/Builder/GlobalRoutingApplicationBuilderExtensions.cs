// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class GlobalRoutingApplicationBuilderExtensions
    {
        private const string GlobalRoutingRegisteredKey = "__GlobalRoutingMiddlewareRegistered";

        public static IApplicationBuilder UseGlobalRouting(this IApplicationBuilder builder)
        {
            VerifyRoutingIsRegistered(builder);

            builder.Properties[GlobalRoutingRegisteredKey] = true;

            return builder.UseMiddleware<GlobalRoutingMiddleware>();
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            VerifyRoutingIsRegistered(builder);

            if (!builder.Properties.TryGetValue(GlobalRoutingRegisteredKey, out _))
            {
                var message = $"{nameof(GlobalRoutingMiddleware)} must be added to the request execution pipeline before {nameof(EndpointMiddleware)}. " +
                    $"Please add {nameof(GlobalRoutingMiddleware)} by calling '{nameof(IApplicationBuilder)}.{nameof(UseGlobalRouting)}' inside the call to 'Configure(...)' in the application startup code.";

                throw new InvalidOperationException(message);
            }

            return builder.UseMiddleware<EndpointMiddleware>();
        }

        private static void VerifyRoutingIsRegistered(IApplicationBuilder app)
        {
            // Verify if AddRouting was done before calling UseGlobalRouting/UseEndpoint
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
