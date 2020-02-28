// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides basic extension methods for registering <see cref="IHealthCheck"/> instances in an <see cref="IHealthChecksBuilder"/>.
    /// </summary>
    public static class HealthChecksBuilderAddCheckExtensions
    {
        /// <summary>
        /// Adds a new health check with the specified name and implementation.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="instance">An <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        // 2.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public static IHealthChecksBuilder AddCheck(
            this IHealthChecksBuilder builder,
            string name,
            IHealthCheck instance,
            HealthStatus? failureStatus,
            IEnumerable<string> tags)
        {
            return AddCheck(builder, name, instance, failureStatus, tags, default);
        }

        /// <summary>
        /// Adds a new health check with the specified name and implementation.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="instance">An <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddCheck(
            this IHealthChecksBuilder builder,
            string name,
            IHealthCheck instance,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return builder.Add(new HealthCheckRegistration(name, instance, failureStatus, tags, timeout));
        }

        /// <summary>
        /// Adds a new health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.GetServiceOrCreateInstance{T}(IServiceProvider)"/> to create the health check
        /// instance when needed. If a service of type <typeparamref name="T"/> is registered in the dependency injection container
        /// with any lifetime it will be used. Otherwise an instance of type <typeparamref name="T"/> will be constructed with
        /// access to services from the dependency injection container.
        /// </remarks>
        // 2.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public static IHealthChecksBuilder AddCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus,
            IEnumerable<string> tags) where T : class, IHealthCheck
        {
            return AddCheck<T>(builder, name, failureStatus, tags, default);
        }

        /// <summary>
        /// Adds a new health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.GetServiceOrCreateInstance{T}(IServiceProvider)"/> to create the health check
        /// instance when needed. If a service of type <typeparamref name="T"/> is registered in the dependency injection container
        /// with any lifetime it will be used. Otherwise an instance of type <typeparamref name="T"/> will be constructed with
        /// access to services from the dependency injection container.
        /// </remarks>
        public static IHealthChecksBuilder AddCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null) where T : class, IHealthCheck
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return builder.Add(new HealthCheckRegistration(name, s => ActivatorUtilities.GetServiceOrCreateInstance<T>(s), failureStatus, tags, timeout));
        }

        // NOTE: AddTypeActivatedCheck has overloads rather than default parameters values, because default parameter values don't
        // play super well with params.

        /// <summary>
        /// Adds a new type activated health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="args">Additional arguments to provide to the constructor.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/> to create the health check
        /// instance when needed. Additional arguments can be provided to the constructor via <paramref name="args"/>.
        /// </remarks>
        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, params object[] args) where T : class, IHealthCheck
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return AddTypeActivatedCheck<T>(builder, name, failureStatus: null, tags: null, args);
        }

        /// <summary>
        /// Adds a new type activated health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="args">Additional arguments to provide to the constructor.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/> to create the health check
        /// instance when needed. Additional arguments can be provided to the constructor via <paramref name="args"/>.
        /// </remarks>
        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus,
            params object[] args) where T : class, IHealthCheck
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return AddTypeActivatedCheck<T>(builder, name, failureStatus, tags: null, args);
        }

        /// <summary>
        /// Adds a new type activated health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <param name="args">Additional arguments to provide to the constructor.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/> to create the health check
        /// instance when needed. Additional arguments can be provided to the constructor via <paramref name="args"/>.
        /// </remarks>
        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus,
            IEnumerable<string> tags,
            params object[] args) where T : class, IHealthCheck
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return builder.Add(new HealthCheckRegistration(name, s => ActivatorUtilities.CreateInstance<T>(s, args), failureStatus, tags));
        }

        /// <summary>
        /// Adds a new type activated health check with the specified name and implementation.
        /// </summary>
        /// <typeparam name="T">The health check implementation type.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <param name="args">Additional arguments to provide to the constructor.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> representing the timeout of the check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        /// <remarks>
        /// This method will use <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/> to create the health check
        /// instance when needed. Additional arguments can be provided to the constructor via <paramref name="args"/>.
        /// </remarks>
        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus,
            IEnumerable<string> tags,
            TimeSpan timeout,
            params object[] args) where T : class, IHealthCheck
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return builder.Add(new HealthCheckRegistration(name, s => ActivatorUtilities.CreateInstance<T>(s, args), failureStatus, tags, timeout));
        }
    }
}
