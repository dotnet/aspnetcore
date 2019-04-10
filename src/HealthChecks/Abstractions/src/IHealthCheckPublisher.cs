// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents a publisher of <see cref="HealthReport"/> information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default health checks implementation provided an <c>IHostedService</c> implementation that can
    /// be used to execute health checks at regular intervals and provide the resulting <see cref="HealthReport"/>
    /// data to all registered <see cref="IHealthCheckPublisher"/> instances.
    /// </para>
    /// <para>
    /// To provide an <see cref="IHealthCheckPublisher"/> implementation, register an instance or type as a singleton
    /// service in the dependency injection container.
    /// </para>
    /// <para>
    /// <see cref="IHealthCheckPublisher"/> instances are provided with a <see cref="HealthReport"/> after executing
    /// health checks in a background thread. The use of <see cref="IHealthCheckPublisher"/> depend on hosting in
    /// an application using <c>IWebHost</c> or generic host (<c>IHost</c>). Execution of <see cref="IHealthCheckPublisher"/>
    /// instance is not related to execution of health checks via a middleware.
    /// </para>
    /// </remarks>
    public interface IHealthCheckPublisher
    {
        /// <summary>
        /// Publishes the provided <paramref name="report"/>.
        /// </summary>
        /// <param name="report">The <see cref="HealthReport"/>. The result of executing a set of health checks.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete when publishing is complete.</returns>
        Task PublishAsync(HealthReport report, CancellationToken cancellationToken);
    }
}
