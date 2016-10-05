// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// TODO if this becomes a true API instead of .Sources package, put strings into resource file

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectFinder
    {
        private readonly string _directory;

        public MsBuildProjectFinder(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(directory));
            }

            _directory = directory;
        }

        public string FindMsBuildProject(string project = null)
        {
            var projectPath = project ?? _directory;

            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(_directory, projectPath);
            }

            if (Directory.Exists(projectPath))
            {
                var projects = FindProjectFiles(projectPath).ToList();
                if (projects.Count > 1)
                {
                    throw MultipleProjectsFound(projectPath);
                }

                if (projects.Count == 0)
                {
                    throw NoProjectsFound(projectPath);
                }

                return projects[0];
            }

            if (!File.Exists(projectPath))
            {
                throw FileDoesNotExist(projectPath);
            }

            return projectPath;
        }

        protected virtual Exception FileDoesNotExist(string filePath)
            => new InvalidOperationException($"No file was found at '{filePath}'.");

        protected virtual Exception MultipleProjectsFound(string directory)
            => new InvalidOperationException($"Multiple MSBuild project files found in '{directory}'.");

        protected virtual Exception NoProjectsFound(string directory)
            => new InvalidOperationException($"Could not find a MSBuild project file in '{directory}'.");

        protected virtual IEnumerable<string> FindProjectFiles(string directory)
            => Directory.EnumerateFileSystemEntries(directory, "*.*proj", SearchOption.TopDirectoryOnly);
    }
}