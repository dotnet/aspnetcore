// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.ObjectPool;

internal sealed class DependencyInjectionPooledObjectPolicy<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation> : IPooledObjectPolicy<TService>
    where TService : class
    where TImplementation : class, TService
{
    private readonly IServiceProvider _provider;
    private readonly ObjectFactory _factory;
    private readonly bool _isResettable;

    public DependencyInjectionPooledObjectPolicy(IServiceProvider provider)
    {
        _provider = provider;
        _factory = ActivatorUtilities.CreateFactory(typeof(TImplementation), Type.EmptyTypes);
        _isResettable = typeof(IResettable).IsAssignableFrom(typeof(TImplementation));
    }

    public TService Create() => (TService)_factory(_provider, Array.Empty<object?>());

    public bool Return(TService obj)
    {
        if (_isResettable)
        {
            return ((IResettable)obj).TryReset();
        }

        return true;
    }
}
