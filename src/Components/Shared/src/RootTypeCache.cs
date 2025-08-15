// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;

#if COMPONENTS
namespace Microsoft.AspNetCore.Components.Infrastructure;
#else
namespace Microsoft.AspNetCore.Components;
#endif

// A cache for root component types
internal sealed class RootTypeCache
{
    private static readonly List<WeakReference> _instances = new();

    static RootTypeCache()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    private readonly ConcurrentDictionary<Key, Type?> _typeToKeyLookUp = new();

    public RootTypeCache()
    {
        lock (_instances)
        {
            _instances.Add(new WeakReference(this));
        }
    }

    private static void ClearCache()
    {
        lock (_instances)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var weakRef = _instances[i];
                if (weakRef.Target is RootTypeCache instance)
                {
                    instance._typeToKeyLookUp.Clear();
                }
                else
                {
                    // Remove dead reference
                    _instances.RemoveAt(i);
                }
            }
        }
    }

    public Type? GetRootType(string assembly, string type)
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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Root components are expected to be defined in assemblies that do not get trimmed.")]
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

    private readonly struct Key : IEquatable<Key>
    {
        public Key(string assembly, string type) =>
            (Assembly, Type) = (assembly, type);

        public string Assembly { get; }

        public string Type { get; }

        public override bool Equals(object? obj) => obj is Key key && Equals(key);

        public bool Equals(Key other) => string.Equals(Assembly, other.Assembly, StringComparison.Ordinal) &&
            string.Equals(Type, other.Type, StringComparison.Ordinal);

        public override int GetHashCode() => HashCode.Combine(Assembly, Type);
    }
}
