// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Build.Framework;
using Moq;
using Xunit;
using BootJsonData = Microsoft.AspNetCore.Components.WebAssembly.Build.GenerateBlazorBootJson.BootJsonData;
using ResourceType = Microsoft.AspNetCore.Components.WebAssembly.Build.GenerateBlazorBootJson.ResourceType;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class GenerateBlazorBootJsonTest
    {
        [Fact]
        public void GroupsResourcesByType()
        {
            // Arrange
            var taskInstance = new GenerateBlazorBootJson
            {
                AssemblyPath = "MyApp.Entrypoint.dll",
                Resources = new[]
                {
                    CreateResourceTaskItem(
                        ResourceType.assembly,
                        name: "My.Assembly1.ext", // Can specify filename with no dir
                        fileHash: "abcdefghikjlmnopqrstuvwxyz"),

                    CreateResourceTaskItem(
                        ResourceType.assembly,
                        name: "dir\\My.Assembly2.ext2", // Can specify Windows-style path
                        fileHash: "012345678901234567890123456789"),

                    CreateResourceTaskItem(
                        ResourceType.pdb,
                        name: "otherdir/SomePdb.pdb", // Can specify Linux-style path
                        fileHash: "pdbhashpdbhashpdbhash"),

                    CreateResourceTaskItem(
                        ResourceType.runtime,
                        name: "some-runtime-file", // Can specify path with no extension
                        fileHash: "runtimehashruntimehash")
                }
            };

            using var stream = new MemoryStream();

            // Act
            taskInstance.WriteBootJson(stream, "MyEntrypointAssembly");

            // Assert
            var parsedContent = ParseBootData(stream);
            Assert.Equal("MyEntrypointAssembly", parsedContent.entryAssembly);
            Assert.Collection(parsedContent.resources.Keys,
                resourceListKey =>
                {
                    var resources = parsedContent.resources[resourceListKey];
                    Assert.Equal(ResourceType.assembly, resourceListKey);
                    Assert.Equal(2, resources.Count);
                    Assert.Equal("sha256-abcdefghikjlmnopqrstuvwxyz", resources["My.Assembly1.ext"]);
                    Assert.Equal("sha256-012345678901234567890123456789", resources["dir/My.Assembly2.ext2"]); // Paths are converted to use URL-style separators
                },
                resourceListKey =>
                {
                    var resources = parsedContent.resources[resourceListKey];
                    Assert.Equal(ResourceType.pdb, resourceListKey);
                    Assert.Single(resources);
                    Assert.Equal("sha256-pdbhashpdbhashpdbhash", resources["otherdir/SomePdb.pdb"]);
                },
                resourceListKey =>
                {
                    var resources = parsedContent.resources[resourceListKey];
                    Assert.Equal(ResourceType.runtime, resourceListKey);
                    Assert.Single(resources);
                    Assert.Equal("sha256-runtimehashruntimehash", resources["some-runtime-file"]);
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanSpecifyCacheBootResources(bool flagValue)
        {
            // Arrange
            var taskInstance = new GenerateBlazorBootJson { CacheBootResources = flagValue };
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
            var taskInstance = new GenerateBlazorBootJson { DebugBuild = flagValue };
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
            var taskInstance = new GenerateBlazorBootJson { LinkerEnabled = flagValue };
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

        private static ITaskItem CreateResourceTaskItem(ResourceType type, string name, string fileHash)
        {
            var mock = new Mock<ITaskItem>();
            mock.Setup(m => m.GetMetadata("BootManifestResourceType")).Returns(type.ToString());
            mock.Setup(m => m.GetMetadata("BootManifestResourceName")).Returns(name);
            mock.Setup(m => m.GetMetadata("FileHash")).Returns(fileHash);
            return mock.Object;
        }
    }
}
