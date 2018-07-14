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
        public static IApplicationBuilder UseDispatcher(this IApplicationBuilder builder)
        {
            VerifyDispatcherIsRegistered(builder);

            return builder.UseMiddleware<DispatcherMiddleware>();
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            VerifyDispatcherIsRegistered(builder);

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
