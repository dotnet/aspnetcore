// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.ConcurrencyLimiter;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Concurrency limit extension methods for <see cref="IEndpointConventionBuilder"/>
    /// </summary>
    public static class ConcurrencyLimiterEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Adds the concurrency limit with LIFO stack as queueing strategy to the endpoint(s).
        /// </summary>
        /// <typeparam name="TBuilder"></typeparam>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="maxConcurrentRequests">
        /// Maximum number of concurrent requests. Any extras will be queued on the server. 
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        /// <param name="requestQueueLimit">
        ///Maximum number of queued requests before the server starts rejecting connections with '503 Service Unavailible'.
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        /// <returns></returns>
        public static TBuilder RequireStackPolicy<TBuilder>(this TBuilder builder,
            int maxConcurrentRequests,
            int requestQueueLimit)
            where TBuilder : IEndpointConventionBuilder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Add(endpoints =>
            {
                endpoints.Metadata.Add(new StackPolicyAttribute(maxConcurrentRequests, requestQueueLimit));
            });

            return builder;
        }
        /// <summary>
        /// Adds the concurrency limit with FIFO queue as queueing strategy to the endpoint(s).
        /// </summary>
        /// <typeparam name="TBuilder"></typeparam>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="maxConcurrentRequests">
        /// Maximum number of concurrent requests. Any extras will be queued on the server. 
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        /// <param name="requestQueueLimit">
        ///Maximum number of queued requests before the server starts rejecting connections with '503 Service Unavailible'.
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        /// <returns></returns>
        public static TBuilder RequireQueuePolicy<TBuilder>(this TBuilder builder,
            int maxConcurrentRequests,
            int requestQueueLimit)
            where TBuilder : IEndpointConventionBuilder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Add(endpoints =>
            {
                endpoints.Metadata.Add(new QueuePolicyAttribute(maxConcurrentRequests, requestQueueLimit));
            });

            return builder;
        }
        /// <summary>
        /// Suppresses the concurrency limit to the endpoint(s).
        /// </summary>
        /// <typeparam name="TBuilder"></typeparam>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <returns></returns>
        public static TBuilder SupressQueuePolicy<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Add(endpoints =>
            {
                endpoints.Metadata.Add(new SuppressQueuePolicyAttribute());
            });

            return builder;
        }
    }
}
