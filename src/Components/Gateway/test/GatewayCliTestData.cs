// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Resolves test-time configuration (package locations, version, repo paths) injected as assembly
/// metadata by the test project's csproj. Used by the tests that validate the
/// <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> dotnet tool package.
/// </summary>
internal static class GatewayCliTestData
{
    /// <summary>
    /// The id of the dotnet tool package that repackages the gateway binaries.
    /// </summary>
    public const string PackageId = "Microsoft.AspNetCore.Components.Gateway.Cli";

    /// <summary>
    /// The command the tool exposes once installed (matches the assembly name of the gateway).
    /// </summary>
    public const string ToolCommandName = "blazor-gateway";

    private static readonly Dictionary<string, string> Metadata = typeof(GatewayCliTestData).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(a => a.Value is not null)
        .GroupBy(a => a.Key, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Last().Value!, StringComparer.Ordinal);

    public static string PackageVersion => GetValue("GatewayCliPackageVersion");

    public static string ShippingPackagesDir => GetValue("ArtifactsShippingPackagesDir");

    public static string NonShippingPackagesDir => GetValue("ArtifactsNonShippingPackagesDir");

    public static string RepoRoot => GetValue("RepoRoot");

    public static string ArtifactsTmpDir => GetValue("ArtifactsTmpDir");

    public static string DefaultTargetFramework => GetValue("DefaultNetCoreTargetFramework");

    /// <summary>
    /// Path to the locally-built SDK host (.dotnet/dotnet[.exe]) used to install and run the tool.
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
    /// The directory containing the shared framework (.dotnet), used as DOTNET_ROOT when running the
    /// installed tool so the framework-dependent gateway can resolve Microsoft.AspNetCore.App.
    /// </summary>
    public static string DotNetRoot => Path.Combine(RepoRoot, ".dotnet");

    /// <summary>
    /// Locates the .nupkg for the given package id (looking in the shipping and non-shipping package
    /// output folders). Returns <see langword="null"/> when the package was not built.
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
