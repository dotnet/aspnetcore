// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
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
        Debugger.Launch();
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
        if (!TestData.VerifyPackageAssemblyVersions())
        {
            return;
        }
    }
}

