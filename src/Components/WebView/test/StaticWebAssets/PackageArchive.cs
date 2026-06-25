// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Thin wrapper over a built .nupkg that exposes its entries for layout assertions.
/// </summary>
internal sealed class PackageArchive : IDisposable
{
    private readonly ZipArchive _archive;

    private PackageArchive(ZipArchive archive, string packageId, string path)
    {
        _archive = archive;
        PackageId = packageId;
        Path = path;
        EntryNames = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
    }

    public string PackageId { get; }

    public string Path { get; }

    public IReadOnlyList<string> EntryNames { get; }

    /// <summary>
    /// Opens the package for the given id. Tests that call this should be gated with
    /// <see cref="RequiresBuiltPackagesAttribute"/> so they are skipped when the package is absent.
    /// </summary>
    public static PackageArchive Open(string packageId)
    {
        var path = StaticWebAssetsTestData.TryGetPackagePath(packageId)
            ?? throw new InvalidOperationException(
                $"Package '{packageId}.{StaticWebAssetsTestData.PackageVersion}.nupkg' was not found under the package output folders.");

        return new PackageArchive(ZipFile.OpenRead(path), packageId, path);
    }

    public bool HasEntry(string entryName)
        => EntryNames.Contains(entryName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

    public string ReadEntry(string entryName)
    {
        var normalized = entryName.Replace('\\', '/');
        var entry = _archive.Entries.FirstOrDefault(e =>
            string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Entry '{entryName}' not found in package '{PackageId}'.");

        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Parses the SDK-generated static web assets package manifest ($(PackageId).PackageAssets.json).
    /// </summary>
    public JsonDocument ReadPackageAssetsManifest()
    {
        var entryName = $"build/{PackageId}.PackageAssets.json";
        return JsonDocument.Parse(ReadEntry(entryName));
    }

    public void Dispose() => _archive.Dispose();
}
