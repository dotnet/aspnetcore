// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore
{
    public class SharedFxTests
    {
        [Theory]
        [MemberData(nameof(GetSharedFxConfig))]
        public void ItContainsValidRuntimeConfigFile(SharedFxConfig config)
        {
            var root = TestData.GetDotNetRoot();
            var dir = Path.Combine(root, "shared", config.Name, config.Version);
            var runtimeConfigFilePath = Path.Combine(dir, config.Name + ".runtimeconfig.json");

            AssertEx.FileExists(runtimeConfigFilePath);
            AssertEx.FileDoesNotExists(Path.Combine(dir, config.Name + ".runtimeconfig.dev.json"));

            var runtimeConfig = JObject.Parse(File.ReadAllText(runtimeConfigFilePath));

            Assert.Equal(config.BaseSharedFxName, (string)runtimeConfig["runtimeOptions"]["framework"]["name"]);
            Assert.Equal("netcoreapp" + config.Version.Substring(0, 3), (string)runtimeConfig["runtimeOptions"]["tfm"]);

            Assert.Equal(config.BaseSharedFxVersion, (string)runtimeConfig["runtimeOptions"]["framework"]["version"]);
        }

        [Theory]
        [MemberData(nameof(GetSharedFxConfig))]
        public void ItContainsValidDepsJson(SharedFxConfig config)
        {
            var root = TestData.GetDotNetRoot();
            var dir = Path.Combine(root, "shared", config.Name, config.Version);
            var depsFilePath = Path.Combine(dir, config.Name + ".deps.json");

            var target = $".NETCoreApp,Version=v{config.Version.Substring(0, 3)}/{config.RuntimeIdentifier}";

            AssertEx.FileExists(depsFilePath);

            var depsFile = JObject.Parse(File.ReadAllText(depsFilePath));

            Assert.Equal(target, (string)depsFile["runtimeTarget"]["name"]);
            Assert.NotNull(depsFile["targets"][target]);
            Assert.NotNull(depsFile["compilationOptions"]);
            Assert.Empty(depsFile["compilationOptions"]);
            Assert.NotEmpty(depsFile["runtimes"][config.RuntimeIdentifier]);
        }

        [Theory]
        [MemberData(nameof(GetSharedFxConfig))]
        public void ItContainsVersionFile(SharedFxConfig config)
        {
            var root = TestData.GetDotNetRoot();
            var versionFile = Path.Combine(root, "shared", config.Name, config.Version, ".version");
            AssertEx.FileExists(versionFile);
            var lines = File.ReadAllLines(versionFile);
            Assert.Equal(2, lines.Length);
            Assert.Equal(TestData.GetRepositoryCommit(), lines[0]);
            Assert.Equal(config.Version, lines[1]);
        }


        public static TheoryData<SharedFxConfig> GetSharedFxConfig()
            => new TheoryData<SharedFxConfig>
            {
                new SharedFxConfig
                {
                    Name = "Microsoft.AspNetCore.All",
                    Version = TestData.GetPackageVersion(),
                    // Intentionally assert aspnetcore frameworks align versions with each other and netcore
                    BaseSharedFxVersion = TestData.GetPackageVersion(),
                    BaseSharedFxName = "Microsoft.AspNetCore.App",
                    RuntimeIdentifier = TestData.GetSharedFxRuntimeIdentifier(),
                },
                new SharedFxConfig
                {
                    Name = "Microsoft.AspNetCore.App",
                    Version = TestData.GetPackageVersion(),
                    BaseSharedFxName = "Microsoft.NETCore.App",
                    BaseSharedFxVersion = TestData.GetMicrosoftNETCoreAppPackageVersion(),
                    RuntimeIdentifier = TestData.GetSharedFxRuntimeIdentifier(),
                },
            };

        public class SharedFxConfig
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string BaseSharedFxName { get; set; }
            public string BaseSharedFxVersion { get; set; }
            public string RuntimeIdentifier { get; set; }
        }
    }
}
