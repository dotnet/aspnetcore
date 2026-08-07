// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;

namespace RepoTasks;

/// <summary>
/// Checks whether a NuGet assets file contains a target for a target framework
/// and runtime identifier.
/// </summary>
public sealed class CheckProjectAssetsRuntimeTarget : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the NuGet project assets file to inspect.
    /// </summary>
    [Required]
    public string ProjectAssetsFile { get; set; }

    /// <summary>
    /// Gets or sets the target framework to find.
    /// </summary>
    [Required]
    public string TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier to find.
    /// </summary>
    [Required]
    public string RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets whether the requested runtime target exists.
    /// </summary>
    [Output]
    public bool RuntimeTargetExists { get; private set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        try
        {
            using var stream = File.OpenRead(ProjectAssetsFile);
            using var document = JsonDocument.Parse(stream);
            var targetName = $"{TargetFramework}/{RuntimeIdentifier}";

            RuntimeTargetExists =
                document.RootElement.TryGetProperty("targets", out var targets) &&
                targets.TryGetProperty(targetName, out _);

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            Log.LogError($"Unable to inspect NuGet assets file '{ProjectAssetsFile}': {exception.Message}");
            return false;
        }
    }
}
