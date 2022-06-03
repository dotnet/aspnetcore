// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

internal sealed class MsBuildProjectFinder
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
                throw new FileNotFoundException($"Multiple MSBuild project files found in '{projectPath}'. Specify which to use with the --project option.");
            }

            if (projects.Count == 0)
            {
                throw new FileNotFoundException($"Could not find a MSBuild project file in '{projectPath}'. Specify which project to use with the --project option.");
            }

            return projects[0];
        }

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"The project file '{projectPath}' does not exist.");
        }

        return projectPath;
    }
}
