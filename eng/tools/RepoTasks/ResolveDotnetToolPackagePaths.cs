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

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' must contain a JSON object.");
                return false;
            }

            if (!document.RootElement.TryGetProperty("tools", out var tools))
            {
                Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' does not contain a 'tools' object.");
                return false;
            }

            if (tools.ValueKind != JsonValueKind.Object)
            {
                Log.LogError($"The 'tools' value in dotnet tool manifest '{ToolManifestPath}' must be a JSON object.");
                return false;
            }

            var toolsByPackageId = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in tools.EnumerateObject())
            {
                if (!toolsByPackageId.TryAdd(tool.Name, tool.Value))
                {
                    Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' contains duplicate package ID '{tool.Name}' under case-insensitive comparison.");
                }
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            var toolPackages = new List<ITaskItem>();
            foreach (var package in PackageIds)
            {
                var packageId = package.ItemSpec;
                if (!toolsByPackageId.TryGetValue(packageId, out var tool))
                {
                    Log.LogError($"The dotnet tool manifest '{ToolManifestPath}' does not contain tool '{packageId}'.");
                    continue;
                }

                if (tool.ValueKind != JsonValueKind.Object)
                {
                    Log.LogError($"Tool '{packageId}' in dotnet tool manifest '{ToolManifestPath}' must be a JSON object.");
                    continue;
                }

                if (!tool.TryGetProperty("version", out var versionElement))
                {
                    Log.LogError($"Tool '{packageId}' in dotnet tool manifest '{ToolManifestPath}' does not contain a version.");
                    continue;
                }

                if (versionElement.ValueKind != JsonValueKind.String)
                {
                    Log.LogError($"The version for tool '{packageId}' in dotnet tool manifest '{ToolManifestPath}' must be a string.");
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
