// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal sealed class DefaultComponentActivator(IServiceProvider serviceProvider) : IComponentActivator
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> _cachedComponentTypeInfo = new();

    public static void ClearCache() => _cachedComponentTypeInfo.Clear();

    /// <inheritdoc />
    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        var factory = GetObjectFactory(componentType);

        return (IComponent)factory(serviceProvider, []);
    }

    private static ObjectFactory GetObjectFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        // Unfortunately we can't use 'GetOrAdd' here because the DynamicallyAccessedMembers annotation doesn't flow through to the
        // callback, so it becomes an IL2111 warning. The following is equivalent and thread-safe because it's a ConcurrentDictionary
        // and it doesn't matter if we build a cache entry more than once.
        if (!_cachedComponentTypeInfo.TryGetValue(componentType, out var factory))
        {
            factory = ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes);
            _cachedComponentTypeInfo.TryAdd(componentType, factory);
        }

        return factory;
    }
}
