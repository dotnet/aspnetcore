// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal static class RootComponentTypeCache
    {
        private static readonly Dictionary<Key, Type> _typeToKeyLookUp = new ();

        public static Type GetRootComponent(string assembly, string type)
        {
            var key = new Key(assembly, type);
            if (_typeToKeyLookUp.TryGetValue(key, out var resolvedType))
            {
                return resolvedType;
            }
            else
            {
                resolvedType = ResolveType(key, AppDomain.CurrentDomain.GetAssemblies());
                _typeToKeyLookUp[key] = resolvedType;
                return resolvedType;
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
