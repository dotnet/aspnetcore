// Copyright (c) .NET Foundation. All rights reserved.
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

namespace RepoTasks
{
    // This is temporary until we can use FrameworkReference to build our own packages
    public class RemoveSharedFrameworkDependencies : Task
    {
        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public ITaskItem[] FrameworkOnlyPackages { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("NuGet version = " + typeof(PackageArchiveReader).Assembly.GetName().Version);
            var dependencyToRemove = new HashSet<string>(FrameworkOnlyPackages.Select(p => p.ItemSpec), StringComparer.OrdinalIgnoreCase);

            foreach (var file in Files)
            {
                FilterDependencies(file.ItemSpec, dependencyToRemove);
            }
            return !Log.HasLoggedErrors;
        }

        private void FilterDependencies(string targetPath, ISet<string> dependencyToRemove)
        {
            var fileName = Path.GetFileName(targetPath);
            Log.LogMessage($"Updating {fileName}");

            using (var fileStream = File.Open(targetPath, FileMode.Open))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Update))
            using (var packageReader = new PackageArchiveReader(fileStream, leaveStreamOpen: true))
            {
                var dirty = false;
                var nuspecFile = packageReader.GetNuspecFile();
                using (var stream = package.OpenFile(nuspecFile))
                {
                    var reader = Manifest.ReadFrom(stream, validateSchema: true);
                    stream.Position = 0;
                    var packageBuilder = new PackageBuilder(stream, basePath: null);
                    var updatedGroups = new List<PackageDependencyGroup>();

                    foreach (var group in packageBuilder.DependencyGroups)
                    {
                        var packages = new List<PackageDependency>();
                        var updatedGroup = new PackageDependencyGroup(group.TargetFramework, packages);
                        foreach (var dependency in group.Packages)
                        {
                            if (dependencyToRemove.Contains(dependency.Id))
                            {
                                dirty = true;
                                Log.LogMessage($"  Remove dependency on '{dependency.Id}'");
                                continue;
                            }

                            packages.Add(dependency);
                        }

                        updatedGroups.Add(updatedGroup);
                    }

                    if (dirty)
                    {
                        packageBuilder.DependencyGroups.Clear();
                        packageBuilder.DependencyGroups.AddRange(updatedGroups);

                        var updatedManifest = Manifest.Create(packageBuilder);
                        stream.Position = 0;
                        stream.SetLength(0);
                        updatedManifest.Save(stream);
                    }
                    else
                    {
                        Log.LogMessage($"No changes made to {fileName}");
                    }
                }
            }
        }
    }
}
