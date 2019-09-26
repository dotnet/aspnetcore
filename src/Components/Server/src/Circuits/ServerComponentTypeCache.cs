// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    // A cache for root component types
    internal class ServerComponentTypeCache
    {
        private readonly ConcurrentDictionary<Key, Type> _typeToKeyLookUp = new ConcurrentDictionary<Key, Type>();

        public Type GetRootComponent(string assembly, string type)
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

        private static Type ResolveType(Key key, Assembly[] assemblies)
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

            public override bool Equals(object obj) => Equals((Key)obj);

            public bool Equals(Key other) => string.Equals(Assembly, other.Assembly, StringComparison.Ordinal) &&
                string.Equals(Type, other.Type, StringComparison.Ordinal);

            public override int GetHashCode() => HashCode.Combine(Assembly, Type);
        }
    }
}
