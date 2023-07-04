// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class CascadingValueServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="services"></param>
    /// <param name="valueFactory"></param>
    /// <returns></returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection services, Func<IServiceProvider, TValue> valueFactory)
        => services.AddScoped<ICascadingValueSupplier>(sp => new CascadingValueSource<TValue>(valueFactory(sp), isFixed: true));

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="services"></param>
    /// <param name="name"></param>
    /// <param name="valueFactory"></param>
    /// <returns></returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection services, string name, Func<IServiceProvider, TValue> valueFactory)
        => services.AddScoped<ICascadingValueSupplier>(sp => new CascadingValueSource<TValue>(name, valueFactory(sp), isFixed: true));

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="services"></param>
    /// <param name="sourceFactory"></param>
    /// <returns></returns>
    public static IServiceCollection AddCascadingValue<TValue>(
        this IServiceCollection services, Func<IServiceProvider, CascadingValueSource<TValue>> sourceFactory)
        => services.AddScoped<ICascadingValueSupplier>(sourceFactory);
}
