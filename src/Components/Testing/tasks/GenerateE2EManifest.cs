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
/// under test. The manifest format depends on the <see cref="E2EAppMode"/>:
/// <list type="bullet">
///   <item><c>build</c> (default) — one entry per app using <c>dotnet run</c>.</item>
///   <item><c>publish</c> — one entry per app using the published executable.</item>
///   <item><c>all</c> — two entries per app: source (<c>&lt;name&gt;</c>) and published (<c>&lt;name&gt;.Published</c>).</item>
/// </list>
/// </summary>
public class GenerateE2EManifest : Task
{
    [Required]
    public ITaskItem[] AppItems { get; set; }

    [Required]
    public string ManifestPath { get; set; }

    [Required]
    public string E2EAppsOutputDir { get; set; }

    /// <summary>
    /// The E2E app mode: build, publish, or all.
    /// </summary>
    [Required]
    public string E2EAppMode { get; set; }

    /// <summary>
    /// Whether the target is running in a Publish context.
    /// When true, working directories are relative to AppContext.BaseDirectory.
    /// When false, source-mode paths are absolute (local dev).
    /// </summary>
    [Required]
    public string IsPublishing { get; set; }

    public override bool Execute()
    {
        var mode = E2EAppMode ?? "build";
        var isPublishing = IsPublishing.Equals("true", StringComparison.OrdinalIgnoreCase);
        var includeBuild = mode.Equals("build", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("all", StringComparison.OrdinalIgnoreCase);
        var includePublish = mode.Equals("publish", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("all", StringComparison.OrdinalIgnoreCase);
        var isBothMode = mode.Equals("all", StringComparison.OrdinalIgnoreCase);

        var manifest = new E2EManifestModel();

        foreach (var item in AppItems)
        {
            var name = item.GetMetadata("Filename");
            var projectPath = item.GetMetadata("FullPath");
            var publicUrl = item.GetMetadata("E2EPublicUrl") ?? "";

            if (includeBuild)
            {
                var entry = new E2EAppEntryModel
                {
                    Executable = "dotnet",
                    Arguments = "run --no-launch-profile",
                    PublicUrl = publicUrl,
                };

                if (isPublishing)
                {
                    // Source was copied to e2e-apps/<name>/ — use relative path.
                    entry.WorkingDirectory = Path.Combine("e2e-apps", name);
                }
                else
                {
                    // Local dev — point at the real project source directory.
                    entry.WorkingDirectory = Path.GetDirectoryName(projectPath) ?? "";
                }

                manifest.Apps[name] = entry;
            }

            if (includePublish)
            {
                var publishRelativeDir = isBothMode
                    ? Path.Combine("e2e-apps", "publish", name)
                    : Path.Combine("e2e-apps", name);

                var publishAbsoluteDir = isBothMode
                    ? Path.Combine(E2EAppsOutputDir, "publish", name)
                    : Path.Combine(E2EAppsOutputDir, name);

                var exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                var appHostPath = Path.Combine(publishAbsoluteDir, name + exeSuffix);
                var appDllPath = Path.Combine(publishAbsoluteDir, name + ".dll");

                var publishEntry = new E2EAppEntryModel
                {
                    PublicUrl = publicUrl,
                    WorkingDirectory = publishRelativeDir,
                };

                if (File.Exists(appHostPath))
                {
                    publishEntry.Executable = name + exeSuffix;
                    publishEntry.Arguments = "";
                }
                else if (File.Exists(appDllPath))
                {
                    publishEntry.Executable = "dotnet";
                    publishEntry.Arguments = name + ".dll";
                }
                else
                {
                    Log.LogError("Could not find published app at '{0}'. Expected '{1}' or '{2}'.",
                        publishAbsoluteDir, appHostPath, appDllPath);
                    return false;
                }

                // In 'all' mode, published entry gets a ".Published" suffix; otherwise it's the primary entry.
                var publishKey = isBothMode ? name + ".Published" : name;
                manifest.Apps[publishKey] = publishEntry;
            }
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
