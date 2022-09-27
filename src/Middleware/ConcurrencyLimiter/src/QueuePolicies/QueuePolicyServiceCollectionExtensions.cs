// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ConcurrencyLimiter;

namespace Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddQueuePolicy(this IServiceCollection services, Action<QueuePolicyOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IQueuePolicy, QueuePolicy>();
        return services;
    }

    /// <summary>
    /// Tells <see cref="ConcurrencyLimiterMiddleware"/> to use a LIFO stack as its queueing strategy.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">Set the options used by the queue.
    /// Mandatory, since <see cref="QueuePolicyOptions.MaxConcurrentRequests"></see> must be provided.</param>
    /// <returns></returns>
    public static IServiceCollection AddStackPolicy(this IServiceCollection services, Action<QueuePolicyOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IQueuePolicy, StackPolicy>();
        return services;
    }
}
