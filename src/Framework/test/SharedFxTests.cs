// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore
{
    public class SharedFxTests
    {
        private readonly string _expectedTfm;

        public SharedFxTests()
        {
            _expectedTfm = "netcoreapp" + TestData.GetPackageVersion().Substring(0, 3);
        }

        [Fact]
        public void ItContainsValidRuntimeConfigFile()
        {
            var runtimeConfigFilePath = Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.json");

            AssertEx.FileExists(runtimeConfigFilePath);
            AssertEx.FileDoesNotExists(Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.dev.json"));

            var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

            Assert.Equal("Microsoft.NETCore.App", (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
            Assert.Equal(_expectedTfm, (string)runtimeConfig["runtimeOptions"]["tfm"]);

            Assert.Equal(TestData.GetMicrosoftNETCoreAppPackageVersion(), (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
        }

        [Fact]
        public void ItContainsValidDepsJson()
        {
            var depsFilePath = Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.deps.json");
            var rid = TestData.GetSharedFxRuntimeIdentifier();

            var target = $".NETCoreApp,Version=v{TestData.GetPackageVersion().Substring(0, 3)}/{rid}";
            var ridPackageId = $"runtime.{rid}.Microsoft.AspNetCore.App";

            AssertEx.FileExists(depsFilePath);

            var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

            Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
            Assert.NotNull(depsFile["compilationOptions"]);
            Assert.Empty(depsFile["compilationOptions"]);
            Assert.NotEmpty(depsFile["runtimes"][rid]);
            Assert.All(depsFile["libraries"], item =>
            {
                var prop = Assert.IsType<JProperty>(item);
                var lib = Assert.IsType<JObject>(prop.Value);
                Assert.Equal("package", lib["type"].Value<string>());
                Assert.Empty(lib["sha512"].Value<string>());
            });

            Assert.NotNull(depsFile["libraries"][$"Microsoft.AspNetCore.App/{TestData.GetPackageVersion()}"]);
            Assert.NotNull(depsFile["libraries"][$"runtime.{rid}.Microsoft.AspNetCore.App/{TestData.GetPackageVersion()}"]);
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
                Assert.StartsWith($"runtimes/{rid}/lib/{_expectedTfm}/", obj.Name);
                Assert.NotEmpty(obj.Value["assemblyVersion"].Value<string>());
                Assert.NotEmpty(obj.Value["fileVersion"].Value<string>());
            });

            if (TestData.GetSharedFxRuntimeIdentifier().StartsWith("win"))
            {
                Assert.All(runtimeLibrary["native"], item =>
                {
                    var obj = Assert.IsType<JProperty>(item);
                    Assert.StartsWith($"runtimes/{rid}/native/", obj.Name);
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
            var versionFile = Path.Combine(GetMetadataOutput(), ".version");
            AssertEx.FileExists(versionFile);
            var lines = File.ReadAllLines(versionFile);
            Assert.Equal(2, lines.Length);
            Assert.Equal(TestData.GetRepositoryCommit(), lines[0]);
            Assert.Equal(TestData.GetPackageVersion(), lines[1]);
        }

        private string GetMetadataOutput() => TestData.GetTestDataValue("SharedFxMetadataOutput:Microsoft.AspNetCore.App");
    }
}
