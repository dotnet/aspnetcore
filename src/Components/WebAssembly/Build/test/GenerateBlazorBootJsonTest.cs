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
                        itemSpec: Path.Combine("dir", "My.Assembly1.ext"), // Can specify item spec
                        relativeOutputPath: null,
                        fileHash: "abcdefghikjlmnopqrstuvwxyz"),

                    CreateResourceTaskItem(
                        ResourceType.assembly,
                        itemSpec: "Ignored",
                        relativeOutputPath: Path.Combine("dir", "My.Assembly2.ext2"), // Can specify relative path
                        fileHash: "012345678901234567890123456789"),

                    CreateResourceTaskItem(
                        ResourceType.pdb,
                        itemSpec: "SomePdb.pdb",
                        relativeOutputPath: null,
                        fileHash: "pdbhashpdbhashpdbhash"),

                    CreateResourceTaskItem(
                        ResourceType.runtime,
                        itemSpec: "some-runtime-file",
                        relativeOutputPath: null,
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
                    Assert.Equal("sha256-012345678901234567890123456789", resources["dir/My.Assembly2.ext2"]); // For relative paths, we preserve the whole relative path, but use URL-style separators
                },
                resourceListKey =>
                {
                    var resources = parsedContent.resources[resourceListKey];
                    Assert.Equal(ResourceType.pdb, resourceListKey);
                    Assert.Single(resources);
                    Assert.Equal("sha256-pdbhashpdbhashpdbhash", resources["SomePdb.pdb"]);
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

        private static ITaskItem CreateResourceTaskItem(ResourceType type, string itemSpec, string relativeOutputPath, string fileHash)
        {
            var mock = new Mock<ITaskItem>();
            mock.Setup(m => m.ItemSpec).Returns(itemSpec);
            mock.Setup(m => m.GetMetadata("TargetOutputPath")).Returns(itemSpec);
            mock.Setup(m => m.GetMetadata("BootResourceType")).Returns(type.ToString());
            mock.Setup(m => m.GetMetadata("RelativeOutputPath")).Returns(relativeOutputPath);
            mock.Setup(m => m.GetMetadata("FileHash")).Returns(fileHash);
            return mock.Object;
        }
    }
}
