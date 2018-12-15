// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks.Builder
{
    public static class HealthCheckEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHealthChecks(
           this IEndpointRouteBuilder builder,
           string pattern)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return MapHealthChecksCore(builder, pattern, null);
        }

        public static IEndpointConventionBuilder MapHealthChecks(
           this IEndpointRouteBuilder builder,
           string pattern,
           HealthCheckOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return MapHealthChecksCore(builder, pattern, options);
        }

        private static IEndpointConventionBuilder MapHealthChecksCore(IEndpointRouteBuilder builder, string pattern, HealthCheckOptions options)
        {
            var args = options != null ? new[] { Options.Create(options) } : Array.Empty<object>();

            var pipeline = builder.CreateApplicationBuilder()
               .UseMiddleware<HealthCheckMiddleware>(args)
               .Build();

            return builder.Map(pattern, "Health checks", pipeline);
        }
    }
}
