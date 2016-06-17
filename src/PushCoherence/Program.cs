// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PushCoherence
{
    class Program
    {
        public static void Main(string[] args)
        {
            var nugetFeed = Environment.GetEnvironmentVariable("NUGET_FEED");
            var dropRoot = Environment.GetEnvironmentVariable("DROP_ROOT");
            var apiKey = Environment.GetEnvironmentVariable("APIKEY");

            if (string.IsNullOrEmpty(nugetFeed))
            {
                throw new Exception("NUGET_FEED not specified");
            }

            if (string.IsNullOrEmpty(dropRoot))
            {
                throw new Exception("DROP_ROOT not specified");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("APIKEY not specified");
            }

            var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
            var packagesDir = Path.Combine(artifactsDir, "Signed", "Packages");

            var packagesToPush = new[]
            {
                packagesDir,
                Path.Combine(artifactsDir, "coherence", "noship"),
                Path.Combine(artifactsDir, "coherence", "ext"),
            }.SelectMany(d => Directory.EnumerateFiles(d, "*.nupkg"));
            Console.WriteLine("Pushing packages from {0} to feed {1}", packagesDir, nugetFeed);

            PackagePublisher.PublishToFeedAsync(packagesToPush, nugetFeed, apiKey).Wait();

            var nonTimeStampedDir = Path.Combine(artifactsDir, "Signed", "Packages-NoTimeStamp");
            Directory.CreateDirectory(nonTimeStampedDir);
            var packages = Directory.GetFiles(packagesDir, "*.nupkg");
            var packageIds = GetPackageIds(packages);

            foreach (var file in packages)
            {
                CreateTimeStampFreePackage(packageIds, nonTimeStampedDir, file);
            }
        }

        public static PackageIdentity GetPackageIdentity(string packagePath)
        {
            using (var reader = new PackageArchiveReader(packagePath))
            {
                return reader.GetIdentity();
            }
        }

        private static HashSet<string> GetPackageIds(string[] packagePaths)
        {
            var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var packagePath in packagePaths)
            {
                packageIds.Add(GetPackageIdentity(packagePath).Id);
            }

            return packageIds;
        }

        private static void CreateTimeStampFreePackage(HashSet<string> aspnetPackageIds, string outDir, string packagePath)
        {
            var targetPath = Path.Combine(outDir, Path.GetFileName(packagePath));
            PackageIdentity updatedIdentity;
            File.Copy(packagePath, targetPath, overwrite: true);

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
                            if (aspnetPackageIds.Contains(dependency.Id))
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
            }

            var updatedTargetPath = Path.Combine(outDir, updatedIdentity.Id + '.' + updatedIdentity.Version.ToNormalizedString() + ".nupkg");
            File.Move(targetPath, updatedTargetPath);
            Console.WriteLine("Creating timestamp free version at {0}", updatedTargetPath);
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
            var releaseLabel = version.Release;
            if (releaseLabel.StartsWith("rtm-", StringComparison.OrdinalIgnoreCase))
            {
                // E.g. change version 2.5.0-rtm-123123 to 2.5.0.
                releaseLabel = string.Empty;
            }
            else
            {
                var timeStampFreeVersion = Environment.GetEnvironmentVariable("TIMESTAMP_FREE_VERSION");
                if (string.IsNullOrEmpty(timeStampFreeVersion))
                {
                    timeStampFreeVersion = "final";
                }

                if (!timeStampFreeVersion.StartsWith("-"))
                {
                    timeStampFreeVersion = "-" + timeStampFreeVersion;
                }

                // E.g. change version 2.5.0-rc2-123123 to 2.5.0-rc2-final.
                var index = releaseLabel.LastIndexOf('-');
                if (index != -1)
                {
                    releaseLabel = releaseLabel.Substring(0, index) + timeStampFreeVersion;
                }
            }

            return new NuGetVersion(version.Version, releaseLabel);
        }
    }
}
