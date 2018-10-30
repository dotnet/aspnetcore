// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore
{
    public class SharedFxTests
    {
        [Fact]
        public void ItContainsValidRuntimeConfigFile()
        {
            var runtimeConfigFilePath = Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.json");

            AssertEx.FileExists(runtimeConfigFilePath);
            AssertEx.FileDoesNotExists(Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.runtimeconfig.dev.json"));

            var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

            Assert.Equal("Microsoft.NETCore.App", (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
            Assert.Equal("netcoreapp" + TestData.GetPackageVersion().Substring(0, 3), (string)runtimeConfig["runtimeOptions"]["tfm"]);

            Assert.Equal(TestData.GetMicrosoftNETCoreAppPackageVersion(), (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
        }

        [Fact]
        public void ItContainsValidDepsJson()
        {
            var depsFilePath = Path.Combine(GetMetadataOutput(), "Microsoft.AspNetCore.App.deps.json");
            var rid = TestData.GetSharedFxRuntimeIdentifier();

            var target = $".NETCoreApp,Version=v{TestData.GetPackageVersion().Substring(0, 3)}/{rid}";

            AssertEx.FileExists(depsFilePath);

            var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

            Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
            Assert.NotNull(depsFile["targets"][target]);
            Assert.NotNull(depsFile["compilationOptions"]);
            Assert.Empty(depsFile["compilationOptions"]);
            Assert.NotEmpty(depsFile["runtimes"][rid]);
            Assert.All(depsFile["libraries"], item =>
            {
                var prop = Assert.IsType<JProperty>(item);
                var lib = Assert.IsType<JObject>(prop.Value);
                Assert.Equal("package", lib["type"].Value<string>());
                Assert.StartsWith("sha512-", lib["sha512"].Value<string>());
            });
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
