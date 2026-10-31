// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Test.Helpers;

public class TestServiceProvider : IServiceProvider, IKeyedServiceProvider
{
    private readonly Dictionary<Type, Func<object?>> _factories = new();

    private Dictionary<(Type, object?), Func<object?>>? _keyedFactories;

    private Dictionary<(Type, object?), Func<object?>> KeyedFactories
    {
        get
        {
            _keyedFactories ??= new();
            return _keyedFactories;
        }
    }

    public object? GetService(Type serviceType)
        => _factories.TryGetValue(serviceType, out var factory)
            ? factory()
            : null;

    public object? GetKeyedService(Type serviceType, object? serviceKey)
        => KeyedFactories.TryGetValue((serviceType, serviceKey), out var factory)
            ? factory()
            : null;

    internal void AddService<T>(T value)
        => _factories.Add(typeof(T), () => value);

    internal void AddKeyedService<T>(T value, object serviceKey)
        => KeyedFactories.Add((typeof(T), serviceKey), () => value);

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => throw new NotImplementedException();
}
