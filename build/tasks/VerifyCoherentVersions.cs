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
        [Required]
        public ITaskItem[] PackageFiles { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        public override bool Execute()
        {
            var packageLookup = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
            var dependencyMap = new Dictionary<string, List<ExternalDependency>>(StringComparer.OrdinalIgnoreCase);

            foreach (var dep in ExternalDependencies)
            {
                if (!dependencyMap.TryGetValue(dep.ItemSpec, out var list))
                {
                    dependencyMap[dep.ItemSpec] = list = new List<ExternalDependency>();
                }
                var externalDep = new ExternalDependency
                {
                    Version = dep.GetMetadata("Version"),
                    IsPrivate = bool.TryParse(dep.GetMetadata("Private"), out var isPrivate) && isPrivate,
                };
                list.Add(externalDep);
            }

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

            foreach (var packageInfo in packageLookup.Values)
            {
                Visit(packageLookup, dependencyMap, packageInfo);
            }

            Log.LogMessage(MessageImportance.High, $"Verified {PackageFiles.Length} package(s) have coherent versions");
            return !Log.HasLoggedErrors;
        }

        private class ExternalDependency
        {
            public string Version { get; set; }
            public bool IsPrivate { get; set; }
        }

        private void Visit(
            IReadOnlyDictionary<string, PackageInfo> packageLookup,
            IReadOnlyDictionary<string, List<ExternalDependency>> dependencyMap,
            PackageInfo packageInfo)
        {
            Log.LogMessage(MessageImportance.Low, $"Processing package {packageInfo.Id}");
            try
            {
                foreach (var dependencySet in packageInfo.DependencyGroups)
                {
                    foreach (var dependency in dependencySet.Packages)
                    {
                        PackageInfo dependencyPackageInfo;
                        var depVersion = dependency.VersionRange.MinVersion.ToString();
                        if (dependencyMap.TryGetValue(dependency.Id, out var externalDependencies))
                        {
                            var matchedVersion = externalDependencies.FirstOrDefault(d => depVersion.Equals(d.Version));

                            if (matchedVersion == null)
                            {
                                var versions = string.Join(" or ", externalDependencies.Select(d => d.Version));
                                Log.LogError($"Package {packageInfo.Id} has an external dependency on the wrong version of {dependency.Id}. "
                                    + $"It uses {depVersion} but only {versions} is allowed.");
                            }
                            else if (matchedVersion.IsPrivate)
                            {
                                Log.LogError($"Package {packageInfo.Id} has an external dependency on {dependency.Id}/{depVersion} which is marked as Private=true.");
                            }
                            continue;
                        }
                        else if (!packageLookup.TryGetValue(dependency.Id, out dependencyPackageInfo))
                        {
                            Log.LogError($"Package {packageInfo.Id} has an undefined external dependency on {dependency.Id}/{depVersion}");
                            continue;
                        }

                        if (dependencyPackageInfo.Version != dependency.VersionRange.MinVersion)
                        {
                            // For any dependency in the universe
                            // Add a mismatch if the min version doesn't work out
                            // (we only really care about >= minVersion)
                            Log.LogError($"{packageInfo.Id} depends on {dependency.Id} " +
                                    $"{dependency.VersionRange} ({dependencySet.TargetFramework}) when the latest build is {depVersion}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Unexpected error while attempting to verify package {packageInfo.Id}.\r\n{ex}");
            }
        }
    }
}
