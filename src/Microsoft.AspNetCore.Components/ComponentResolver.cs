// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        /// <summary>
        /// Lists all the types 
        /// </summary>
        /// <param name="appAssembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> ResolveComponents(Assembly appAssembly)
        {
            var blazorAssembly = typeof(IComponent).Assembly;

            return EnumerateAssemblies(appAssembly.GetName(), blazorAssembly, new HashSet<Assembly>(new AssemblyComparer()))
                .SelectMany(a => a.ExportedTypes)
                .Where(t => typeof(IComponent).IsAssignableFrom(t));
        }

        private static IEnumerable<Assembly> EnumerateAssemblies(
            AssemblyName assemblyName,
            Assembly blazorAssembly,
            HashSet<Assembly> visited)
        {
            var assembly = Assembly.Load(assemblyName);
            if (visited.Contains(assembly))
            {
                // Avoid traversing visited assemblies.
                yield break;
            }
            visited.Add(assembly);
            var references = assembly.GetReferencedAssemblies();
            if (!references.Any(r => string.Equals(r.FullName, blazorAssembly.FullName, StringComparison.Ordinal)))
            {
                // Avoid traversing references that don't point to blazor (like netstandard2.0)
                yield break;
            }
            else
            {
                yield return assembly;

                // Look at the list of transitive dependencies for more components.
                foreach (var reference in references.SelectMany(r => EnumerateAssemblies(r, blazorAssembly, visited)))
                {
                    yield return reference;
                }
            }
        }

        private class AssemblyComparer : IEqualityComparer<Assembly>
        {
            public bool Equals(Assembly x, Assembly y)
            {
                return string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);
            }

            public int GetHashCode(Assembly obj)
            {
                return obj.FullName.GetHashCode();
            }
        }
    }
}
