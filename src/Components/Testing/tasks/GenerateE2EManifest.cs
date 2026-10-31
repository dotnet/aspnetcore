// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Testing.Tasks;

/// <summary>
/// MSBuild task that generates a JSON manifest describing all E2E app projects
/// under test. Each app can override <see cref="E2EAppMode"/> through item metadata.
/// The manifest format depends on the effective mode:
/// <list type="bullet">
///   <item><c>build</c> (default) — one entry per app using <c>dotnet run</c>.</item>
///   <item><c>publish</c> — one entry per app using the published executable.</item>
///   <item><c>all</c> — two entries per app: source (<c>&lt;name&gt;</c>) and published (<c>publish/&lt;name&gt;</c>).</item>
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
    /// Relative directory name for E2E apps (e.g. "e2e-apps").
    /// Used in manifest paths — never hardcoded.
    /// </summary>
    [Required]
    public string E2EAppsRelativeDir { get; set; }

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
        var isPublishing = IsPublishing.Equals("true", StringComparison.OrdinalIgnoreCase);
        var manifest = new E2EManifestModel();

        foreach (var item in AppItems)
        {
            var mode = item.GetMetadata("E2EAppMode");
            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = E2EAppMode ?? "build";
            }

            var includeBuild = mode.Equals("build", StringComparison.OrdinalIgnoreCase)
                || mode.Equals("all", StringComparison.OrdinalIgnoreCase);
            var includePublish = mode.Equals("publish", StringComparison.OrdinalIgnoreCase)
                || mode.Equals("all", StringComparison.OrdinalIgnoreCase);
            var isBothMode = mode.Equals("all", StringComparison.OrdinalIgnoreCase);
            var name = item.GetMetadata("Filename");
            var projectPath = item.GetMetadata("FullPath");
            var publicUrl = item.GetMetadata("E2EPublicUrl") ?? "";
            var isCompiledHarness = item.GetMetadata("E2ENativeAot").Equals("true", StringComparison.OrdinalIgnoreCase);
            var runtimeIdentifier = item.GetMetadata("E2ERuntimeIdentifier");

            if (isCompiledHarness && !mode.Equals("publish", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError(
                    "E2E app '{0}' enables E2ENativeAot, which requires E2EAppMode=publish. The current mode is '{1}'.",
                    name,
                    mode);
                return false;
            }

            if (isCompiledHarness && string.IsNullOrWhiteSpace(runtimeIdentifier))
            {
                Log.LogError(
                    "E2E app '{0}' enables E2ENativeAot, which requires non-empty E2ERuntimeIdentifier metadata.",
                    name);
                return false;
            }

            if (includeBuild)
            {
                var entry = new E2EAppEntryModel
                {
                    Executable = "dotnet",
                    Arguments = isPublishing
                        ? "run --no-launch-profile"
                        : "run --no-build --no-restore --no-launch-profile",
                    PublicUrl = publicUrl,
                    HarnessMode = "startupHook",
                };

                if (isPublishing)
                {
                    // Source was copied to <relDir>/<name>/ — use relative path.
                    entry.WorkingDirectory = Path.Combine(E2EAppsRelativeDir, name);
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
                // In 'all' mode, published output lives under publish/<name> within the apps dir.
                var subPath = isBothMode
                    ? Path.Combine("publish", name)
                    : name;
                var relativeDir = Path.Combine(E2EAppsRelativeDir, subPath);
                var absoluteDir = Path.Combine(E2EAppsOutputDir, subPath);

                var exeSuffix = isCompiledHarness
                    ? (runtimeIdentifier.StartsWith("win-", StringComparison.OrdinalIgnoreCase) ? ".exe" : "")
                    : (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
                var appHostPath = Path.Combine(absoluteDir, name + exeSuffix);
                var appDllPath = Path.Combine(absoluteDir, name + ".dll");

                var publishEntry = new E2EAppEntryModel
                {
                    PublicUrl = publicUrl,
                    WorkingDirectory = relativeDir,
                    HarnessMode = isCompiledHarness ? "compiled" : "startupHook",
                };

                if (File.Exists(appHostPath))
                {
                    publishEntry.Executable = name + exeSuffix;
                    publishEntry.Arguments = "";
                }
                else if (!isCompiledHarness && File.Exists(appDllPath))
                {
                    publishEntry.Executable = "dotnet";
                    publishEntry.Arguments = name + ".dll";
                }
                else if (isCompiledHarness)
                {
                    Log.LogError(
                        "Could not find the Native AOT executable for compiled E2E app '{0}' at '{1}'. " +
                        "Compiled harness entries never fall back to 'dotnet {0}.dll'.",
                        name,
                        appHostPath);
                    return false;
                }
                else
                {
                    Log.LogError("Could not find published app at '{0}'. Expected '{1}' or '{2}'.",
                        absoluteDir, appHostPath, appDllPath);
                    return false;
                }

                // In 'all' mode, published entry uses "publish/<name>" key; otherwise it's the primary entry.
                var publishKey = isBothMode ? "publish/" + name : name;
                manifest.Apps[publishKey] = publishEntry;
            }
        }

        var manifestDir = Path.GetDirectoryName(ManifestPath);
        if (!string.IsNullOrEmpty(manifestDir))
        {
            Directory.CreateDirectory(manifestDir);
        }

        using var fileStream = File.Create(ManifestPath);
        var serializer = new DataContractJsonSerializer(typeof(E2EManifestModel), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
        });
        using var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, ownsStream: false, indent: true);
        serializer.WriteObject(writer, manifest);

        Log.LogMessage(MessageImportance.High, "Generated E2E manifest: {0}", ManifestPath);
        return true;
    }

}
