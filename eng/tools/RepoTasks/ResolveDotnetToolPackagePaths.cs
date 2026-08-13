// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks;

public class ResolveDotnetToolPackagePaths : Microsoft.Build.Utilities.Task
{
    [Required]
    public string ToolManifestPath { get; set; }

    [Required]
    public string NuGetPackageRoot { get; set; }

    [Required]
    public ITaskItem[] PackageIds { get; set; }

    [Output]
    public ITaskItem[] ToolPackages { get; private set; }

    public override bool Execute()
    {
        try
        {
            using var stream = File.OpenRead(ToolManifestPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("tools", out var tools))
            {
                Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' does not contain a 'tools' object.");
                return false;
            }

            var toolPackages = new List<ITaskItem>();
            foreach (var package in PackageIds)
            {
                var packageId = package.ItemSpec;
                if (!tools.TryGetProperty(packageId, out var tool) ||
                    !tool.TryGetProperty("version", out var versionElement))
                {
                    Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' does not contain a version for '{packageId}'.");
                    continue;
                }

                var version = versionElement.GetString();
                if (string.IsNullOrEmpty(version))
                {
                    Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' contains an empty version for '{packageId}'.");
                    continue;
                }

                var normalizedPackageId = packageId.ToLowerInvariant();
                var packagePath = Path.Combine(
                    NuGetPackageRoot,
                    normalizedPackageId,
                    version,
                    $"{normalizedPackageId}.{version}.nupkg");
                var toolPackage = new TaskItem(packagePath);
                toolPackage.SetMetadata("PackageId", packageId);
                toolPackage.SetMetadata("Version", version);
                toolPackages.Add(toolPackage);
            }

            ToolPackages = toolPackages.ToArray();
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception);
        }

        return !Log.HasLoggedErrors;
    }
}
