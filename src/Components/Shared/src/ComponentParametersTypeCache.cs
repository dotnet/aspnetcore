// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentParametersTypeCache
{
    private readonly ConcurrentDictionary<Key, Type?> _typeToKeyLookUp = new();

    public Type? GetParameterType(string assembly, string type)
    {
        var key = new Key(assembly, type);
        if (_typeToKeyLookUp.TryGetValue(key, out var resolvedType))
        {
            return resolvedType;
        }
        else
        {
            return _typeToKeyLookUp.GetOrAdd(key, ResolveType, AppDomain.CurrentDomain.GetAssemblies());
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to preserve component parameters.")]
    private static Type? ResolveType(Key key, Assembly[] assemblies)
    {
        Assembly? assembly = null;
        for (var i = 0; i < assemblies.Length; i++)
        {
            var current = assemblies[i];
            if (current.GetName().Name == key.Assembly)
            {
                assembly = current;
                break;
            }
        }

        if (assembly == null)
        {
            return null;
        }

        return assembly.GetType(key.Type, throwOnError: false, ignoreCase: false);
    }

    private struct Key : IEquatable<Key>
    {
        public Key(string assembly, string type) =>
            (Assembly, Type) = (assembly, type);

        public string Assembly { get; set; }

        public string Type { get; set; }

        public override bool Equals(object? obj) => obj is Key key && Equals(key);

        public bool Equals(Key other) => string.Equals(Assembly, other.Assembly, StringComparison.Ordinal) &&
            string.Equals(Type, other.Type, StringComparison.Ordinal);

        public override int GetHashCode() => HashCode.Combine(Assembly, Type);
    }
}
