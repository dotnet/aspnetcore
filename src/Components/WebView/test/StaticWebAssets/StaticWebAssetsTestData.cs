// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Resolves test-time configuration (package locations, version, repo paths) injected as assembly
/// metadata by the test project's csproj.
/// </summary>
internal static class StaticWebAssetsTestData
{
    private static readonly Dictionary<string, string> Metadata = typeof(StaticWebAssetsTestData).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(a => a.Value is not null)
        .GroupBy(a => a.Key, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Last().Value!, StringComparer.Ordinal);

    public static string PackageVersion => GetValue("StaticWebAssetsTestPackageVersion");

    public static string ShippingPackagesDir => GetValue("ArtifactsShippingPackagesDir");

    public static string NonShippingPackagesDir => GetValue("ArtifactsNonShippingPackagesDir");

    public static string RepoRoot => GetValue("RepoRoot");

    /// <summary>
    /// The repo's global NuGet packages folder, used as a read-only fallback so consumer builds can
    /// resolve the exact transitive package versions the repo restored (which may not be on public feeds).
    /// </summary>
    public static string NuGetPackageRoot => GetValue("NuGetPackageRoot");

    /// <summary>
    /// Root directory for throwaway build working folders (under the repo's artifacts/tmp), used
    /// instead of the system temp folder so test output is colocated with other build artifacts and
    /// cleaned up by the normal artifacts lifecycle.
    /// </summary>
    public static string ArtifactsTmpDir => GetValue("ArtifactsTmpDir");

    /// <summary>
    /// Directory where build logs (binlogs) are written so CI collects them for diagnosing failures.
    /// </summary>
    public static string ArtifactsLogDir => GetValue("ArtifactsLogDir");

    public static string DefaultTargetFramework => GetValue("DefaultNetCoreTargetFramework");

    /// <summary>
    /// Absolute path to the WebView source project, used by the ProjectReference (P2P) publish test
    /// that reproduces the in-repo "Conflicting assets" publish failure.
    /// </summary>
    public static string WebViewProjectPath => Path.Combine(
        RepoRoot, "src", "Components", "WebView", "WebView", "src", "Microsoft.AspNetCore.Components.WebView.csproj");

    /// <summary>
    /// Absolute path to the WebView consumer-side targets (sets JSModuleManifestRelativePath and
    /// conditionally materializes the empty blazor.modules.json fallback when the app has no JS
    /// library modules of its own). Imported by P2P consumers like the in-repo Photino sample.
    /// </summary>
    public static string WebViewGroupsTargetsPath => Path.Combine(
        RepoRoot, "src", "Components", "WebView", "WebView", "src", "StaticWebAssets.Groups.targets");

    /// <summary>
    /// Path to the locally-built SDK host (.dotnet/dotnet[.exe]) used to run consumer builds.
    /// </summary>
    public static string DotNetHost
    {
        get
        {
            var fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            return Path.Combine(RepoRoot, ".dotnet", fileName);
        }
    }

    /// <summary>
    /// Locates the .nupkg for the given package id (looking in the shipping and non-shipping
    /// package output folders). Returns <see langword="null"/> when the package was not built.
    /// </summary>
    public static string? TryGetPackagePath(string packageId)
    {
        var fileName = $"{packageId}.{PackageVersion}.nupkg";
        foreach (var dir in new[] { ShippingPackagesDir, NonShippingPackagesDir })
        {
            if (string.IsNullOrEmpty(dir))
            {
                continue;
            }

            var candidate = Path.Combine(dir, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GetValue(string key)
        => Metadata.TryGetValue(key, out var value)
            ? value
            : throw new InvalidOperationException($"Missing assembly metadata '{key}'. Ensure the test project injects it.");
}
