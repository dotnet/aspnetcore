// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Options for the default service that executes <see cref="IHealthCheckPublisher"/> instances.
    /// </summary>
    public sealed class HealthCheckPublisherOptions
    {
        private TimeSpan _delay;
        private TimeSpan _period;

        public HealthCheckPublisherOptions()
        {
            _delay = TimeSpan.FromSeconds(5);
            _period = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Gets or sets the initial delay applied after the application starts before executing 
        /// <see cref="IHealthCheckPublisher"/> instances. The delay is applied once at startup, and does
        /// not apply to subsequent iterations. The default value is 5 seconds.
        /// </summary>
        public TimeSpan Delay
        {
            get => _delay;
            set
            {
                if (value == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentException($"The {nameof(Delay)} must not be infinite.", nameof(value));
                }

                _delay = value;
            }
        }

        /// <summary>
        /// Gets or sets the period of <see cref="IHealthCheckPublisher"/> execution. The default value is
        /// 30 seconds.
        /// </summary>
        /// <remarks>
        /// The <see cref="Period"/> cannot be set to a value lower than 1 second.
        /// </remarks>
        public TimeSpan Period
        {
            get => _period;
            set
            {
                if (value < TimeSpan.FromSeconds(1))
                {
                    throw new ArgumentException($"The {nameof(Period)} must be greater than or equal to one second.", nameof(value));
                }

                if (value == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentException($"The {nameof(Period)} must not be infinite.", nameof(value));
                }

                _period = value;
            }
        }

        /// <summary>
        /// Gets or sets a predicate that is used to filter the set of health checks executed.
        /// </summary>
        /// <remarks>
        /// If <see cref="Predicate"/> is <c>null</c>, the health check publisher service will run all
        /// registered health checks - this is the default behavior. To run a subset of health checks,
        /// provide a function that filters the set of checks. The predicate will be evaluated each period.
        /// </remarks>
        public Func<HealthCheckRegistration, bool> Predicate { get; set; }

        /// <summary>
        /// Gets or sets the timeout for executing the health checks an all <see cref="IHealthCheckPublisher"/> 
        /// instances. Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to execute with no timeout.
        /// The default value is 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
