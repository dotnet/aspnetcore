// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Files;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.Watcher.Core.Internal
{
    internal class Project : IProject
    {
        public Project(ProjectModel.Project runtimeProject)
        {
            ProjectFile = runtimeProject.ProjectFilePath;
            ProjectDirectory = runtimeProject.ProjectDirectory;

            var compilerOptions = runtimeProject.GetCompilerOptions(targetFramework: null, configurationName: null);

            var filesToWatch = new List<string>() { runtimeProject.ProjectFilePath };
            if (compilerOptions?.CompileInclude != null)
            {
                filesToWatch.AddRange(compilerOptions.CompileInclude.ResolveFiles());
            }
            else
            {
                filesToWatch.AddRange(runtimeProject.Files.SourceFiles);
            }

            if (compilerOptions?.EmbedInclude != null)
            {
                filesToWatch.AddRange(compilerOptions.EmbedInclude.ResolveFiles());
            }
            else
            {
                filesToWatch.AddRange(runtimeProject.Files.ResourceFiles.Values);
            }

            filesToWatch.AddRange(runtimeProject.Files.SharedFiles);
            filesToWatch.AddRange(runtimeProject.Files.PreprocessSourceFiles);

            Files = filesToWatch;

            var projectLockJsonPath = Path.Combine(runtimeProject.ProjectDirectory, "project.lock.json");

            if (File.Exists(projectLockJsonPath))
            {
                var lockFile = LockFileReader.Read(projectLockJsonPath, designTime: false);
                ProjectDependencies = lockFile.ProjectLibraries.Select(dep => GetProjectRelativeFullPath(dep.Path)).ToList();
            }
            else
            {
                ProjectDependencies = new string[0];
            }
        }

        public IEnumerable<string> ProjectDependencies { get; private set; }

        public IEnumerable<string> Files { get; private set; }

        public string ProjectFile { get; private set; }

        public string ProjectDirectory { get; private set; }

        private string GetProjectRelativeFullPath(string path)
        {
            return Path.GetFullPath(Path.Combine(ProjectDirectory, path));
        }
    }
}
