// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class VerifyCoherentVersions : Microsoft.Build.Utilities.Task
    {
        public ITaskItem[] PackageFiles { get; set; }

        public override bool Execute()
        {
            var packageLookup = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in PackageFiles)
            {
                PackageInfo package;
                using (var reader = new PackageArchiveReader(file.ItemSpec))
                {
                    var identity = reader.GetIdentity();
                    var metadata = new PackageBuilder(reader.GetNuspec(), basePath: null);
                    package = new PackageInfo(identity.Id, identity.Version,
                        source: Path.GetDirectoryName(file.ItemSpec),
                        dependencyGroups: metadata.DependencyGroups.ToArray());
                }

                if (packageLookup.TryGetValue(package.Id, out var existingPackage))
                {
                    throw new Exception("Multiple copies of the following package were found: " +
                        Environment.NewLine +
                        existingPackage +
                        Environment.NewLine +
                        package);
                }

                packageLookup[package.Id] = package;
            }

            var dependencyIssues = new List<DependencyWithIssue>();
            foreach (var packageInfo in packageLookup.Values)
            {
                dependencyIssues.AddRange(Visit(packageLookup, packageInfo));
            }

            var success = true;
            foreach (var mismatch in dependencyIssues)
            {
                var message = $"{mismatch.Info.Id} depends on {mismatch.Dependency.Id} " +
                    $"v{mismatch.Dependency.VersionRange} ({mismatch.TargetFramework}) when the latest build is v{mismatch.Info.Version}.";
                Log.LogError(message);
                success = false;
            }

            Log.LogMessage(MessageImportance.High, $"Verified {PackageFiles.Length} package(s) have coherent versions");
            return success;
        }

        private class DependencyWithIssue
        {
            public PackageDependency Dependency { get; set; }
            public PackageInfo Info { get; set; }
            public NuGetFramework TargetFramework { get; set; }
        }

        private IEnumerable<DependencyWithIssue> Visit(IDictionary<string, PackageInfo> packageLookup, PackageInfo packageInfo)
        {
            Log.LogMessage(MessageImportance.Low, $"Processing package {packageInfo.Id}");
            try
            {
                var issues = new List<DependencyWithIssue>();
                foreach (var dependencySet in packageInfo.DependencyGroups)
                {
                    // If the package doens't target any frameworks, just accept it
                    if (dependencySet.TargetFramework == null)
                    {
                        continue;
                    }

                    foreach (var dependency in dependencySet.Packages)
                    {
                        if (!packageLookup.TryGetValue(dependency.Id, out var dependencyPackageInfo))
                        {
                            // External dependency
                            continue;
                        }

                        if (dependencyPackageInfo.Version != dependency.VersionRange.MinVersion)
                        {
                            // For any dependency in the universe
                            // Add a mismatch if the min version doesn't work out
                            // (we only really care about >= minVersion)
                            issues.Add(new DependencyWithIssue
                            {
                                Dependency = dependency,
                                TargetFramework = dependencySet.TargetFramework,
                                Info = dependencyPackageInfo
                            });
                        }
                    }
                }
                return issues;
            }
            catch
            {
                Log.LogError($"Unable to verify package {packageInfo.Id}");
                throw;
            }
        }
    }
}
