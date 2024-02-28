// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Tools;
using Microsoft.Extensions.Tools.Internal;

internal sealed class MsBuildProjectFinder
{
    private readonly string _directory;

    public MsBuildProjectFinder(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);

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
                throw new FileNotFoundException(SecretsHelpersResources.FormatError_MultipleProjectsFound(projectPath));
            }

            if (projects.Count == 0)
            {
                throw new FileNotFoundException(SecretsHelpersResources.FormatError_NoProjectsFound(projectPath));
            }

            return projects[0];
        }

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException(SecretsHelpersResources.FormatError_ProjectPath_NotFound(projectPath));
        }

        return projectPath;
    }
}
