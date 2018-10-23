// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Sourced from  https://github.com/dotnet/core-setup/tree/be8d8e3486b2bf598ed69d39b1629a24caaba45e/tools-local/tasks, needs to be kept in sync

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace RepoTasks.Utilities
{
    internal class RuntimeReference
    {
        public static IEnumerable<RuntimeLibrary> RemoveSharedFxRuntimeEntry(IEnumerable<RuntimeLibrary> runtimeLibraries, string fxName)
        {
            foreach (var runtimeLib in runtimeLibraries)
            {
                if (string.Equals(runtimeLib.Name, fxName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new RuntimeLibrary(runtimeLib.Type,
                                                    runtimeLib.Name,
                                                    runtimeLib.Version,
                                                    runtimeLib.Hash,
                                                    Array.Empty<RuntimeAssetGroup>(), // runtimeLib.RuntimeAssemblyGroups,
                                                    runtimeLib.NativeLibraryGroups,
                                                    runtimeLib.ResourceAssemblies,
                                                    runtimeLib.Dependencies,
                                                    runtimeLib.Serviceable);
                }
                else
                {
                    yield return runtimeLib;
                }
            }
        }

        public static List<RuntimeLibrary> RemoveReferences(IReadOnlyList<RuntimeLibrary> runtimeLibraries, IEnumerable<string> packages)
        {
            List<RuntimeLibrary> result = new List<RuntimeLibrary>();

            foreach (var runtimeLib in runtimeLibraries)
            {
                if (string.IsNullOrEmpty(packages.FirstOrDefault(elem => runtimeLib.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                {
                    List<Dependency> toRemoveDependecy = new List<Dependency>();
                    foreach (var dependency in runtimeLib.Dependencies)
                    {
                        if (!string.IsNullOrEmpty(packages.FirstOrDefault(elem => dependency.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                        {
                            toRemoveDependecy.Add(dependency);
                        }
                    }

                    if (toRemoveDependecy.Count > 0)
                    {
                        List<Dependency> modifiedDependencies = new List<Dependency>();
                        foreach (var dependency in runtimeLib.Dependencies)
                        {
                            if (!toRemoveDependecy.Contains(dependency))
                            {
                                modifiedDependencies.Add(dependency);
                            }
                        }


                        result.Add(new RuntimeLibrary(runtimeLib.Type,
                                                      runtimeLib.Name,
                                                      runtimeLib.Version,
                                                      runtimeLib.Hash,
                                                      runtimeLib.RuntimeAssemblyGroups,
                                                      runtimeLib.NativeLibraryGroups,
                                                      runtimeLib.ResourceAssemblies,
                                                      modifiedDependencies,
                                                      runtimeLib.Serviceable));

                    }
                    else if (string.IsNullOrEmpty(packages.FirstOrDefault(elem => runtimeLib.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                    {
                        result.Add(runtimeLib);
                    }
                }
            }
            return result;
        }
    }
}