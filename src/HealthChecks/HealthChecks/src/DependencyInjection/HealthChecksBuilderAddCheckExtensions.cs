// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static IHealthChecksBuilder AddCheck(
        this IHealthChecksBuilder builder,
        string name,
        IHealthCheck instance,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);
        ArgumentNullThrowHelper.ThrowIfNull(instance);

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
    public static IHealthChecksBuilder AddCheck<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
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
    public static IHealthChecksBuilder AddCheck<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null) where T : class, IHealthCheck
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        return builder.Add(new HealthCheckRegistration(name, GetServiceOrCreateInstance, failureStatus, tags, timeout));

        [UnconditionalSuppressMessage("Trimming", "IL2091",
           Justification = "DynamicallyAccessedMemberTypes.PublicConstructors is enforced by calling method.")]
        static T GetServiceOrCreateInstance(IServiceProvider serviceProvider) =>
            ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
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
    public static IHealthChecksBuilder AddTypeActivatedCheck<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IHealthChecksBuilder builder, string name, params object[] args) where T : class, IHealthCheck
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);

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
    public static IHealthChecksBuilder AddTypeActivatedCheck<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus,
        params object[] args) where T : class, IHealthCheck
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);

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
    public static IHealthChecksBuilder AddTypeActivatedCheck<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus,
        IEnumerable<string>? tags,
        params object[] args) where T : class, IHealthCheck
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        return builder.Add(new HealthCheckRegistration(name, CreateInstance, failureStatus, tags));

        [UnconditionalSuppressMessage("Trimming", "IL2091",
           Justification = "DynamicallyAccessedMemberTypes.PublicConstructors is enforced by calling method.")]
        T CreateInstance(IServiceProvider serviceProvider) =>
            ActivatorUtilities.CreateInstance<T>(serviceProvider, args);
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
    public static IHealthChecksBuilder AddTypeActivatedCheck<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus,
        IEnumerable<string> tags,
        TimeSpan timeout,
        params object[] args) where T : class, IHealthCheck
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        return builder.Add(new HealthCheckRegistration(name, CreateInstance, failureStatus, tags, timeout));

        [UnconditionalSuppressMessage("Trimming", "IL2091",
            Justification = "DynamicallyAccessedMemberTypes.PublicConstructors is enforced by calling method.")]
        T CreateInstance(IServiceProvider serviceProvider) => ActivatorUtilities.CreateInstance<T>(serviceProvider, args);
    }
}
