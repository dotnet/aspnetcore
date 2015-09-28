// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime;

namespace Microsoft.Dnx.Watcher.Core
{
    internal class Project : IProject
    {
        public Project(Runtime.Project runtimeProject)
        {
            ProjectFile = runtimeProject.ProjectFilePath;
            ProjectDirectory = runtimeProject.ProjectDirectory;

            Files = runtimeProject.Files.SourceFiles.Concat(
                    runtimeProject.Files.ResourceFiles.Values.Concat(
                    runtimeProject.Files.PreprocessSourceFiles.Concat(
                    runtimeProject.Files.SharedFiles))).Concat(
                    new string[] { runtimeProject.ProjectFilePath })
                .ToList();

            var projectLockJsonPath = Path.Combine(runtimeProject.ProjectDirectory, LockFileReader.LockFileName);
            var lockFileReader = new LockFileReader();

            if (File.Exists(projectLockJsonPath))
            {
                var lockFile = lockFileReader.Read(projectLockJsonPath);
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
