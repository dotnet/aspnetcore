// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore
{
    public class SharedFxTests
    {
        private readonly string _expectedTfm;
        private readonly string _expectedRid;
        private readonly ITestOutputHelper _output;

        public SharedFxTests(ITestOutputHelper output)
        {
            _output = output;
            _expectedTfm = "netcoreapp" + TestData.GetPackageVersion().Substring(0, 3);
            _expectedRid = TestData.GetSharedFxRuntimeIdentifier();
        }

        [Fact]
        public void PlatformManifestListsAllFiles()
        {
            var platformManifestPath = Path.Combine(TestData.GetManifestOutputDir(), "Microsoft.AspNetCore.App.PlatformManifest.txt");
            var expectedAssemblies = TestData.GetSharedFxDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            _output.WriteLine("==== file contents ====");
            _output.WriteLine(File.ReadAllText(platformManifestPath));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', expectedAssemblies));

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

            var missing = expectedAssemblies.Except(actualAssemblies);
            var unexpected = actualAssemblies.Except(expectedAssemblies)
                .Where(s => !string.Equals(s, "aspnetcorev2_inprocess", StringComparison.Ordinal)); // this native assembly only appears in Windows builds.

            if (_expectedRid.StartsWith("win", StringComparison.Ordinal) && !_expectedRid.Contains("arm"))
            {
                Assert.Contains("aspnetcorev2_inprocess", actualAssemblies);
            }

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
                Assert.Equal("Microsoft.AspNetCore.App", parts[1]);
                if (parts[2].Length > 0)
                {
                    Assert.True(Version.TryParse(parts[2], out _), "Assembly version must be convertable to System.Version");
                }
                Assert.True(Version.TryParse(parts[3], out _), "File version must be convertable to System.Version");
            });
        }

        [Fact]
        public void ItContainsValidRuntimeConfigFile()
        {
            var runtimeConfigFilePath = Path.Combine(TestData.GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.json");

            AssertEx.FileExists(runtimeConfigFilePath);
            AssertEx.FileDoesNotExists(Path.Combine(TestData.GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.dev.json"));

            var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

            Assert.Equal("Microsoft.NETCore.App", (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
            Assert.Equal(_expectedTfm, (string)runtimeConfig["runtimeOptions"]["tfm"]);

            Assert.Equal(TestData.GetMicrosoftNETCoreAppPackageVersion(), (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
        }

        [Fact]
        public void ItContainsValidDepsJson()
        {
            var depsFilePath = Path.Combine(TestData.GetMetadataOutput(), "Microsoft.AspNetCore.App.deps.json");

            var target = $".NETCoreApp,Version=v{TestData.GetPackageVersion().Substring(0, 3)}/{_expectedRid}";
            var ridPackageId = $"runtime.{_expectedRid}.Microsoft.AspNetCore.App";

            AssertEx.FileExists(depsFilePath);

            var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

            Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
            Assert.NotNull(depsFile["compilationOptions"]);
            Assert.Empty(depsFile["compilationOptions"]);
            Assert.NotEmpty(depsFile["runtimes"][_expectedRid]);
            Assert.All(depsFile["libraries"], item =>
            {
                var prop = Assert.IsType<JProperty>(item);
                var lib = Assert.IsType<JObject>(prop.Value);
                Assert.Equal("package", lib["type"].Value<string>());
                Assert.Empty(lib["sha512"].Value<string>());
            });

            Assert.NotNull(depsFile["libraries"][$"Microsoft.AspNetCore.App/{TestData.GetPackageVersion()}"]);
            Assert.NotNull(depsFile["libraries"][$"runtime.{_expectedRid}.Microsoft.AspNetCore.App/{TestData.GetPackageVersion()}"]);
            Assert.Equal(2, depsFile["libraries"].Values().Count());

            var targetLibraries = depsFile["targets"][target];
            Assert.Equal(2, targetLibraries.Values().Count());
            var metapackage = targetLibraries[$"Microsoft.AspNetCore.App/{TestData.GetPackageVersion()}"];
            Assert.Null(metapackage["runtime"]);
            Assert.Null(metapackage["native"]);

            var runtimeLibrary = targetLibraries[$"{ridPackageId}/{TestData.GetPackageVersion()}"];
            Assert.Null(runtimeLibrary["dependencies"]);
            Assert.All(runtimeLibrary["runtime"], item =>
            {
                var obj = Assert.IsType<JProperty>(item);
                Assert.StartsWith($"runtimes/{_expectedRid}/lib/{_expectedTfm}/", obj.Name);
                Assert.NotEmpty(obj.Value["assemblyVersion"].Value<string>());
                Assert.NotEmpty(obj.Value["fileVersion"].Value<string>());
            });

            if (_expectedRid.StartsWith("win", StringComparison.Ordinal) && !_expectedRid.Contains("arm"))
            {
                Assert.All(runtimeLibrary["native"], item =>
                {
                    var obj = Assert.IsType<JProperty>(item);
                    Assert.StartsWith($"runtimes/{_expectedRid}/native/", obj.Name);
                });
            }
            else
            {
                Assert.Null(runtimeLibrary["native"]);
            }
        }

        [Fact]
        public void ItContainsVersionFile()
        {
            var versionFile = Path.Combine(TestData.GetMetadataOutput(), ".version");
            AssertEx.FileExists(versionFile);
            var lines = File.ReadAllLines(versionFile);
            Assert.Equal(2, lines.Length);
            Assert.Equal(TestData.GetRepositoryCommit(), lines[0]);
            Assert.Equal(TestData.GetPackageVersion(), lines[1]);
        }
    }
}
