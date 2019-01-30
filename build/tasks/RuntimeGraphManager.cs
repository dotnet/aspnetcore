// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Sourced from  https://github.com/dotnet/core-setup/tree/be8d8e3486b2bf598ed69d39b1629a24caaba45e/tools-local/tasks, needs to be kept in sync

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;

namespace RepoTasks.Utilities
{
    internal class RuntimeGraphManager
    {
        private const string RuntimeJsonFileName = "runtime.json";

        public RuntimeGraph Collect(LockFile lockFile)
        {
            string userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
            var fallBackFolders = lockFile.PackageFolders.Skip(1).Select(f => f.Path);
            var packageResolver = new FallbackPackagePathResolver(userPackageFolder, fallBackFolders);

            var graph = RuntimeGraph.Empty;
            foreach (var library in lockFile.Libraries)
            {
                if (string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
                {
                    var runtimeJson = library.Files.FirstOrDefault(f => f == RuntimeJsonFileName);
                    if (runtimeJson != null)
                    {
                        var libraryPath = packageResolver.GetPackageDirectory(library.Name, library.Version);
                        var runtimeJsonFullName = Path.Combine(libraryPath, runtimeJson);
                        graph = RuntimeGraph.Merge(graph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonFullName));
                    }
                }
            }
            return graph;
        }

        public IEnumerable<RuntimeFallbacks> Expand(RuntimeGraph runtimeGraph, string runtime)
        {
            var importers = FindImporters(runtimeGraph, runtime);
            foreach (var importer in importers)
            {
                // ExpandRuntime return runtime itself as first item so we are skiping it
                yield return new RuntimeFallbacks(importer, runtimeGraph.ExpandRuntime(importer).Skip(1));
            }
        }

        private IEnumerable<string> FindImporters(RuntimeGraph runtimeGraph, string runtime)
        {
            foreach (var runtimePair in runtimeGraph.Runtimes)
            {
                var expanded = runtimeGraph.ExpandRuntime(runtimePair.Key);
                if (expanded.Contains(runtime))
                {
                    yield return runtimePair.Key;
                }
            }
        }
    }
}
