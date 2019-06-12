// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Schema;
using Microsoft.AspNetCore.RequestThrottling;
using Microsoft.AspNetCore.RequestThrottling.Strategies;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for adding Q
    /// </summary>
    public static class QueuePolicyServiceCollectionExtensions
    {
        /// <summary>
        /// Tells <see cref="RequestThrottlingMiddleware"/> to use a TailDrop queue as its queueing strategy.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">Set the options used by the queue.
        /// Mandatory, since <see cref="TailDropOptions.MaxConcurrentRequests"></see> must be provided.</param>
        /// <returns></returns>
        public static IServiceCollection AddTailDropQueue(this IServiceCollection services, Action<TailDropOptions> configure)
        {
            services.Configure<TailDropOptions>(configure);
            services.AddSingleton<IQueuePolicy, TailDrop>();
            return services;
        }
    }
}
