// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Resolves components for an application.
    /// </summary>
    internal static class ComponentResolver
    {
        private static readonly string ComponentAssemblyName = typeof(IComponent).Assembly.FullName;
        private static readonly ConcurrentDictionary<Assembly, Type[]> Cache =
            new ConcurrentDictionary<Assembly, Type[]>();

        public static IEnumerable<Type> ResolveComponents(Assembly appAssembly)
        {
            if (Cache.TryGetValue(appAssembly, out var resolvedComponents))
            {
                return resolvedComponents;
            }

            var components = DiscoverComponents(appAssembly);
            Cache.TryAdd(appAssembly, components);
            return components;
        }

        private static Type[] DiscoverComponents(Assembly assembly)
        {
            var candidateAssemblies = new List<Assembly> { assembly };

            var references = assembly.GetReferencedAssemblies();
            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                if (referencedAssembly.GetReferencedAssemblies().Any(r => string.Equals(r.FullName, ComponentAssemblyName, StringComparison.Ordinal)))
                {
                    // The referenced assembly references components. We'll use it as a candidate for component discovery
                    candidateAssemblies.Add(referencedAssembly);
                }
            }

            return candidateAssemblies.SelectMany(c => c.ExportedTypes)
                .Where(t => typeof(IComponent).IsAssignableFrom(t))
                .ToArray();
        }
    }
}
