// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GenerateBlazorWebAssemblyBootJsonTest
    {
        [Fact(Skip = "TODO fix this")]
        public void GroupsResourcesByType()
        {
            // Arrange
            var taskInstance = new GenerateBlazorWebAssemblyBootJson
            {
                AssemblyPath = "MyApp.Entrypoint.dll",
                Resources = new[]
                {
                    CreateResourceTaskItem(
                        "assembly",
                        name: "My.Assembly1.dll", // Can specify filename with no dir
                        fileHash: "abcdefghikjlmnopqrstuvwxyz"),

                    CreateResourceTaskItem(
                        "assembly",
                        name: "dir\\My.Assembly2.dll", // Can specify Windows-style path
                        fileHash: "012345678901234567890123456789"),

                    CreateResourceTaskItem(
                        "pdb",
                        name: "otherdir/SomePdb.pdb", // Can specify Linux-style path
                        fileHash: "pdbhashpdbhashpdbhash"),

                    CreateResourceTaskItem(
                        "pdb",
                        name: "My.Assembly1.pdb",
                        fileHash: "pdbdefghikjlmnopqrstuvwxyz"),

                    CreateResourceTaskItem(
                        "runtime",
                        name: "some-runtime-file", // Can specify path with no extension
                        fileHash: "runtimehashruntimehash"),

                    CreateResourceTaskItem(
                        "satellite",
                        name: "en-GB\\satellite-assembly1.dll",
                        fileHash: "hashsatelliteassembly1",
                        ("Culture", "en-GB")),

                    CreateResourceTaskItem(
                        "satellite",
                        name: "fr/satellite-assembly2.dll",
                        fileHash: "hashsatelliteassembly2",
                        ("Culture", "fr")),

                    CreateResourceTaskItem(
                        "satellite",
                        name: "en-GB\\satellite-assembly3.dll",
                        fileHash: "hashsatelliteassembly3",
                        ("Culture", "en-GB")),
                }
            };

            using var stream = new MemoryStream();

            // Act
            taskInstance.WriteBootJson(stream, "MyEntrypointAssembly");

            // Assert
            var parsedContent = ParseBootData(stream);
            Assert.Equal("MyEntrypointAssembly", parsedContent.entryAssembly);

            var resources = parsedContent.resources.assembly;
            Assert.Equal(2, resources.Count);
            Assert.Equal("sha256-abcdefghikjlmnopqrstuvwxyz", resources["My.Assembly1.dll"]);
            Assert.Equal("sha256-012345678901234567890123456789", resources["dir/My.Assembly2.dll"]); // Paths are converted to use URL-style separators

            resources = parsedContent.resources.pdb;
            Assert.Equal(2, resources.Count);
            Assert.Equal("sha256-pdbhashpdbhashpdbhash", resources["otherdir/SomePdb.pdb"]);
            Assert.Equal("sha256-pdbdefghikjlmnopqrstuvwxyz", resources["My.Assembly1.pdb"]);

            resources = parsedContent.resources.runtime;
            Assert.Single(resources);
            Assert.Equal("sha256-runtimehashruntimehash", resources["some-runtime-file"]);

            var satelliteResources = parsedContent.resources.satelliteResources;
            Assert.Collection(
                satelliteResources.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("en-GB", kvp.Key);
                    Assert.Collection(
                        kvp.Value.OrderBy(item => item.Key),
                        item =>
                        {
                            Assert.Equal("en-GB/satellite-assembly1.dll", item.Key);
                            Assert.Equal("sha256-hashsatelliteassembly1", item.Value);
                        },
                        item =>
                        {
                            Assert.Equal("en-GB/satellite-assembly3.dll", item.Key);
                            Assert.Equal("sha256-hashsatelliteassembly3", item.Value);
                        });
                },
                kvp =>
                {
                    Assert.Equal("fr", kvp.Key);
                    Assert.Collection(
                        kvp.Value.OrderBy(item => item.Key),
                        item =>
                        {
                            Assert.Equal("fr/satellite-assembly2.dll", item.Key);
                            Assert.Equal("sha256-hashsatelliteassembly2", item.Value);
                        });
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanSpecifyCacheBootResources(bool flagValue)
        {
            // Arrange
            var taskInstance = new GenerateBlazorWebAssemblyBootJson { CacheBootResources = flagValue };
            using var stream = new MemoryStream();

            // Act
            taskInstance.WriteBootJson(stream, "MyEntrypointAssembly");

            // Assert
            var parsedContent = ParseBootData(stream);
            Assert.Equal(flagValue, parsedContent.cacheBootResources);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanSpecifyDebugBuild(bool flagValue)
        {
            // Arrange
            var taskInstance = new GenerateBlazorWebAssemblyBootJson { DebugBuild = flagValue };
            using var stream = new MemoryStream();

            // Act
            taskInstance.WriteBootJson(stream, "MyEntrypointAssembly");

            // Assert
            var parsedContent = ParseBootData(stream);
            Assert.Equal(flagValue, parsedContent.debugBuild);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanSpecifyLinkerEnabled(bool flagValue)
        {
            // Arrange
            var taskInstance = new GenerateBlazorWebAssemblyBootJson { LinkerEnabled = flagValue };
            using var stream = new MemoryStream();

            // Act
            taskInstance.WriteBootJson(stream, "MyEntrypointAssembly");

            // Assert
            var parsedContent = ParseBootData(stream);
            Assert.Equal(flagValue, parsedContent.linkerEnabled);
        }

        private static BootJsonData ParseBootData(Stream stream)
        {
            stream.Position = 0;
            var serializer = new DataContractJsonSerializer(
                typeof(BootJsonData),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            return (BootJsonData)serializer.ReadObject(stream);
        }

        private static ITaskItem CreateResourceTaskItem(string type, string name, string fileHash, params (string key, string value)[] values)
        {
            var mock = new Mock<ITaskItem>();
            mock.Setup(m => m.GetMetadata("BootManifestResourceName")).Returns(name);
            mock.Setup(m => m.GetMetadata("Integrity")).Returns(fileHash);

            if (values != null)
            {
                foreach (var (key, value) in values)
                {
                    mock.Setup(m => m.GetMetadata(key)).Returns(value);
                }
            }
            return mock.Object;
        }
    }
}
