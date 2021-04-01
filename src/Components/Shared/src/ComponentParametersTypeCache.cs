// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    internal class ComponentParametersTypeCache
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

        [RequiresUnreferencedCode("This type attempts to load component parameters that may be trimmed.")]
        private static Type? ResolveType(Key key, Assembly[] assemblies)
        {
            var assembly = assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, key.Assembly, StringComparison.Ordinal));

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
}
