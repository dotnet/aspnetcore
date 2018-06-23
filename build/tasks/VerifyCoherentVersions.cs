// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using NuGet.Packaging;
using RepoTasks.ProjectModel;

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
            if (PackageFiles.Length == 0)
            {
                Log.LogError("Did not find any packages to verify for version coherence");
                return false;
            }

            var packageLookup = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
            var dependencyMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var dep in ExternalDependencies)
            {
                if (!dependencyMap.TryGetValue(dep.ItemSpec, out var list))
                {
                    dependencyMap[dep.ItemSpec] = list = new List<string>();
                }

                list.Add(dep.GetMetadata("Version"));
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
                    Log.LogError("Multiple copies of the following package were found: " +
                        Environment.NewLine +
                        existingPackage +
                        Environment.NewLine +
                        package);
                    continue;
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

        private void Visit(
            IReadOnlyDictionary<string, PackageInfo> packageLookup,
            IReadOnlyDictionary<string, List<string>> dependencyMap,
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
                        var minVersion = dependency.VersionRange.MinVersion;
                        var minVersionString = minVersion.ToString();
                        if (dependencyMap.TryGetValue(dependency.Id, out var externalDepVersions))
                        {
                            var matchedVersion = externalDepVersions.FirstOrDefault(d => minVersionString.Equals(d));

                            // If dependency does not match an external dependency version, check if matching version
                            // will be built in Universe. That's fine in benchmark apps for example.
                            var universePackageVersion = string.Empty;
                            if (matchedVersion == null &&
                                packageLookup.TryGetValue(dependency.Id, out var universePackageInfo))
                            {
                                if (universePackageInfo.Version == minVersion)
                                {
                                    continue;
                                }

                                // Include Universe version in following error message.
                                universePackageVersion = universePackageInfo.Version.ToString();
                            }

                            if (matchedVersion == null)
                            {
                                var versions = string.Join(" or ", externalDepVersions);
                                if (!string.IsNullOrEmpty(universePackageVersion))
                                {
                                    versions += $" or {universePackageVersion}";
                                }

                                Log.LogError($"Package {packageInfo.Id} has an external dependency on the wrong version of {dependency.Id}. "
                                    + $"It uses {minVersionString} but only {versions} is allowed.");
                            }

                            continue;
                        }
                        else if (!packageLookup.TryGetValue(dependency.Id, out dependencyPackageInfo))
                        {
                            Log.LogError($"Package {packageInfo.Id} has an undefined external dependency on {dependency.Id}/{minVersionString}. " +
                                "If the package is built in aspnet/Universe, make sure it is also marked as 'ship'. " +
                                "If it is an external dependency, add it as a new ExternalDependency.");
                            continue;
                        }

                        if (dependencyPackageInfo.Version != minVersion)
                        {
                            // For any dependency in the universe
                            // Add a mismatch if the min version doesn't work out
                            // (we only really care about >= minVersion)
                            Log.LogError($"{packageInfo.Id} depends on {dependency.Id} " +
                                    $"{dependency.VersionRange} ({dependencySet.TargetFramework}) when the latest build is {dependencyPackageInfo.Version}.");
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
