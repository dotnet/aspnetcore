// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring cascading values on an <see cref="IServiceCollection"/>.
/// </summary>
public static class CascadingValueServiceCollectionExtensions
{
    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>. This is equivalent to having
    /// a fixed <see cref="CascadingValue{TValue}"/> at the root of the component hierarchy.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="initialValueFactory">A callback that supplies a fixed value within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, Func<IServiceProvider, TValue> initialValueFactory)
        => serviceCollection.AddScoped<ICascadingValueSupplier>(sp => new CascadingValueSource<TValue>(() => initialValueFactory(sp), isFixed: true));

    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>. This is equivalent to having
    /// a fixed <see cref="CascadingValue{TValue}"/> at the root of the component hierarchy.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="name">A name for the cascading value. If set, <see cref="CascadingParameterAttribute"/> can be configured to match based on this name.</param>
    /// <param name="initialValueFactory">A callback that supplies a fixed value within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, string name, Func<IServiceProvider, TValue> initialValueFactory)
        => serviceCollection.AddScoped<ICascadingValueSupplier>(sp => new CascadingValueSource<TValue>(name, () => initialValueFactory(sp), isFixed: true));

    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>. This is equivalent to having
    /// a <see cref="CascadingValue{TValue}"/> at the root of the component hierarchy.
    ///
    /// With this overload, you can supply a <see cref="CascadingValueSource{TValue}"/> which allows you
    /// to notify about updates to the value later, causing recipients to re-render. This overload should
    /// only be used if you plan to update the value dynamically.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="sourceFactory">A callback that supplies a <see cref="CascadingValueSource{TValue}"/> within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, Func<IServiceProvider, CascadingValueSource<TValue>> sourceFactory)
        => serviceCollection.AddScoped<ICascadingValueSupplier>(sourceFactory);

    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>, if none is already registered
    /// with the value type. This is equivalent to having a fixed <see cref="CascadingValue{TValue}"/> at
    /// the root of the component hierarchy.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="valueFactory">A callback that supplies a fixed value within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static void TryAddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, Func<IServiceProvider, TValue> valueFactory)
    {
        serviceCollection.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICascadingValueSupplier, CascadingValueSource<TValue>>(
                sp => new CascadingValueSource<TValue>(() => valueFactory(sp), isFixed: true)));
    }

    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>, if none is already registered
    /// with the value type, regardless of the <paramref name="name"/>. This is equivalent to having a fixed
    /// <see cref="CascadingValue{TValue}"/> at the root of the component hierarchy.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="name">A name for the cascading value. If set, <see cref="CascadingParameterAttribute"/> can be configured to match based on this name.</param>
    /// <param name="valueFactory">A callback that supplies a fixed value within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static void TryAddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, string name, Func<IServiceProvider, TValue> valueFactory)
    {
        serviceCollection.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICascadingValueSupplier, CascadingValueSource<TValue>>(
                sp => new CascadingValueSource<TValue>(name, () => valueFactory(sp), isFixed: true)));
    }

    /// <summary>
    /// Adds a cascading value to the <paramref name="serviceCollection"/>, if none is already registered
    /// with the value type. This is equivalent to having a fixed <see cref="CascadingValue{TValue}"/> at
    /// the root of the component hierarchy.
    /// 
    /// With this overload, you can supply a <see cref="CascadingValueSource{TValue}"/> which allows you
    /// to notify about updates to the value later, causing recipients to re-render. This overload should
    /// only be used if you plan to update the value dynamically.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="sourceFactory">A callback that supplies a <see cref="CascadingValueSource{TValue}"/> within each service provider scope.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static void TryAddCascadingValue<TValue>(
        this IServiceCollection serviceCollection, Func<IServiceProvider, CascadingValueSource<TValue>> sourceFactory)
    {
        serviceCollection.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICascadingValueSupplier, CascadingValueSource<TValue>>(sourceFactory));
    }
}
