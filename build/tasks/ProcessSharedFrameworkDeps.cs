// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Sourced from  https://github.com/dotnet/core-setup/tree/be8d8e3486b2bf598ed69d39b1629a24caaba45e/tools-local/tasks, needs to be kept in sync

using System;
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
        public string FrameworkName { get; set; }

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

            LockFile lockFile = LockFileUtilities.GetLockFile(AssetsFilePath, NullLogger.Instance);
            if (lockFile == null)
            {
                throw new ArgumentException($"Could not load a LockFile at '{AssetsFilePath}'.", nameof(AssetsFilePath));
            }

            var manager = new RuntimeGraphManager();
            var graph = manager.Collect(lockFile);
            var expandedGraph = manager.Expand(graph, Runtime);

            // Remove the runtime entry for the project which generates the original deps.json. For example, there is no Microsoft.AspNetCore.App.dll.
            var trimmedRuntimeLibraries = RuntimeReference.RemoveSharedFxRuntimeEntry(context.RuntimeLibraries, FrameworkName);

            if (PackagesToRemove != null && PackagesToRemove.Any())
            {
                trimmedRuntimeLibraries = RuntimeReference.RemoveReferences(context.RuntimeLibraries, PackagesToRemove);
            }

            context = new DependencyContext(
                context.Target,
                context.CompilationOptions,
                context.CompileLibraries,
                trimmedRuntimeLibraries,
                expandedGraph
                );

            using (var depsStream = File.Create(OutputPath))
            {
                new DependencyContextWriter().Write(context, depsStream);
            }
        }
    }
}