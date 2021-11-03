// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace RepoTasks;

// This is temporary until we can use FrameworkReference to build our own packages
public class RemoveSharedFrameworkDependencies : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Files { get; set; }

    [Required]
    public ITaskItem[] FrameworkOnlyPackages { get; set; }

    [Required]
    public string SharedFrameworkTargetFramework { get; set; }

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
            var referencesFrameworkOnlyAssembly = false;
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
                            referencesFrameworkOnlyAssembly = true;
                            Log.LogMessage($"  Remove dependency on '{dependency.Id}'");
                            continue;
                        }

                        packages.Add(dependency);
                    }

                    updatedGroups.Add(updatedGroup);
                }

                if (referencesFrameworkOnlyAssembly)
                {
                    packageBuilder.DependencyGroups.Clear();
                    packageBuilder.DependencyGroups.AddRange(updatedGroups);

                    var updatedManifest = Manifest.Create(packageBuilder);
                    var inMemory = new MemoryStream();
                    updatedManifest.Save(inMemory);
                    inMemory.Position = 0;
                    // Hack the raw nuspec to add the <frameworkReference> dependency
                    var rawNuspec = XDocument.Load(inMemory, LoadOptions.PreserveWhitespace);
                    var ns = rawNuspec.Root.GetDefaultNamespace();
                    var metadata = rawNuspec.Root.Descendants(ns + "metadata").Single();
                    metadata.Add(
                        new XElement(ns + "frameworkReferences",
                            new XElement(ns + "group",
                                new XAttribute("targetFramework", NuGetFramework.Parse(SharedFrameworkTargetFramework).GetFrameworkString()),
                                new XElement(ns + "frameworkReference", new XAttribute("name", "Microsoft.AspNetCore.App")))));
                    stream.Position = 0;
                    stream.SetLength(0);
                    rawNuspec.Save(stream);
                    Log.LogMessage(MessageImportance.High, "Added <frameworkReference> to {0}", fileName);
                }
                else
                {
                    Log.LogMessage($"No changes made to {fileName}");
                }
            }
        }
    }
}
