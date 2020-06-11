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
        private readonly string _sharedFxRoot;
        private readonly ITestOutputHelper _output;

        public SharedFxTests(ITestOutputHelper output)
        {
            _output = output;
            _expectedTfm = "netcoreapp" + TestData.GetSharedFxVersion().Substring(0, 3);
            _expectedRid = TestData.GetSharedFxRuntimeIdentifier();
            _sharedFxRoot = Path.Combine(TestData.GetTestDataValue("SharedFrameworkLayoutRoot"), "shared", "Microsoft.AspNetCore.App", TestData.GetTestDataValue("RuntimePackageVersion"));
        }

        [Fact]
        public void SharedFrameworkContainsExpectedFiles()
        {
            var actualAssemblies = Directory.GetFiles(_sharedFxRoot, "*.dll")
                .Select(Path.GetFileNameWithoutExtension)
                .ToHashSet();
            var expectedAssemblies = TestData.GetSharedFxDependencies()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            _output.WriteLine("==== actual assemblies ====");
            _output.WriteLine(string.Join('\n', actualAssemblies.OrderBy(i => i)));
            _output.WriteLine("==== expected assemblies ====");
            _output.WriteLine(string.Join('\n', expectedAssemblies.OrderBy(i => i)));

            var missing = expectedAssemblies.Except(actualAssemblies);
            var unexpected = actualAssemblies.Except(expectedAssemblies);

            _output.WriteLine("==== missing assemblies from the framework ====");
            _output.WriteLine(string.Join('\n', missing));
            _output.WriteLine("==== unexpected assemblies in the framework ====");
            _output.WriteLine(string.Join('\n', unexpected));

            Assert.Empty(missing);
            Assert.Empty(unexpected);
        }

        [Fact]
        public void ItContainsValidRuntimeConfigFile()
        {
            var runtimeConfigFilePath = Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.runtimeconfig.json");

            AssertEx.FileExists(runtimeConfigFilePath);
            AssertEx.FileDoesNotExists(Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.runtimeconfig.dev.json"));

            var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

            Assert.Equal("Microsoft.NETCore.App", (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
            Assert.Equal(_expectedTfm, (string)runtimeConfig["runtimeOptions"]["tfm"]);
            Assert.Equal("LatestPatch", (string)runtimeConfig["runtimeOptions"]["rollForward"]);

            Assert.Equal(TestData.GetMicrosoftNETCoreAppPackageVersion(), (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
        }

        [Fact]
        public void ItContainsValidDepsJson()
        {
            var depsFilePath = Path.Combine(_sharedFxRoot, "Microsoft.AspNetCore.App.deps.json");

            var target = $".NETCoreApp,Version=v{TestData.GetSharedFxVersion().Substring(0, 3)}/{_expectedRid}";
            var ridPackageId = $"Microsoft.AspNetCore.App.Runtime.{_expectedRid}";
            var libraryId = $"{ridPackageId}/{TestData.GetTestDataValue("RuntimePackageVersion")}";

            AssertEx.FileExists(depsFilePath);

            var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

            Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
            Assert.NotNull(depsFile["compilationOptions"]);
            Assert.Empty(depsFile["compilationOptions"]);
            Assert.All(depsFile["libraries"], item =>
            {
                var prop = Assert.IsType<JProperty>(item);
                var lib = Assert.IsType<JObject>(prop.Value);
                Assert.Equal("package", lib["type"].Value<string>());
                Assert.Empty(lib["sha512"].Value<string>());
            });

            Assert.NotNull(depsFile["libraries"][libraryId]);
            Assert.Single(depsFile["libraries"].Values());

            var targetLibraries = depsFile["targets"][target];
            Assert.Single(targetLibraries.Values());
            var runtimeLibrary = targetLibraries[libraryId];
            Assert.Null(runtimeLibrary["dependencies"]);
            Assert.All(runtimeLibrary["runtime"], item =>
            {
                var obj = Assert.IsType<JProperty>(item);
                var assemblyVersion = obj.Value["assemblyVersion"].Value<string>();
                Assert.NotEmpty(assemblyVersion);
                Assert.True(Version.TryParse(assemblyVersion, out _), $"{assemblyVersion} should deserialize to System.Version");
                var fileVersion = obj.Value["fileVersion"].Value<string>();
                Assert.NotEmpty(fileVersion);
                Assert.True(Version.TryParse(fileVersion, out _), $"{fileVersion} should deserialize to System.Version");
            });

            if (_expectedRid.StartsWith("win", StringComparison.Ordinal) && !_expectedRid.Contains("arm"))
            {
                Assert.All(runtimeLibrary["native"], item =>
                {
                    var obj = Assert.IsType<JProperty>(item);
                    var fileVersion = obj.Value["fileVersion"].Value<string>();
                    Assert.NotEmpty(fileVersion);
                    Assert.True(Version.TryParse(fileVersion, out _), $"{fileVersion} should deserialize to System.Version");
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
            var versionFile = Path.Combine(_sharedFxRoot, ".version");
            AssertEx.FileExists(versionFile);
            var lines = File.ReadAllLines(versionFile);
            Assert.Equal(2, lines.Length);
            Assert.Equal(TestData.GetRepositoryCommit(), lines[0]);
            Assert.Equal(TestData.GetTestDataValue("RuntimePackageVersion"), lines[1]);
        }
    }
}
