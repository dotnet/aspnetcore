// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add health checks.
    /// </summary>
    public static class HealthCheckEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a health checks endpoint to the <see cref="IEndpointRouteBuilder"/> with the specified template.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the health checks endpoint to.</param>
        /// <param name="pattern">The URL pattern of the health checks endpoint.</param>
        /// <returns>A convention builder for the health checks endpoint.</returns>
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

        /// <summary>
        /// Adds a health checks endpoint to the <see cref="IEndpointRouteBuilder"/> with the specified template and options.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the health checks endpoint to.</param>
        /// <param name="pattern">The URL pattern of the health checks endpoint.</param>
        /// <param name="options">A <see cref="HealthCheckOptions"/> used to configure the health checks.</param>
        /// <returns>A convention builder for the health checks endpoint.</returns>
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
