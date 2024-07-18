// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configuration extension methods for <see cref="IHybridCacheBuilder"/> / <see cref="HybridCache"/>.
/// </summary>
public static class HybridCacheBuilderExtensions
{
    /// <summary>
    /// Serialize values of type <typeparamref name="T"/> with the specified serializer from <paramref name="serializer"/>.
    /// </summary>
    public static IHybridCacheBuilder AddSerializer<T>(this IHybridCacheBuilder builder, IHybridCacheSerializer<T> serializer)
    {
        builder.Services.AddSingleton<IHybridCacheSerializer<T>>(serializer);
        return builder;
    }

    /// <summary>
    /// Serialize values of type <typeparamref name="T"/> with the serializer of type <typeparamref name="TImplementation"/>.
    /// </summary>
    public static IHybridCacheBuilder AddSerializer<T,
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializer<T>
    {
        builder.Services.AddSingleton<IHybridCacheSerializer<T>, TImplementation>();
        return builder;
    }

    /// <summary>
    /// Add <paramref name="factory"/> as an additional serializer factory, which can provide serializers for multiple types.
    /// </summary>
    public static IHybridCacheBuilder AddSerializerFactory(this IHybridCacheBuilder builder, IHybridCacheSerializerFactory factory)
    {
        builder.Services.AddSingleton<IHybridCacheSerializerFactory>(factory);
        return builder;
    }

    /// <summary>
    /// Add a factory of type <typeparamref name="TImplementation"/> as an additional serializer factory, which can provide serializers for multiple types.
    /// </summary>
    public static IHybridCacheBuilder AddSerializerFactory<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializerFactory
    {
        builder.Services.AddSingleton<IHybridCacheSerializerFactory, TImplementation>();
        return builder;
    }
}
