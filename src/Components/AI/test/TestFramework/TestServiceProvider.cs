// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, Func<object?>> _factories = new();

    internal TestServiceProvider()
    {
        AddService<IJSRuntime>(new NullJSRuntime());
    }

    public object? GetService(Type serviceType)
    {
        if (_factories.TryGetValue(serviceType, out var factory))
        {
            return factory();
        }

        return null;
    }

    internal void AddService<T>(T value) where T : notnull
    {
        _factories[typeof(T)] = () => value;
    }
}
