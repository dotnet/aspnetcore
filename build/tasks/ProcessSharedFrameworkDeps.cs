// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Sourced from  https://github.com/dotnet/core-setup/tree/be8d8e3486b2bf598ed69d39b1629a24caaba45e/tools-local/tasks, needs to be kept in sync

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        public string FrameworkName { get; set; }

        // When generating the .deps.json file, these files are used to replace "project" libraries with "packages".
        public ITaskItem[] ResolvedPackageProjectReferences { get; set; }

        public string[] PackagesToRemove { get; set; }

        [Required]
        public string Runtime { get; set; }

        public override bool Execute()
        {
            ExecuteCore();

            return true;
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
            var expandedGraph = manager.Expand(graph, Runtime);

            // Remove the runtime entry for the project which generates the original deps.json. For example, there is no Microsoft.AspNetCore.App.dll.
            var trimmedRuntimeLibraries = RuntimeReference.RemoveSharedFxRuntimeEntry(context.RuntimeLibraries, FrameworkName);

            trimmedRuntimeLibraries = ResolveProjectsAsPackages(ResolvedPackageProjectReferences, trimmedRuntimeLibraries);

            if (PackagesToRemove != null && PackagesToRemove.Any())
            {
                trimmedRuntimeLibraries = RuntimeReference.RemoveReferences(trimmedRuntimeLibraries, PackagesToRemove);
            }

            context = new DependencyContext(
                context.Target,
                CompilationOptions.Default,
                Array.Empty<CompilationLibrary>(),
                trimmedRuntimeLibraries,
                expandedGraph
                );

            using (var depsStream = File.Create(OutputPath))
            {
                new DependencyContextWriter().Write(context, depsStream);
            }
        }

        private IEnumerable<RuntimeLibrary> ResolveProjectsAsPackages(ITaskItem[] resolvedProjects, IEnumerable<RuntimeLibrary> compilationLibraries)
        {
            var projects = resolvedProjects.ToDictionary(k => k.GetMetadata("PackageId"), k => k, StringComparer.OrdinalIgnoreCase);

            foreach (var library in compilationLibraries)
            {
                if (projects.TryGetValue(library.Name, out var project))
                {
                    Log.LogMessage("Replacing the library entry for {0}", library.Name);

                    var packagePath = project.ItemSpec;
                    var packageId = library.Name;
                    var version = library.Version;
                    string packageHash;
                    using (var sha512 = SHA512.Create())
                    {
                        packageHash = "sha512-" + sha512.ComputeHashAsBase64(File.OpenRead(packagePath), leaveStreamOpen: false);
                    }

                    yield return new RuntimeLibrary("package",
                        library.Name,
                        library.Version,
                        packageHash,
                        library.RuntimeAssemblyGroups,
                        library.NativeLibraryGroups,
                        library.ResourceAssemblies,
                        library.Dependencies,
                        serviceable: true,
                        path: $"{library.Name}/{library.Version}".ToLowerInvariant(),
                        hashPath: $"{library.Name}.{library.Version}.nupkg.sha512".ToLowerInvariant());
                }
                else
                {
                    yield return library;
                }
            }
        }
    }
}
