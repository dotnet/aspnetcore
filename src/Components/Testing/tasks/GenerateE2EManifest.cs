// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Testing.Tasks;

/// <summary>
/// MSBuild task that generates a JSON manifest describing all E2E app projects
/// under test. The manifest includes project paths and, when published, executable details.
/// </summary>
public class GenerateE2EManifest : Task
{
    [Required]
    public ITaskItem[] AppItems { get; set; }

    [Required]
    public string ManifestPath { get; set; }

    [Required]
    public string E2EAppsOutputDir { get; set; }

    [Required]
    public string IsPublished { get; set; }

    public override bool Execute()
    {
        var usePublished = IsPublished.Equals("true", StringComparison.OrdinalIgnoreCase);
        var manifest = new E2EManifestModel();

        foreach (var item in AppItems)
        {
            var name = item.GetMetadata("Filename");
            var projectPath = item.GetMetadata("FullPath");
            var publicUrl = item.GetMetadata("E2EPublicUrl") ?? "";

            var entry = new E2EAppEntryModel
            {
                PublicUrl = publicUrl,
            };

            if (usePublished)
            {
                var publishDir = Path.Combine(E2EAppsOutputDir, name);
                var exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                var appHostPath = Path.Combine(publishDir, name + exeSuffix);
                var appDllPath = Path.Combine(publishDir, name + ".dll");

                // WorkingDirectory is relative to AppContext.BaseDirectory at runtime.
                // e2e-apps/<name> matches the publish layout from the targets.
                var relativeWorkingDir = Path.Combine("e2e-apps", name);

                if (File.Exists(appHostPath))
                {
                    entry.Executable = name + exeSuffix;
                    entry.Arguments = "";
                    entry.WorkingDirectory = relativeWorkingDir;
                }
                else if (File.Exists(appDllPath))
                {
                    entry.Executable = "dotnet";
                    entry.Arguments = name + ".dll";
                    entry.WorkingDirectory = relativeWorkingDir;
                }
                else
                {
                    Log.LogError("Could not find published app at '{0}'. Expected '{1}' or '{2}'.",
                        publishDir, appHostPath, appDllPath);
                    return false;
                }
            }
            else
            {
                // Dev mode: run from the project directory so the content root is correct.
                entry.Executable = "dotnet";
                entry.Arguments = "run --no-launch-profile";
                entry.WorkingDirectory = Path.GetDirectoryName(projectPath);
            }

            manifest.Apps[name] = entry;
        }

        var json = JsonSerializer.Serialize(manifest, E2EManifestJsonContext.Default.E2EManifestModel);

        var manifestDir = Path.GetDirectoryName(ManifestPath);
        if (!string.IsNullOrEmpty(manifestDir))
        {
            Directory.CreateDirectory(manifestDir);
        }
        File.WriteAllText(ManifestPath, json);
        Log.LogMessage(MessageImportance.High, "Generated E2E manifest: {0}", ManifestPath);
        return true;
    }

}
