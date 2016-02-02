// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.Watcher.Core
{
    internal class Project : IProject
    {
        public Project(ProjectModel.Project runtimeProject)
        {
            ProjectFile = runtimeProject.ProjectFilePath;
            ProjectDirectory = runtimeProject.ProjectDirectory;

            Files = runtimeProject.Files.SourceFiles.Concat(
                    runtimeProject.Files.ResourceFiles.Values.Concat(
                    runtimeProject.Files.PreprocessSourceFiles.Concat(
                    runtimeProject.Files.SharedFiles))).Concat(
                    new string[] { runtimeProject.ProjectFilePath })
                .ToList();

            var projectLockJsonPath = Path.Combine(runtimeProject.ProjectDirectory, "project.lock.json");
            
            if (File.Exists(projectLockJsonPath))
            {
                var lockFile = LockFileReader.Read(projectLockJsonPath);
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
