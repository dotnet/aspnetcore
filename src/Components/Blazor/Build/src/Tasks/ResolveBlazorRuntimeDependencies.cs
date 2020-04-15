// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class ResolveBlazorRuntimeDependencies : Task
    {
        [Required]
        public string EntryPoint { get; set; }

        [Required]
        public ITaskItem[] ApplicationDependencies { get; set; }

        [Required]
        public ITaskItem[] WebAssemblyBCLAssemblies { get; set; }

        [Output]
        public ITaskItem[] Dependencies { get; set; }

        public override bool Execute()
        {
            var paths = ResolveRuntimeDependenciesCore(EntryPoint, ApplicationDependencies.Select(c => c.ItemSpec), WebAssemblyBCLAssemblies.Select(c => c.ItemSpec));
            Dependencies = paths.Select(p => new TaskItem(p)).ToArray();

            return true;
        }

        public static IEnumerable<string> ResolveRuntimeDependenciesCore(
            string entryPoint,
            IEnumerable<string> applicationDependencies,
            IEnumerable<string> monoBclAssemblies)
        {
            var entryAssembly = new AssemblyEntry(entryPoint, GetAssemblyName(entryPoint));

            var dependencies = CreateAssemblyLookup(applicationDependencies);

            var bcl = CreateAssemblyLookup(monoBclAssemblies);

            var assemblyResolutionContext = new AssemblyResolutionContext(
                entryAssembly,
                dependencies,
                bcl);

            assemblyResolutionContext.ResolveAssemblies();

            var paths = assemblyResolutionContext.Results.Select(r => r.Path);
            return paths.Concat(FindPdbs(paths));

            static Dictionary<string, AssemblyEntry> CreateAssemblyLookup(IEnumerable<string> assemblyPaths)
            {
                var dictionary = new Dictionary<string, AssemblyEntry>(StringComparer.Ordinal);
                foreach (var path in assemblyPaths)
                {
                    var assemblyName = AssemblyName.GetAssemblyName(path).Name;
                    if (dictionary.TryGetValue(assemblyName, out var previous))
                    {
                        throw new InvalidOperationException($"Multiple assemblies found with the same assembly name '{assemblyName}':" +
                            Environment.NewLine + string.Join(Environment.NewLine, previous, path));
                    }
                    dictionary[assemblyName] = new AssemblyEntry(path, assemblyName);
                }

                return dictionary;
            }
        }

        private static string GetAssemblyName(string assemblyPath)
        {
            return AssemblyName.GetAssemblyName(assemblyPath).Name;
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
                AssemblyEntry entryAssembly,
                Dictionary<string, AssemblyEntry> dependencies,
                Dictionary<string, AssemblyEntry> bcl)
            {
                EntryAssembly = entryAssembly;
                Dependencies = dependencies;
                Bcl = bcl;
            }

            public AssemblyEntry EntryAssembly { get; }
            public Dictionary<string, AssemblyEntry> Dependencies { get; }
            public Dictionary<string, AssemblyEntry> Bcl { get; }

            public IList<AssemblyEntry> Results { get; } = new List<AssemblyEntry>();

            internal void ResolveAssemblies()
            {
                var visitedAssemblies = new HashSet<string>();
                var pendingAssemblies = new Stack<string>();
                pendingAssemblies.Push(EntryAssembly.Name);
                ResolveAssembliesCore();

                void ResolveAssembliesCore()
                {
                    while (pendingAssemblies.Count > 0)
                    {
                        var current = pendingAssemblies.Pop();
                        if (visitedAssemblies.Add(current))
                        {
                            // Not all references will be resolvable within the Mono BCL.
                            // Skipping unresolved assemblies here is equivalent to passing "--skip-unresolved true" to the Mono linker.
                            if (Resolve(current) is AssemblyEntry resolved)
                            {
                                Results.Add(resolved);
                                var references = GetAssemblyReferences(resolved.Path);
                                foreach (var reference in references)
                                {
                                    pendingAssemblies.Push(reference);
                                }
                            }
                        }
                    }
                }

                AssemblyEntry? Resolve(string assemblyName)
                {
                    if (EntryAssembly.Name == assemblyName)
                    {
                        return EntryAssembly;
                    }

                    // Resolution logic. For right now, we will prefer the mono BCL version of a given
                    // assembly if there is a candidate assembly and an equivalent mono assembly.
                    if (Bcl.TryGetValue(assemblyName, out var assembly) ||
                        Dependencies.TryGetValue(assemblyName, out assembly))
                    {
                        return assembly;
                    }

                    return null;
                }

                static IReadOnlyList<string> GetAssemblyReferences(string assemblyPath)
                {
                    try
                    {
                        using var peReader = new PEReader(File.OpenRead(assemblyPath));
                        if (!peReader.HasMetadata)
                        {
                            return Array.Empty<string>(); // not a managed assembly
                        }

                        var metadataReader = peReader.GetMetadataReader();

                        var references = new List<string>();
                        foreach (var handle in metadataReader.AssemblyReferences)
                        {
                            var reference = metadataReader.GetAssemblyReference(handle);
                            var referenceName = metadataReader.GetString(reference.Name);

                            references.Add(referenceName);
                        }

                        return references;
                    }
                    catch (BadImageFormatException)
                    {
                        // not a PE file, or invalid metadata
                    }

                    return Array.Empty<string>(); // not a managed assembly
                }
            }
        }

        [DebuggerDisplay("{ToString(),nq}")]
        public readonly struct AssemblyEntry
        {
            public AssemblyEntry(string path, string name)
            {
                Path = path;
                Name = name;
            }

            public string Path { get; }
            public string Name { get; }

            public override string ToString() => Name;
        }
    }
}
