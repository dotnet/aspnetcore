// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class MsBuildProjectFinder
    {
        private readonly string _directory;

        public MsBuildProjectFinder(string directory)
        {
            Ensure.NotNullOrEmpty(directory, nameof(directory));

            _directory = directory;
        }

        public string FindMsBuildProject(string project)
        {
            var projectPath = project ?? _directory;

            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(_directory, projectPath);
            }

            if (Directory.Exists(projectPath))
            {
                var projects = Directory.EnumerateFileSystemEntries(projectPath, "*.*proj", SearchOption.TopDirectoryOnly)
                    .Where(f => !".xproj".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (projects.Count > 1)
                {
                    throw new FileNotFoundException(Resources.FormatError_MultipleProjectsFound(projectPath));
                }

                if (projects.Count == 0)
                {
                    throw new FileNotFoundException(Resources.FormatError_NoProjectsFound(projectPath));
                }

                return projects[0];
            }

            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException(Resources.FormatError_ProjectPath_NotFound(projectPath));
            }

            return projectPath;
        }
    }
}