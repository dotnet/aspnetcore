// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ConcurrencyLimiter;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for specifying which queue the middleware should use.
    /// </summary>
    public static class QueuePolicyServiceCollectionExtensions
    {
        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a FIFO queue as its queueing strategy.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">Set the options used by the queue.
        /// Mandatory, since <see cref="QueuePolicyOptions.MaxConcurrentRequests"></see> must be provided.</param>
        /// <returns></returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use AddConcurrencyLimiter(configure).AddQueuePolicy() instead")]
        public static IServiceCollection AddQueuePolicy(this IServiceCollection services, Action<QueuePolicyOptions> configure)
        {
            services.AddConcurrencyLimiter(configure)
                .AddQueuePolicy();

            return services;
        }

        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a LIFO stack as its queueing strategy.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">Set the options used by the queue.
        /// Mandatory, since <see cref="QueuePolicyOptions.MaxConcurrentRequests"></see> must be provided.</param>
        /// <returns></returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use AddConcurrencyLimiter(configure).AddStackPolicy() instead")]
        public static IServiceCollection AddStackPolicy(this IServiceCollection services, Action<QueuePolicyOptions> configure)
        {
            services.AddConcurrencyLimiter(configure)
               .AddStackPolicy();

            return services;
        }
    }
}
