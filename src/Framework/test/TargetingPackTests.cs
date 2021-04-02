// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore
{
    public class TargetingPackTests
    {
        private readonly string _expectedRid;
        private readonly string _targetingPackTfm;
        private readonly string _targetingPackRoot;
        private readonly ITestOutputHelper _output;
        private readonly bool _isTargetingPackBuilding;

        public TargetingPackTests(ITestOutputHelper output)
        {
            _output = output;
            _expectedRid = TestData.GetSharedFxRuntimeIdentifier();
            _targetingPackTfm = TestData.GetDefaultNetCoreTargetFramework();
            var root = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")) ?
                TestData.GetTestDataValue("TargetingPackLayoutRoot") :
                Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");
            _targetingPackRoot = Path.Combine(
                root,
                "packs",
                "Microsoft.AspNetCore.App.Ref",
                TestData.GetTestDataValue("TargetingPackVersion"));
            _isTargetingPackBuilding = bool.Parse(TestData.GetTestDataValue("IsTargetingPackBuilding"));
        }

        [Fact]
        public void TargetingPackContainsListedAssemblies()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            var actualAssemblies = Directory.GetFiles(Path.Combine(_targetingPackRoot, "ref", _targetingPackTfm), "*.dll")
                .Select(Path.GetFileNameWithoutExtension)
                .ToHashSet();
            var listedTargetingPackAssemblies = TestData.ListedTargetingPackAssemblies.Keys.ToHashSet();

            _output.WriteLine("==== actual assemblies ====");
            _output.WriteLine(string.Join('\n', actualAssemblies.OrderBy(i => i)));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', listedTargetingPackAssemblies.OrderBy(i => i)));

            var missing = listedTargetingPackAssemblies.Except(actualAssemblies);
            var unexpected = actualAssemblies.Except(listedTargetingPackAssemblies);

            _output.WriteLine("==== missing assemblies from the framework ====");
            _output.WriteLine(string.Join('\n', missing));
            _output.WriteLine("==== unexpected assemblies in the framework ====");
            _output.WriteLine(string.Join('\n', unexpected));

            Assert.Empty(missing);
            Assert.Empty(unexpected);
        }

        [Fact]
        public void RefAssembliesHaveExpectedAssemblyVersions()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            IEnumerable<string> dlls = Directory.GetFiles(Path.Combine(_targetingPackRoot, "ref", _targetingPackTfm), "*.dll", SearchOption.AllDirectories);
            Assert.NotEmpty(dlls);

            Assert.All(dlls, path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var assemblyName = AssemblyName.GetAssemblyName(path);
                using var fileStream = File.OpenRead(path);
                using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
                var assemblyDefinition = reader.GetAssemblyDefinition();

                TestData.ListedTargetingPackAssemblies.TryGetValue(fileName, out var expectedVersion);
                Assert.Equal(expectedVersion, assemblyDefinition.Version.ToString());
            });
        }

        [Fact]
        public void RefAssemblyReferencesHaveExpectedAssemblyVersions()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            IEnumerable<string> dlls = Directory.GetFiles(Path.Combine(_targetingPackRoot, "ref", _targetingPackTfm), "*.dll", SearchOption.AllDirectories);
            Assert.NotEmpty(dlls);

            Assert.All(dlls, path =>
            {
                // Skip netstandard2.0 System.IO.Pipelines assembly. References have old versions.
                var filename = Path.GetFileName(path);
                if (!string.Equals("System.IO.Pipelines.dll", filename, StringComparison.OrdinalIgnoreCase))
                {
                    using var fileStream = File.OpenRead(path);
                    using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                    var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);

                    Assert.All(reader.AssemblyReferences, handle =>
                    {
                        var reference = reader.GetAssemblyReference(handle);
                        var result = 0 == reference.Version.Revision;

                        Assert.True(result, $"In {filename}, {reference.GetAssemblyName()} has unexpected version {reference.Version}.");
                    });
                }
            });
        }

        [Fact]
        public void PackageOverridesContainsCorrectEntries()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            var packageOverridePath = Path.Combine(_targetingPackRoot, "data", "PackageOverrides.txt");

            AssertEx.FileExists(packageOverridePath);

            var packageOverrideFileLines = File.ReadAllLines(packageOverridePath);
            var runtimeDependencies = TestData.GetRuntimeTargetingPackDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
            var aspnetcoreDependencies = TestData.GetAspNetCoreTargetingPackDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            Assert.Equal(packageOverrideFileLines.Length, runtimeDependencies.Count + aspnetcoreDependencies.Count);

            // PackageOverrides versions should remain at Major.Minor.0 while servicing.
            var netCoreAppPackageVersion = TestData.GetMicrosoftNETCoreAppPackageVersion();
            Assert.True(
                NuGetVersion.TryParse(netCoreAppPackageVersion, out var parsedVersion),
                "MicrosoftNETCoreAppPackageVersion must be convertable to a NuGetVersion.");
            if (parsedVersion.Patch != 0 && !parsedVersion.IsPrerelease)
            {
                netCoreAppPackageVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.0";
            }

            var aspNetCoreAppPackageVersion = TestData.GetReferencePackSharedFxVersion();
            Assert.True(
                NuGetVersion.TryParse(aspNetCoreAppPackageVersion, out parsedVersion),
                "ReferencePackSharedFxVersion must be convertable to a NuGetVersion.");
            if (parsedVersion.Patch != 0 && !parsedVersion.IsPrerelease)
            {
                aspNetCoreAppPackageVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.0";
            }

            Assert.All(packageOverrideFileLines, entry =>
            {
                var packageOverrideParts = entry.Split("|");
                Assert.Equal(2, packageOverrideParts.Length);

                var packageName = packageOverrideParts[0];
                var packageVersion = packageOverrideParts[1];

                if (runtimeDependencies.Contains(packageName))
                {
                    Assert.Equal(netCoreAppPackageVersion, packageVersion);
                }
                else if (aspnetcoreDependencies.Contains(packageName))
                {
                    Assert.Equal(aspNetCoreAppPackageVersion, packageVersion);
                }
                else
                {
                    Assert.True(false, $"{packageName} is not a recognized aspNetCore or runtime dependency");
                }
            });
        }

        [Fact]
        public void AssembliesAreReferenceAssemblies()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            IEnumerable<string> dlls = Directory.GetFiles(_targetingPackRoot, "*.dll", SearchOption.AllDirectories);
            Assert.NotEmpty(dlls);

            Assert.All(dlls, path =>
            {
                var assemblyName = AssemblyName.GetAssemblyName(path);
                using var fileStream = File.OpenRead(path);
                using var peReader = new PEReader(fileStream, PEStreamOptions.Default);
                var reader = peReader.GetMetadataReader(MetadataReaderOptions.Default);
                var assemblyDefinition = reader.GetAssemblyDefinition();
                var hasRefAssemblyAttribute = assemblyDefinition.GetCustomAttributes().Any(attr =>
                {
                    var attribute = reader.GetCustomAttribute(attr);
                    var attributeConstructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    var attributeType = reader.GetTypeReference((TypeReferenceHandle)attributeConstructor.Parent);
                    return reader.StringComparer.Equals(attributeType.Namespace, typeof(ReferenceAssemblyAttribute).Namespace)
                        && reader.StringComparer.Equals(attributeType.Name, nameof(ReferenceAssemblyAttribute));
                });

                Assert.True(hasRefAssemblyAttribute, $"{path} should have {nameof(ReferenceAssemblyAttribute)}");
                Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
            });
        }

        [Fact]
        public void PlatformManifestListsAllFiles()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            var platformManifestPath = Path.Combine(_targetingPackRoot, "data", "PlatformManifest.txt");
            var expectedAssemblies = TestData.GetSharedFxDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(i =>
                {
                    var fileName = Path.GetFileName(i);
                    return fileName.EndsWith(".dll", StringComparison.Ordinal)
                        ? fileName.Substring(0, fileName.Length - 4)
                        : fileName;
                })
                .ToHashSet();

            _output.WriteLine("==== file contents ====");
            _output.WriteLine(File.ReadAllText(platformManifestPath));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', expectedAssemblies.OrderBy(i => i)));

            AssertEx.FileExists(platformManifestPath);

            var manifestFileLines = File.ReadAllLines(platformManifestPath);

            var actualAssemblies = manifestFileLines
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(i =>
                {
                    var fileName = i.Split('|')[0];
                    return fileName.EndsWith(".dll", StringComparison.Ordinal)
                        ? fileName.Substring(0, fileName.Length - 4)
                        : fileName;
                })
                .ToHashSet();

            if (!TestData.VerifyAncmBinary())
            {
                actualAssemblies.Remove("aspnetcorev2_inprocess");
                expectedAssemblies.Remove("aspnetcorev2_inprocess");
            }

            var missing = expectedAssemblies.Except(actualAssemblies);
            var unexpected = actualAssemblies.Except(expectedAssemblies);

            _output.WriteLine("==== missing assemblies from the manifest ====");
            _output.WriteLine(string.Join('\n', missing));
            _output.WriteLine("==== unexpected assemblies in the manifest ====");
            _output.WriteLine(string.Join('\n', unexpected));

            Assert.Empty(missing);
            Assert.Empty(unexpected);

            Assert.All(manifestFileLines, line =>
            {
                var parts = line.Split('|');
                Assert.Equal(4, parts.Length);
                Assert.Equal("Microsoft.AspNetCore.App.Ref", parts[1]);
                if (parts[2].Length > 0)
                {
                    Assert.True(Version.TryParse(parts[2], out _), "Assembly version must be convertable to System.Version");
                }
                Assert.True(Version.TryParse(parts[3], out _), "File version must be convertable to System.Version");
            });
        }

        [Fact]
        public void FrameworkListListsContainsCorrectEntries()
        {
            if (!_isTargetingPackBuilding)
            {
                return;
            }

            var frameworkListPath = Path.Combine(_targetingPackRoot, "data", "FrameworkList.xml");
            var expectedAssemblies = TestData.GetTargetingPackDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
            expectedAssemblies.Remove("aspnetcorev2_inprocess");

            AssertEx.FileExists(frameworkListPath);

            var frameworkListDoc = XDocument.Load(frameworkListPath);
            var frameworkListEntries = frameworkListDoc.Root.Descendants();

            _output.WriteLine("==== file contents ====");
            _output.WriteLine(string.Join('\n', frameworkListEntries.Select(i => i.Attribute("AssemblyName").Value).OrderBy(i => i)));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', expectedAssemblies.OrderBy(i => i)));

             var actualAssemblies = frameworkListEntries
                .Select(i =>
                {
                    var fileName = i.Attribute("AssemblyName").Value;
                    return fileName.EndsWith(".dll", StringComparison.Ordinal)
                        ? fileName.Substring(0, fileName.Length - 4)
                        : fileName;
                })
                .ToHashSet();

            var missing = expectedAssemblies.Except(actualAssemblies);
            var unexpected = actualAssemblies.Except(expectedAssemblies);

            _output.WriteLine("==== missing assemblies from the framework list ====");
            _output.WriteLine(string.Join('\n', missing));
            _output.WriteLine("==== unexpected assemblies in the framework list ====");
            _output.WriteLine(string.Join('\n', unexpected));

            Assert.Empty(missing);
            Assert.Empty(unexpected);

            Assert.All(frameworkListEntries, i =>
            {
                var assemblyPath = i.Attribute("Path").Value;
                var assemblyVersion = i.Attribute("AssemblyVersion").Value;
                var fileVersion = i.Attribute("FileVersion").Value;

                Assert.True(Version.TryParse(assemblyVersion, out _), $"{assemblyPath} has assembly version {assemblyVersion}. Assembly version must be convertable to System.Version");
                Assert.True(Version.TryParse(fileVersion, out _), $"{assemblyPath} has file version {fileVersion}. File version must be convertable to System.Version");
            });
        }

        [Fact]
        public void FrameworkListListsContainsCorrectPaths()
        {
            if (!_isTargetingPackBuilding || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
            {
                return;
            }

            var frameworkListPath = Path.Combine(_targetingPackRoot, "data", "FrameworkList.xml");

            AssertEx.FileExists(frameworkListPath);

            var frameworkListDoc = XDocument.Load(frameworkListPath);
            var frameworkListEntries = frameworkListDoc.Root.Descendants();

            var targetingPackPath = Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), ("Microsoft.AspNetCore.App.Ref." + TestData.GetSharedFxVersion() + ".nupkg"));

            ZipArchive archive = ZipFile.OpenRead(targetingPackPath);

            var actualPaths = archive.Entries
                .Where(i => i.FullName.EndsWith(".dll", StringComparison.Ordinal))
                .Select(i => i.FullName).ToHashSet();

            var expectedPaths = frameworkListEntries.Select(i => i.Attribute("Path").Value).ToHashSet();

            _output.WriteLine("==== package contents ====");
            _output.WriteLine(string.Join('\n', actualPaths.OrderBy(i => i)));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', expectedPaths.OrderBy(i => i)));

            var missing = expectedPaths.Except(actualPaths);
            var unexpected = actualPaths.Except(expectedPaths);

            _output.WriteLine("==== missing assemblies from the runtime list ====");
            _output.WriteLine(string.Join('\n', missing));
            _output.WriteLine("==== unexpected assemblies in the runtime list ====");
            _output.WriteLine(string.Join('\n', unexpected));

            Assert.Empty(missing);
            Assert.Empty(unexpected);
        }
    }
}
