// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DispatcherApplicationBuilderExtensions
    {
        private const string DispatcherRegisteredKey = "__DispatcherMiddlewareRegistered";

        public static IApplicationBuilder UseDispatcher(this IApplicationBuilder builder)
        {
            VerifyDispatcherIsRegistered(builder);

            builder.Properties[DispatcherRegisteredKey] = true;

            return builder.UseMiddleware<DispatcherMiddleware>();
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            VerifyDispatcherIsRegistered(builder);

            if (!builder.Properties.TryGetValue(DispatcherRegisteredKey, out _))
            {
                var message = $"{nameof(DispatcherMiddleware)} must be added to the request execution pipeline before {nameof(EndpointMiddleware)}. " +
                    $"Please add {nameof(DispatcherMiddleware)} by calling '{nameof(IApplicationBuilder)}.{nameof(UseDispatcher)}' inside the call to 'Configure(...)' in the application startup code.";

                throw new InvalidOperationException(message);
            }

            return builder.UseMiddleware<EndpointMiddleware>();
        }

        private static void VerifyDispatcherIsRegistered(IApplicationBuilder app)
        {
            // Verify if AddDispatcher was done before calling UseDispatcher/UseEndpoint
            // We use the DispatcherMarkerService to make sure if all the services were added.
            if (app.ApplicationServices.GetService(typeof(DispatcherMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    nameof(DispatcherServiceCollectionExtensions.AddDispatcher),
                    "ConfigureServices(...)"));
            }
        }
    }
}
