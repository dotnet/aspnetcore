// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using Microsoft.AspNetCore.ConcurrencyLimiter;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConcurrencyLimiterBuilderExtensions
    {
        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a FIFO queue as its queueing strategy.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure">Set the options used by the queue.</param>
        /// <returns></returns>
        public static ConcurrencyLimiterBuilder AddQueuePolicy(this ConcurrencyLimiterBuilder builder, Action<QueuePolicyOptions> configure)
        {
            builder.Services.AddOptions<QueuePolicyOptions>().Configure(configure);

            builder.Services.AddSingleton<IQueuePolicy, QueuePolicy>();
            return builder;
        }

        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a FIFO queue as its queueing strategy.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure">Set the options used by the queue.</param>
        /// <returns></returns>
        public static ConcurrencyLimiterBuilder AddQueuePolicy(this ConcurrencyLimiterBuilder builder, Action<QueuePolicyOptions, IServiceProvider> configure)
        {
            builder.Services.AddOptions<QueuePolicyOptions>().Configure(configure);

            builder.Services.AddSingleton<IQueuePolicy, QueuePolicy>();
            return builder;
        }

        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a LIFO stack as its queueing strategy.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure">Set the options used by the queue.</param>
        /// <returns></returns>
        public static ConcurrencyLimiterBuilder AddStackPolicy(this ConcurrencyLimiterBuilder builder, Action<QueuePolicyOptions> configure)
        {
            builder.Services.AddOptions<QueuePolicyOptions>().Configure(configure);

            builder.Services.AddSingleton<IQueuePolicy, StackPolicy>();
            return builder;
        }

        /// <summary>
        /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a LIFO stack as its queueing strategy.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure">Set the options used by the queue.</param>
        /// <returns></returns>
        public static ConcurrencyLimiterBuilder AddStackPolicy(this ConcurrencyLimiterBuilder builder, Action<QueuePolicyOptions, IServiceProvider> configure)
        {
            builder.Services.AddOptions<QueuePolicyOptions>().Configure(configure);

            builder.Services.AddSingleton<IQueuePolicy, StackPolicy>();
            return builder;
        }
    }
}
