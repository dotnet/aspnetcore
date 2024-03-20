// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Caching.Distributed;

public static class HybridCacheServiceExtensions
{
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        AddHybridCache(services);
        services.Configure(setupAction);
        return new HybridCacheBuilder(services);
    }

    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // we need a backend; use in-proc by default
        services.AddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.AddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<HybridCache, DefaultHybridCache>();
        return new HybridCacheBuilder(services);
    }

    public static IHybridCacheBuilder WithSerializer<T>(this IHybridCacheBuilder builder, IHybridCacheSerializer<T> serializer)
    {
        builder.Services.AddSingleton<IHybridCacheSerializer<T>>(serializer);
        return builder;
    }
    public static IHybridCacheBuilder WithSerializer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializer<T>
    {
        builder.Services.AddSingleton<IHybridCacheSerializer<T>, TImplementation>();
        return builder;
    }

    public static IHybridCacheBuilder WithSerializerFactory(this IHybridCacheBuilder builder, IHybridCacheSerializerFactory factory)
    {
        builder.Services.AddSingleton<IHybridCacheSerializerFactory>(factory);
        return builder;
    }
    public static IHybridCacheBuilder WithSerializerFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializerFactory
{
        builder.Services.AddSingleton<IHybridCacheSerializerFactory, TImplementation>();
        return builder;
    }

}
