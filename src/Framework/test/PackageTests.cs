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
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore;

public class PackageTests
{
    private readonly string _packageLayoutRoot;
    private readonly ITestOutputHelper _output;

    public PackageTests(ITestOutputHelper output)
    {
        // In official builds, only run this test on Helix. The test will work locally so long as you're building w/
        // shipping versions (i.e., include `/p:DotNetUseShippingVersions=true` on a full build command), and you do not
        // have binaries built locally from another major version.
        if (!TestData.VerifyPackageAssemblyVersions() && !SkipOnCIAttribute.OnCI())
        {
            return;
        }

        _output = output;
        _packageLayoutRoot = SkipOnHelixAttribute.OnHelix() ?
            Path.Combine(
                Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"),
                "Packages.Layout") :
            TestData.GetPackageLayoutRoot();
        var packageRoot = SkipOnHelixAttribute.OnHelix() ?
            Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") :
            TestData.GetPackagesFolder();
        var packages = Directory
                        .GetFiles(packageRoot, "*.nupkg", SearchOption.AllDirectories)
                        .Where(file => !file.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        if (Directory.Exists(_packageLayoutRoot))
        {
            Directory.Delete(_packageLayoutRoot, true);
        }

        foreach (var package in packages)
        {
            var outputPath = Path.Combine(_packageLayoutRoot, Path.GetFileNameWithoutExtension(package));
            ZipFile.ExtractToDirectory(package, outputPath);
        }
    }

    [Fact]
    public void PackageAssembliesHaveExpectedAssemblyVersions()
    {
        // In official builds, only run this test on Helix. The test will work locally so long as we're building w/ shipping versions.
        if (!TestData.VerifyPackageAssemblyVersions() && !SkipOnCIAttribute.OnCI())
        {
            return;
        }

        var versionStringWithoutPrereleaseTag = TestData.GetSharedFxVersion().Split('-', 2)[0];
        var expectedVersion = Version.Parse(versionStringWithoutPrereleaseTag);

        string[] helixTestRunnerToolPackages = { "dotnet-serve", "dotnet-ef", "dotnet-dump" };
        string[] toolAssembliesToSkip = { "System.", "Microsoft.", "Azure.", "Newtonsoft.", "aspnetcorev2" };

        foreach (var packageDir in Directory.GetDirectories(_packageLayoutRoot))
        {
            // Don't test the Shared Framework or Ref pack; assembly versions in those packages are checked elsewhere.
            if (packageDir.Contains("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Don't test helix test runner tool packages
            if (helixTestRunnerToolPackages.Any(s => packageDir.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Test lib assemblies
            var packageAssembliesDir = Path.Combine(packageDir, "lib");
            if (Directory.Exists(packageAssembliesDir))
            {
                foreach (var tfmDir in Directory.GetDirectories(packageAssembliesDir))
                {
                    var isNetFx = IsNetFx(new DirectoryInfo(tfmDir).Name);
                    foreach (var assembly in Directory.GetFiles(tfmDir, "*.dll"))
                    {
                        using var fileStream = File.OpenRead(assembly);
                        using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                        var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
                        var assemblyVersion = reader.GetAssemblyDefinition().Version;

                        // net & netstandard assembly versions should all match Major.Minor.0.0
                        // netfx assembly versions should match Major.Minor.Patch.0
                        Assert.Equal(expectedVersion.Major, assemblyVersion.Major);
                        Assert.Equal(expectedVersion.Minor, assemblyVersion.Minor);
                        if (isNetFx)
                        {
                            Assert.Equal(expectedVersion.Build, assemblyVersion.Build);
                        }
                        else
                        {
                            Assert.Equal(0, assemblyVersion.Build);
                        }
                        Assert.Equal(0, assemblyVersion.Revision);
                    }
                }
            }

            // Test tool assemblies
            var packageToolsDir = Path.Combine(packageDir, "tools");
            if (Directory.Exists(packageToolsDir))
            {
                var assemblies = Directory.GetFiles(packageToolsDir, "*.dll", SearchOption.AllDirectories)
                    .Where(f => !toolAssembliesToSkip.Any(s => Path.GetFileNameWithoutExtension(f).Contains(s, StringComparison.OrdinalIgnoreCase)));
                foreach (var assembly in assemblies)
                {
                    using var fileStream = File.OpenRead(assembly);
                    using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                    var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
                    var assemblyVersion = reader.GetAssemblyDefinition().Version;

                    Assert.Equal(expectedVersion.Major, assemblyVersion.Major);
                    Assert.Equal(expectedVersion.Minor, assemblyVersion.Minor);
                    Assert.Equal(0, assemblyVersion.Build);
                    Assert.Equal(0, assemblyVersion.Revision);
                }
            }
        }
    }

    private bool IsNetFx(string tfm)
    {
        return tfm.StartsWith("net4", StringComparison.OrdinalIgnoreCase);
    }
}

