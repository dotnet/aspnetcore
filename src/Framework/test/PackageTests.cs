// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore;

public class PackageTests
{
    private readonly string _packageLayoutRoot;
    private readonly ITestOutputHelper _output;

    public PackageTests(ITestOutputHelper output)
    {
        _output = output;
        var packageRoot = TestData.GetTestDataValue("ArtifactsPackagesDir");
        _packageLayoutRoot = TestData.GetTestDataValue("PackageLayoutRoot");
        var packages = Directory
                        .GetFiles(packageRoot, "*.nupkg", SearchOption.AllDirectories)
                        .Where(file => !file.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));
        foreach (var package in packages)
        {
            var outputPath = _packageLayoutRoot + Path.GetFileNameWithoutExtension(package);
            if (!Directory.Exists(outputPath))
            {
                ZipFile.ExtractToDirectory(package, outputPath);
            }
        }
    }

    [Fact]
    public void PackageAssembliesHaveExpectedAssemblyVersions()
    {
        /*if (!TestData.VerifyPackageAssemblyVersions())
        {
            return;
        } */

        var versionStringWithoutPrereleaseTag = TestData.GetSharedFxVersion().Split('-', 2)[0];
        var version = Version.Parse(versionStringWithoutPrereleaseTag);

        Debugger.Launch();
        foreach (var packageDir in Directory.GetDirectories(_packageLayoutRoot))
        {
            // Don't test the Shared Framework or Ref pack
            if (packageDir.Contains("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var packageAssembliesDir = Path.Combine(packageDir, "lib");
            if (Directory.Exists(packageAssembliesDir))
            {
                foreach (var tfmDir in Directory.GetDirectories(packageAssembliesDir))
                {
                    var tfm = new DirectoryInfo(tfmDir).Name;
                    foreach (var assembly in Directory.GetFiles(tfmDir, "*.dll"))
                    {
                        using var fileStream = File.OpenRead(assembly);
                        using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                        var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
                        var assemblyDefinition = reader.GetAssemblyDefinition();

                        // net & netstandard assembly versions should all match Major.Minor.0.0
                        // netfx assembly versions should match Major.Minor.Patch.0
                        Assert.Equal(version.Major, assemblyDefinition.Version.Major);
                        Assert.Equal(version.Minor, assemblyDefinition.Version.Minor);
                        if (IsNetFx(tfm))
                        {
                            Assert.Equal(version.Build, assemblyDefinition.Version.Build);
                        }
                        else
                        {
                            Assert.Equal(0, assemblyDefinition.Version.Build);
                        }
                        Assert.Equal(0, assemblyDefinition.Version.Revision);
                    }
                }
            }
        }
    }

    private bool IsNetFx(string tfm)
    {
        return (tfm.StartsWith("net4", StringComparison.OrdinalIgnoreCase));
    }
}

