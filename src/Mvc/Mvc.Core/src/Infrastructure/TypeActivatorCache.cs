// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Caches <see cref="ObjectFactory"/> instances produced by
/// <see cref="ActivatorUtilities.CreateFactory(Type, Type[])"/>.
/// </summary>
internal sealed class TypeActivatorCache : ITypeActivatorCache
{
    private readonly Func<Type, ObjectFactory> _createFactory =
        (type) => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes);
    private readonly ConcurrentDictionary<Type, ObjectFactory> _typeActivatorCache =
           new ConcurrentDictionary<Type, ObjectFactory>();

    /// <inheritdoc/>
    public TInstance CreateInstance<TInstance>(
        IServiceProvider serviceProvider,
        Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(implementationType);

        var createFactory = _typeActivatorCache.GetOrAdd(implementationType, _createFactory);
        return (TInstance)createFactory(serviceProvider, arguments: null);
    }
}
