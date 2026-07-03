// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Thin wrapper over the built <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> .nupkg that exposes
/// its entries for dotnet tool layout assertions.
/// </summary>
internal sealed class GatewayToolPackageArchive : IDisposable
{
    private readonly ZipArchive _archive;

    private GatewayToolPackageArchive(ZipArchive archive, string path)
    {
        _archive = archive;
        Path = path;
        EntryNames = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
    }

    public string Path { get; }

    public IReadOnlyList<string> EntryNames { get; }

    /// <summary>
    /// Opens the tool package. Tests that call this should be gated with
    /// <see cref="RequiresBuiltGatewayCliPackageAttribute"/> so they are skipped when it is absent.
    /// </summary>
    public static GatewayToolPackageArchive Open()
    {
        var path = GatewayCliTestData.TryGetPackagePath(GatewayCliTestData.PackageId)
            ?? throw new InvalidOperationException(
                $"Package '{GatewayCliTestData.PackageId}.{GatewayCliTestData.PackageVersion}.nupkg' was not found under the package output folders.");

        return new GatewayToolPackageArchive(ZipFile.OpenRead(path), path);
    }

    public bool HasEntry(string entryName)
        => EntryNames.Contains(entryName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

    public string ReadEntry(string entryName)
    {
        var normalized = entryName.Replace('\\', '/');
        var entry = _archive.Entries.FirstOrDefault(e =>
            string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Entry '{entryName}' not found in package '{GatewayCliTestData.PackageId}'.");

        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads the .nuspec embedded in the package.
    /// </summary>
    public string ReadNuspec()
    {
        var entryName = $"{GatewayCliTestData.PackageId}.nuspec";
        return ReadEntry(entryName);
    }

    public void Dispose() => _archive.Dispose();
}
