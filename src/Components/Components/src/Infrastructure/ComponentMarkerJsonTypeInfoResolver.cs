// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal sealed class ComponentMarkerJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    private readonly object _lock = new();
    private IJsonTypeInfoResolver[] _resolvers = [];

    public static ComponentMarkerJsonTypeInfoResolver Instance { get; } = new();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var resolvers = Volatile.Read(ref _resolvers);
        foreach (var resolver in resolvers)
        {
            if (resolver.GetTypeInfo(type, options) is { } typeInfo)
            {
                return typeInfo;
            }
        }

        return null;
    }

    public void AddResolver(IJsonTypeInfoResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        lock (_lock)
        {
            if (Array.IndexOf(_resolvers, resolver) >= 0)
            {
                return;
            }

            var updated = new IJsonTypeInfoResolver[_resolvers.Length + 1];
            Array.Copy(_resolvers, updated, _resolvers.Length);
            updated[^1] = resolver;
            Volatile.Write(ref _resolvers, updated);
        }
    }
}
