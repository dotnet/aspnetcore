// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore
{
    public class TargetingPackTests
    {
        private readonly string _expectedRid;
        private readonly string _targetingPackRoot;
        private readonly ITestOutputHelper _output;

        public TargetingPackTests(ITestOutputHelper output)
        {
            _output = output;
            _expectedRid = TestData.GetSharedFxRuntimeIdentifier();
            _targetingPackRoot = Path.Combine(TestData.GetTestDataValue("TargetingPackLayoutRoot"), "packs", "Microsoft.AspNetCore.App.Ref", TestData.GetTestDataValue("TargetingPackVersion"));
        }

        [Fact]
        public void AssembliesAreReferenceAssemblies()
        {
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
    }
}
