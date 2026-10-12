// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

class E2EManifest
{
    [JsonPropertyName("apps")]
    public Dictionary<string, E2EAppEntry> Apps { get; set; } = new();

    public E2EAppEntry? GetApp(string appName)
    {
        Apps.TryGetValue(appName, out var entry);
        return entry;
    }

    internal static E2EManifest Load(string assemblyName)
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.e2e-manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                $"E2E manifest not found: {manifestPath}. " +
                "Ensure Microsoft.AspNetCore.Components.Testing.targets is imported in the test .csproj " +
                "and at least one ProjectReference has <E2EApp>true</E2EApp> metadata.",
                manifestPath);
        }

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize(json, E2EManifestJsonContext.Default.E2EManifest)
            ?? throw new InvalidOperationException($"Failed to deserialize E2E manifest: {manifestPath}");

        manifest.Validate();
        return manifest;
    }

    internal void Validate()
    {
        foreach (var (appName, entry) in Apps)
        {
            if (entry.HarnessMode != E2EAppEntry.StartupHookHarnessMode &&
                entry.HarnessMode != E2EAppEntry.CompiledHarnessMode)
            {
                throw new InvalidOperationException(
                    $"E2E manifest entry '{appName}' has unsupported harnessMode '{entry.HarnessMode}'. " +
                    $"Expected '{E2EAppEntry.StartupHookHarnessMode}' or '{E2EAppEntry.CompiledHarnessMode}'.");
            }

            if (entry.HarnessMode == E2EAppEntry.CompiledHarnessMode &&
                string.Equals(entry.Executable, "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"E2E manifest entry '{appName}' uses the compiled harness but does not point to a native executable.");
            }
        }
    }
}
