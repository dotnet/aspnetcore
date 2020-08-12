// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ValidateStaticWebAssetsUniquePathsTest
    {
        [Fact]
        public void ReturnsError_WhenStaticWebAssetsWebRootPathMatchesExistingContentItemPath()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new ValidateStaticWebAssetsUniquePaths
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine(".", "Library", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["BasePath"] = "/",
                        ["RelativePath"] = "/sample.js",
                    })
                },
                WebRootFiles = new TaskItem[]
                {
                    CreateItem(Path.Combine(".", "App", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["TargetPath"] = "/SAMPLE.js",
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"The static web asset '{Path.Combine(".", "Library", "wwwroot", "sample.js")}' has a conflicting web root path '/SAMPLE.js' with the project file '{Path.Combine(".", "App", "wwwroot", "sample.js")}'.", message);
        }

        [Fact]
        public void ReturnsError_WhenMultipleStaticWebAssetsHaveTheSameWebRootPath()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new ValidateStaticWebAssetsUniquePaths
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine(".", "Library", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["BasePath"] = "/",
                        ["RelativePath"] = "/sample.js",
                    }),
                    CreateItem(Path.Combine(".", "Library", "bin", "dist", "sample.js"), new Dictionary<string,string>
                    {
                        ["BasePath"] = "/",
                        ["RelativePath"] = "/sample.js",
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Conflicting assets with the same path '/sample.js' for content root paths '{Path.Combine(".", "Library", "bin", "dist", "sample.js")}' and '{Path.Combine(".", "Library", "wwwroot", "sample.js")}'.", message);
        }

        [Fact]
        public void ReturnsSuccess_WhenStaticWebAssetsDontConflictWithApplicationContentItems()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();

            var task = new ValidateStaticWebAssetsUniquePaths
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine(".", "Library", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["BasePath"] = "/_library",
                        ["RelativePath"] = "/sample.js",
                    }),
                    CreateItem(Path.Combine(".", "Library", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["BasePath"] = "/_library",
                        ["RelativePath"] = "/sample.js",
                    })
                },
                WebRootFiles = new TaskItem[]
                {
                    CreateItem(Path.Combine(".", "App", "wwwroot", "sample.js"), new Dictionary<string,string>
                    {
                        ["TargetPath"] = "/SAMPLE.js",
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
        }

        private static TaskItem CreateItem(
            string spec,
            IDictionary<string, string> metadata)
        {
            var result = new TaskItem(spec);
            foreach (var (key, value) in metadata)
            {
                result.SetMetadata(key, value);
            }

            return result;
        }
    }
}
