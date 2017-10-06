// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace RepoTasks
{
    public class CreateTimestampFreePackages : Task
    {
        /// <summary>
        /// The packages to produce time stamp free versions for.
        /// </summary>
        [Required]
        public ITaskItem[] PackagesWithTimestamp { get; set; }

        /// <summary>
        /// The directory to output time stamp free packages to.
        /// </summary>
        [Required]
        public string OutputDirectory { get; set; }

        [Output]
        public ITaskItem[] PackagesWithoutTimestamp { get; set; }

        public override bool Execute()
        {
            var packageIds = GetKnownPackageIds();

            var output = new List<ITaskItem>();
            foreach (var package in PackagesWithTimestamp)
            {
                var packageWithoutTimestampPath = CreateTimeStampFreePackage(packageIds, package.ItemSpec);
                Log.LogMessage($"Creating timestamp free version at {packageWithoutTimestampPath} from {package.ItemSpec}.");

                output.Add(new TaskItem(packageWithoutTimestampPath));
            }

            PackagesWithoutTimestamp = output.ToArray();
            return true;
        }

        private HashSet<string> GetKnownPackageIds()
        {
            var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in PackagesWithTimestamp)
            {
                using (var reader = new PackageArchiveReader(package.ItemSpec))
                {
                    var packageId = reader.GetIdentity();
                    packageIds.Add(packageId.Id);
                }
            }

            return packageIds;
        }

        private string CreateTimeStampFreePackage(HashSet<string> knownPackageIds, string packagePath)
        {
            var targetPath = Path.Combine(OutputDirectory, Path.GetFileName(packagePath));
            File.Copy(packagePath, targetPath, overwrite: true);

            PackageIdentity updatedIdentity;

            using (var fileStream = File.Open(targetPath, FileMode.Open))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Update))
            {
                var packageReader = new PackageArchiveReader(packagePath);
                var identity = packageReader.GetIdentity();
                var updatedVersion = new NuGetVersion(StripBuildVersion(identity.Version));

                updatedIdentity = new PackageIdentity(identity.Id, updatedVersion);

                var nuspecFile = packageReader.GetNuspecFile();
                using (var stream = package.OpenFile(nuspecFile))
                {
                    var reader = Manifest.ReadFrom(stream, validateSchema: true);
                    stream.Position = 0;
                    var packageBuilder = new PackageBuilder(stream, basePath: null);
                    packageBuilder.Version = updatedVersion;
                    var updatedGroups = new List<PackageDependencyGroup>();

                    foreach (var group in packageBuilder.DependencyGroups)
                    {
                        var packages = new List<PackageDependency>();
                        var updatedGroup = new PackageDependencyGroup(group.TargetFramework, packages);
                        foreach (var dependency in group.Packages)
                        {
                            PackageDependency dependencyToAdd;
                            if (knownPackageIds.Contains(dependency.Id))
                            {
                                dependencyToAdd = UpdateDependency(identity, dependency);
                            }
                            else
                            {
                                dependencyToAdd = dependency;
                            }

                            packages.Add(dependencyToAdd);
                        }

                        updatedGroups.Add(updatedGroup);
                    }

                    packageBuilder.DependencyGroups.Clear();
                    packageBuilder.DependencyGroups.AddRange(updatedGroups);

                    var updatedManifest = Manifest.Create(packageBuilder);
                    stream.Position = 0;
                    stream.SetLength(0);
                    updatedManifest.Save(stream);
                }

                // Metapackage needs to update manifest files
                if (identity.Id.Equals("Microsoft.AspNetCore.All", StringComparison.OrdinalIgnoreCase))
                {
                    var entry = package.GetEntry($"build/aspnetcore-store-{identity.Version}.xml");
                    var releaseLabel = identity.Version.Release;
                    var updatedReleaseLabel = Utilities.GetNoTimestampReleaseLabel(releaseLabel);
                    using (var entryStream = new StreamReader(entry.Open()))
                    using (var writer = new StreamWriter(package.CreateEntry($"build/aspnetcore-store-{updatedIdentity.Version.ToNormalizedString()}.xml").Open()))
                    {
                            writer.Write(entryStream.ReadToEnd().Replace($"-{releaseLabel}", string.IsNullOrEmpty(updatedReleaseLabel) ? updatedReleaseLabel : $"-{updatedReleaseLabel}"));
                    }
                    entry.Delete();
                }
            }

            var updatedTargetPath = Path.Combine(OutputDirectory, updatedIdentity.Id + '.' + updatedIdentity.Version.ToNormalizedString() + ".nupkg");
            if (File.Exists(updatedTargetPath))
            {
                File.Delete(updatedTargetPath);
            }
            File.Move(targetPath, updatedTargetPath);
            return updatedTargetPath;
        }

        private static PackageDependency UpdateDependency(PackageIdentity id, PackageDependency dependency)
        {
            if (!dependency.VersionRange.HasLowerBound)
            {
                throw new Exception($"Dependency {dependency} for {id} does not have a lower bound.");
            }

            if (dependency.VersionRange.HasUpperBound)
            {
                throw new Exception($"Dependency {dependency} for {id} has an upper bound.");
            }

            var minVersion = StripBuildVersion(dependency.VersionRange.MinVersion);
            return new PackageDependency(dependency.Id, new VersionRange(minVersion));
        }

        private static NuGetVersion StripBuildVersion(NuGetVersion version)
        {
            return new NuGetVersion(version.Version, Utilities.GetNoTimestampReleaseLabel(version.Release));
        }
    }
}
