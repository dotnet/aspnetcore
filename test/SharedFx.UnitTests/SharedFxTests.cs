// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using TriageBuildFailures;
using Xunit;

namespace Microsoft.AspNetCore
{
    public class SharedFxTests
    {
        [Theory]
        [MemberData(nameof(GetSharedFxConfig))]
        public async Task BaselineTest(SharedFxConfig config)
        {
            if (!TestData.GetValidateBaseline())
            {
                // Inability to validate the package baselines indicates dotnetcli is not up-to-date.
                return;
            }

            var previousVersion = TestData.GetPreviousAspNetCoreReleaseVersion();
            var url = new Uri($"https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/" + previousVersion + "/aspnetcore-runtime-internal-" + previousVersion + "-win-x64.zip");
            var zipName = "assemblies.zip";
            var nugetAssemblyVersions = new Dictionary<string, Version>();
            var root = TestData.GetDotNetRoot();
            var dir = Path.Combine(root, "shared", config.Name, config.Version);

            using (var testClient = new WebClient())
            {
                var reporter = new ConsoleReporter(PhysicalConsole.Singleton);
                await RetryHelpers.RetryAsync(async () => await testClient.DownloadFileTaskAsync(url, zipName), reporter);
            }

            var zipPath = Path.Combine(AppContext.BaseDirectory, zipName);
            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                Directory.CreateDirectory(tempDirectoryPath);
                ZipFile.ExtractToDirectory(zipPath, tempDirectoryPath);
                var nugetAssembliesPath = Path.Combine(tempDirectoryPath, "shared", config.Name, previousVersion);

                var files = Directory.GetFiles(nugetAssembliesPath, "*.dll");
                foreach (var file in files)
                {
                    try
                    {
                        var assemblyVersion = AssemblyName.GetAssemblyName(file).Version;
                        var dllName = Path.GetFileName(file);
                        nugetAssemblyVersions.Add(dllName, assemblyVersion);
                    }
                    catch (BadImageFormatException) { }
                }

                files = Directory.GetFiles(dir, "*.dll");

                Assert.All(files, file =>
                {
                    try
                    {
                        var localAssemblyVersion = AssemblyName.GetAssemblyName(file).Version;
                        var dllName = Path.GetFileName(file);
                        Assert.Contains(dllName, nugetAssemblyVersions.Keys);
                        Assert.True(localAssemblyVersion >= nugetAssemblyVersions[dllName], $"Expected {dllName} ({localAssemblyVersion}) to have assembly version >= {nugetAssemblyVersions[dllName]}");
                    }
                    catch (BadImageFormatException) { }

                });
            }
            finally
            {
                Directory.Delete(tempDirectoryPath, true);
            }
        }

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

            var targetLibraries = depsFile["targets"][target];
            Assert.All(targetLibraries, libEntry =>
            {
                var lib = Assert.IsType<JProperty>(libEntry);
                if (lib.Value["runtime"] == null)
                {
                    return;
                }

                Assert.All(lib.Value["runtime"], item =>
                {
                    var obj = Assert.IsType<JProperty>(item);
                    var assemblyVersion = obj.Value["assemblyVersion"];
                    Assert.NotNull(assemblyVersion);
                    Assert.NotEmpty(assemblyVersion.Value<string>());

                    var fileVersion = obj.Value["fileVersion"];
                    Assert.NotNull(fileVersion);
                    Assert.NotEmpty(fileVersion.Value<string>());
                });
            });
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
