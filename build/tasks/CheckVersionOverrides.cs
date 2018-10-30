// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class CheckVersionOverrides : Task
    {
        [Required]
        public string DotNetPackageVersionPropsPath { get; set; }

        [Required]
        public string DependenciesFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage($"Verifying versions set in {DotNetPackageVersionPropsPath} match expected versions set in {DependenciesFile}");

            var versionOverrides = ProjectRootElement.Open(DotNetPackageVersionPropsPath);
            var dependencies = ProjectRootElement.Open(DependenciesFile);
            var pinnedVersions = dependencies.PropertyGroups
                .Where(p => string.Equals("Package Versions: Pinned", p.Label))
                .SelectMany(p => p.Properties)
                .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in versionOverrides.Properties)
            {
                if (pinnedVersions.TryGetValue(prop.Name, out var pinnedVersion))
                {
                    if (!string.Equals(pinnedVersion, prop.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError($"The imported package version props file conflicts with a pinned version variable {prop.Name}. Imported value: {prop.Value}, Pinned value: {pinnedVersion}");
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
