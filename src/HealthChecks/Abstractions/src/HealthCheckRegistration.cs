// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represent the registration information associated with an <see cref="IHealthCheck"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The health check registration is provided as a separate object so that application developers can customize
    /// how health check implementations are configured.
    /// </para>
    /// <para>
    /// The registration is provided to an <see cref="IHealthCheck"/> implementation during execution through
    /// <see cref="HealthCheckContext.Registration"/>. This allows a health check implementation to access named
    /// options or perform other operations based on the registered name.
    /// </para>
    /// </remarks>
    public sealed class HealthCheckRegistration
    {
        private Func<IServiceProvider, IHealthCheck> _factory;
        private string _name;
        private TimeSpan _timeout;

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration" /> for an existing <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck" /> instance.
        /// </summary>
        /// <param name="name">The health check name.</param>
        /// <param name="instance">The <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck" /> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus" /> that should be reported upon failure of the health check. If the provided value
        /// is <c>null</c>, then <see cref="F:Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
        public HealthCheckRegistration(string name, IHealthCheck instance, HealthStatus? failureStatus, IEnumerable<string> tags)
            : this(name, instance, failureStatus, tags, default)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HealthCheckRegistration"/> for an existing <see cref="IHealthCheck"/> instance.
        /// </summary>
        /// <param name="name">The health check name.</param>
        /// <param name="instance">The <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported upon failure of the health check. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
        /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
        public HealthCheckRegistration(string name, IHealthCheck instance, HealthStatus? failureStatus, IEnumerable<string> tags, TimeSpan? timeout)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (timeout <= TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            Name = name;
            FailureStatus = failureStatus ?? HealthStatus.Unhealthy;
            Tags = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            Factory = (_) => instance;
            Timeout = timeout ?? System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Creates a new <see cref="HealthCheckRegistration"/> for an existing <see cref="IHealthCheck"/> instance.
        /// </summary>
        /// <param name="name">The health check name.</param>
        /// <param name="factory">A delegate used to create the <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
        public HealthCheckRegistration(
            string name,
            Func<IServiceProvider, IHealthCheck> factory,
            HealthStatus? failureStatus,
            IEnumerable<string> tags)
            : this(name, factory, failureStatus, tags, default)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HealthCheckRegistration"/> for an existing <see cref="IHealthCheck"/> instance.
        /// </summary>
        /// <param name="name">The health check name.</param>
        /// <param name="factory">A delegate used to create the <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
        /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
        public HealthCheckRegistration(
            string name,
            Func<IServiceProvider, IHealthCheck> factory,
            HealthStatus? failureStatus,
            IEnumerable<string> tags,
            TimeSpan? timeout)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (timeout <= TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            Name = name;
            FailureStatus = failureStatus ?? HealthStatus.Unhealthy;
            Tags = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            Factory = factory;
            Timeout = timeout ?? System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Gets or sets a delegate used to create the <see cref="IHealthCheck"/> instance.
        /// </summary>
        public Func<IServiceProvider, IHealthCheck> Factory
        {
            get => _factory;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _factory = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="HealthStatus"/> that should be reported upon failure of the health check.
        /// </summary>
        public HealthStatus FailureStatus { get; set; }

        /// <summary>
        /// Gets or sets the timeout used for the test.
        /// </summary>
        public TimeSpan Timeout
        {
            get => _timeout;
            set
            {
                if (value <= TimeSpan.Zero && value != System.Threading.Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the health check name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets a list of tags that can be used for filtering health checks.
        /// </summary>
        public ISet<string> Tags { get; }
    }
}
