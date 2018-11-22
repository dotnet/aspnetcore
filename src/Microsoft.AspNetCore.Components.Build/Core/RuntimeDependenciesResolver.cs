// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Microsoft.AspNetCore.Components.Build
{
    internal class RuntimeDependenciesResolver
    {
        public static void ResolveRuntimeDependencies(
            string entryPoint,
            string[] applicationDependencies,
            string[] monoBclDirectories,
            string outputFile)
        {
            var paths = ResolveRuntimeDependenciesCore(entryPoint, applicationDependencies, monoBclDirectories);
            File.WriteAllLines(outputFile, paths);
        }

        public static IEnumerable<string> ResolveRuntimeDependenciesCore(
            string entryPoint,
            string[] applicationDependencies,
            string[] monoBclDirectories)
        {
            var assembly = new AssemblyEntry(entryPoint, AssemblyDefinition.ReadAssembly(entryPoint));

            var dependencies = applicationDependencies
                .Select(a => new AssemblyEntry(a, AssemblyDefinition.ReadAssembly(a)))
                .ToArray();

            var bcl = monoBclDirectories
                .SelectMany(d => Directory.EnumerateFiles(d, "*.dll").Select(f => Path.Combine(d, f)))
                .Select(a => new AssemblyEntry(a, AssemblyDefinition.ReadAssembly(a)))
                .ToArray();

            var assemblyResolutionContext = new AssemblyResolutionContext(
                assembly,
                dependencies,
                bcl);

            assemblyResolutionContext.ResolveAssemblies();

            var paths = assemblyResolutionContext.Results.Select(r => r.Path);
            return paths.Concat(FindPdbs(paths));
        }

        private static IEnumerable<string> FindPdbs(IEnumerable<string> dllPaths)
        {
            return dllPaths
                .Select(path => Path.ChangeExtension(path, "pdb"))
                .Where(path => File.Exists(path));
        }

        public class AssemblyResolutionContext
        {
            public AssemblyResolutionContext(
                AssemblyEntry assembly,
                AssemblyEntry[] dependencies,
                AssemblyEntry[] bcl)
            {
                Assembly = assembly;
                Dependencies = dependencies;
                Bcl = bcl;
            }

            public AssemblyEntry Assembly { get; }
            public AssemblyEntry[] Dependencies { get; }
            public AssemblyEntry[] Bcl { get; }

            public IList<AssemblyEntry> Results { get; } = new List<AssemblyEntry>();

            internal void ResolveAssemblies()
            {
                var visitedAssemblies = new HashSet<string>();
                var pendingAssemblies = new Stack<AssemblyNameReference>();
                pendingAssemblies.Push(Assembly.Definition.Name);
                ResolveAssembliesCore();

                void ResolveAssembliesCore()
                {
                    while (pendingAssemblies.TryPop(out var current))
                    {
                        if (!visitedAssemblies.Contains(current.Name))
                        {
                            visitedAssemblies.Add(current.Name);

                            // Not all references will be resolvable within the Mono BCL, particularly
                            // when building for server-side Blazor as you will be running on CoreCLR
                            // and therefore may depend on System.* BCL assemblies that aren't present
                            // in Mono WebAssembly. Skipping unresolved assemblies here is equivalent
                            // to passing "--skip-unresolved true" to the Mono linker.
                            var resolved = Resolve(current);
                            if (resolved != null)
                            {
                                Results.Add(resolved);
                                var references = GetAssemblyReferences(resolved);
                                foreach (var reference in references)
                                {
                                    pendingAssemblies.Push(reference);
                                }
                            }
                        }
                    }
                }

                IEnumerable<AssemblyNameReference> GetAssemblyReferences(AssemblyEntry current) =>
                    current.Definition.Modules.SelectMany(m => m.AssemblyReferences);

                AssemblyEntry Resolve(AssemblyNameReference current)
                {
                    if (Assembly.Definition.Name.Name == current.Name)
                    {
                        return Assembly;
                    }

                    var referencedAssemblyCandidate = FindCandidate(current, Dependencies);
                    var bclAssemblyCandidate = FindCandidate(current, Bcl);

                    // Resolution logic. For right now, we will prefer the mono BCL version of a given
                    // assembly if there is a candidate assembly and an equivalent mono assembly.
                    if (bclAssemblyCandidate != null)
                    {
                        return bclAssemblyCandidate;
                    }

                    return referencedAssemblyCandidate;
                }

                AssemblyEntry FindCandidate(AssemblyNameReference current, AssemblyEntry[] candidates)
                {
                    // Do simple name match. Assume no duplicates.
                    foreach (var candidate in candidates)
                    {
                        if (current.Name == candidate.Definition.Name.Name)
                        {
                            return candidate;
                        }
                    }

                    return null;
                }
            }
        }

        [DebuggerDisplay("{ToString(),nq}")]
        public class AssemblyEntry
        {
            public AssemblyEntry(string path, AssemblyDefinition definition)
            {
                Path = path;
                Definition = definition;
            }

            public string Path { get; set; }
            public AssemblyDefinition Definition { get; set; }

            public override string ToString() => Definition.FullName;
        }
    }
}
