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
            // It might be that the assembly is not loaded yet, this can happen if the root component is defined in a
            // different assembly than the app and there is no reference from the app assembly to any type in the class
            // library that has been used yet.
            // In this case, try and load the assembly and look up the type again.
            // We only need to do this in the browser because its a different process, in the server the assembly will already
            // be loaded.
            if (OperatingSystem.IsBrowser())
            {
                try
                {
                    assembly = Assembly.Load(key.Assembly);
                }
                catch
                {
                    // It's fine to ignore the exception, since we'll return null below.
                }
            }
        }

        return assembly?.GetType(key.Type, throwOnError: false, ignoreCase: false);
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
