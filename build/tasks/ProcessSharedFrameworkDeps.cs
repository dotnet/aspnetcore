// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
using NuGet.ProjectModel;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class ProcessSharedFrameworkDeps : Task
    {
        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public string DepsFilePath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string TargetFramework { get; set; }

        [Required]
        public string FrameworkName { get; set; }

        [Required]
        public string FrameworkVersion { get; set; }

        [Required]
        public string BaseRuntimeIdentifier { get; set; }

        public override bool Execute()
        {
            ExecuteCore();

            return !Log.HasLoggedErrors;
        }

        private void ExecuteCore()
        {
            DependencyContext context;
            using (var depsStream = File.OpenRead(DepsFilePath))
            {
                context = new DependencyContextJsonReader().Read(depsStream);
            }

            var lockFile = LockFileUtilities.GetLockFile(AssetsFilePath, NullLogger.Instance);
            if (lockFile == null)
            {
                throw new ArgumentException($"Could not load a LockFile at '{AssetsFilePath}'.", nameof(AssetsFilePath));
            }

            var manager = new RuntimeGraphManager();
            var graph = manager.Collect(lockFile);
            var expandedGraph = manager.Expand(graph, BaseRuntimeIdentifier);

            var runtimeFiles = new List<RuntimeFile>();
            var nativeFiles = new List<RuntimeFile>();
            var resourceAssemblies = new List<ResourceAssembly>();

            foreach (var library in context.RuntimeLibraries)
            {
                foreach (var file in library.RuntimeAssemblyGroups.SelectMany(g => g.RuntimeFiles))
                {
                    var path = $"runtimes/{context.Target.Runtime}/lib/{TargetFramework}/{Path.GetFileName(file.Path)}";
                    runtimeFiles.Add(
                        new RuntimeFile(
                            path,
                            file.AssemblyVersion,
                            file.FileVersion));
                }

                foreach (var file in library.NativeLibraryGroups.SelectMany(g => g.RuntimeFiles))
                {
                    var path = $"runtimes/{context.Target.Runtime}/native/{Path.GetFileName(file.Path)}";
                    nativeFiles.Add(
                        new RuntimeFile(
                            path,
                            file.AssemblyVersion,
                            file.FileVersion));
                }

                resourceAssemblies.AddRange(
                    library.ResourceAssemblies);
            }

            var runtimePackageName = $"runtime.{context.Target.Runtime}.{FrameworkName}";

            var runtimeLibrary = new RuntimeLibrary("package",
                runtimePackageName,
                FrameworkVersion,
                string.Empty,
                new[] { new RuntimeAssetGroup(string.Empty, runtimeFiles) },
                new[] { new RuntimeAssetGroup(string.Empty, nativeFiles) },
                resourceAssemblies,
                Array.Empty<Dependency>(),
                hashPath: null,
                path: $"{runtimePackageName.ToLowerInvariant()}/{FrameworkVersion}",
                serviceable: true);

            var targetingPackLibrary = new RuntimeLibrary("package",
                FrameworkName,
                FrameworkVersion,
                string.Empty,
                Array.Empty<RuntimeAssetGroup>(),
                Array.Empty<RuntimeAssetGroup>(),
                resourceAssemblies,
                new[] { new Dependency(runtimeLibrary.Name, runtimeLibrary.Version) },
                hashPath: null,
                path: $"{FrameworkName.ToLowerInvariant()}/{FrameworkVersion}",
                serviceable: true);

            context = new DependencyContext(
                context.Target,
                CompilationOptions.Default,
                Array.Empty<CompilationLibrary>(),
                new[] { targetingPackLibrary, runtimeLibrary },
                expandedGraph
                );

            using (var depsStream = File.Create(OutputPath))
            {
                new DependencyContextWriter().Write(context, depsStream);
            }
        }
    }
}
