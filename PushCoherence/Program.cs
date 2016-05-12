// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var nugetExe = Environment.GetEnvironmentVariable("PUSH_NUGET_EXE");

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

            if (string.IsNullOrEmpty(nugetExe))
            {
                throw new Exception("PUSH_NUGET_EXE not specified");
            }

            var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
            var packagesDir = Path.Combine(artifactsDir, "Signed", "Packages");

            var packagesToPush = new[]
            {
                packagesDir,
                Path.Combine(artifactsDir, "coherence", "noship"),
                Path.Combine(artifactsDir, "coherence", "ext"),
                Path.Combine(artifactsDir, "coherence", "ship-ext"),
            }.SelectMany(d => Directory.EnumerateFiles(d, "*.nupkg"));
            Console.WriteLine("Pushing packages from {0} to feed {1}", packagesDir, nugetFeed);

            Parallel.ForEach(packagesToPush, new ParallelOptions { MaxDegreeOfParallelism = 5 }, package =>
            {
                Retry(() => PushPackage(nugetFeed, apiKey, nugetExe, package));
            });

            var nonTimeStampedDir = Path.Combine(artifactsDir, "Signed", "Packages-NoTimeStamp");
            Directory.CreateDirectory(nonTimeStampedDir);
            var packages = Directory.GetFiles(packagesDir, "*.nupkg");
            var packageIds = GetPackageIds(packages);

            foreach (var file in packages)
            {
                CreateTimeStampFreePackage(packageIds, nonTimeStampedDir, file);
            }
        }

        private static HashSet<string> GetPackageIds(string[] packagePaths)
        {
            var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var packagePath in packagePaths)
            {
                using (var reader = new PackageArchiveReader(packagePath))
                {
                    packageIds.Add(reader.GetIdentity().Id);
                }
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
                var index = releaseLabel.LastIndexOf('-');
                if (index != -1)
                {
                    releaseLabel = releaseLabel.Substring(0, index);
                }
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

        private static void Retry(Action pushPackage)
        {
            int attempts = 5;
            while (attempts-- > 0)
            {
                try
                {
                    pushPackage();
                    break;
                }
                catch (Exception)
                {
                    if (attempts == 1)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    Console.WriteLine("Retry ({0})", attempts);
                }
            }
        }

        private static void PushPackage(string nugetFeed, string apiKey, string nugetExe, string packagePath)
        {
            var nugetExeArgs = string.Format(
                CultureInfo.InvariantCulture, "push -Source {0} -ApiKey {1} {2}",
                nugetFeed,
                apiKey,
                packagePath);
            var packageName = Path.GetFileNameWithoutExtension(packagePath);
            Console.WriteLine("Pushing package {0}", packageName);
            var psi = new ProcessStartInfo(nugetExe, nugetExeArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    var message = string.Format("Pushing package {0} failed. Exit code from nuget.exe: {1}", packageName, p.ExitCode);
                    throw new Exception(message);
                }
            }
            Console.WriteLine("Pushed package {0}", packageName);
        }
    }
}
