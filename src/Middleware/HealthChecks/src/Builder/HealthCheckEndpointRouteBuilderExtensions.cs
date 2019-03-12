// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add health checks.
    /// </summary>
    public static class HealthCheckEndpointRouteBuilderExtensions
    {
        private const string DefaultDisplayName = "Health checks";

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

            return MapHealthChecksCore(builder, pattern, null, DefaultDisplayName);
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

            return MapHealthChecksCore(builder, pattern, options, DefaultDisplayName);
        }

        /// <summary>
        /// Adds a health checks endpoint to the <see cref="IEndpointRouteBuilder"/> with the specified template, options and display name.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the health checks endpoint to.</param>
        /// <param name="pattern">The URL pattern of the health checks endpoint.</param>
        /// <param name="options">A <see cref="HealthCheckOptions"/> used to configure the health checks.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <returns>A convention builder for the health checks endpoint.</returns>
        public static IEndpointConventionBuilder MapHealthChecks(
           this IEndpointRouteBuilder builder,
           string pattern,
           HealthCheckOptions options,
           string displayName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("A valid non-empty display name must be provided.", nameof(displayName));
            }

            return MapHealthChecksCore(builder, pattern, options, displayName);
        }

        private static IEndpointConventionBuilder MapHealthChecksCore(IEndpointRouteBuilder builder, string pattern, HealthCheckOptions options, string displayName)
        {
            if (builder.ServiceProvider.GetService(typeof(HealthCheckService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    nameof(HealthCheckServiceCollectionExtensions.AddHealthChecks),
                    "ConfigureServices(...)"));
            }

            var args = options != null ? new[] { Options.Create(options) } : Array.Empty<object>();

            var pipeline = builder.CreateApplicationBuilder()
               .UseMiddleware<HealthCheckMiddleware>(args)
               .Build();

            return builder.Map(pattern, displayName, pipeline);
        }
    }
}
