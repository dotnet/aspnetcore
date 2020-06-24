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
    public class GenerateStaticWebAssetsPropsFileTest
    {
        [Fact]
        public void Fails_WhenStaticWebAsset_DoesNotContainSourceType()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Missing required metadata 'SourceType' for '{Path.Combine("wwwroot", "js", "sample.js")}'.", message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_DoesNotContainSourceId()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Missing required metadata 'SourceId' for '{Path.Combine("wwwroot", "js", "sample.js")}'.", message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_DoesNotContainContentRoot()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Missing required metadata 'ContentRoot' for '{Path.Combine("wwwroot", "js", "sample.js")}'.", message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_DoesNotContainBasePath()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Missing required metadata 'BasePath' for '{Path.Combine("wwwroot", "js", "sample.js")}'.", message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_DoesNotContainRelativePath()
        {
            // Arrange
            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal($"Missing required metadata 'RelativePath' for '{Path.Combine("wwwroot", "js", "sample.js")}'.", message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_HaveDifferentSourceType()
        {
            // Arrange
            var expectedError = "Static web assets have different 'SourceType' metadata values " +
                "'' and 'Package' " +
                $"for '{Path.Combine("wwwroot", "js", "sample.js")}' and '{Path.Combine("wwwroot", "css", "site.css")}'.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    }),
                    CreateItem(Path.Combine("wwwroot","css","site.css"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "Package",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("css", "site.css"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_HaveDifferentSourceId()
        {
            // Arrange
            var expectedError = "Static web assets have different 'SourceId' metadata values " +
                "'MyLibrary' and 'MyLibrary2' " +
                $"for '{Path.Combine("wwwroot", "js", "sample.js")}' and '{Path.Combine("wwwroot", "css", "site.css")}'.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    }),
                    CreateItem(Path.Combine("wwwroot","css","site.css"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary2",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("css", "site.css"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_HaveDifferentContentRoot()
        {
            // Arrange
            var expectedError = "Static web assets have different 'ContentRoot' metadata values " +
                @"'$(MSBuildThisFileDirectory)..\staticwebassets' and '..\staticwebassets' " +
                $"for '{Path.Combine("wwwroot", "js", "sample.js")}' and '{Path.Combine("wwwroot", "css", "site.css")}'.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    }),
                    CreateItem(Path.Combine("wwwroot","css","site.css"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("css", "site.css"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Fact]
        public void Fails_WhenStaticWebAsset_HaveDifferentBasePath()
        {
            // Arrange
            var expectedError = "Static web assets have different 'BasePath' metadata values " +
                "'_content/mylibrary' and '_content/mylibrary2' " +
                $"for '{Path.Combine("wwwroot", "js", "sample.js")}' and '{Path.Combine("wwwroot", "css", "site.css")}'.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GenerateStaticWebAsssetsPropsFile
            {
                BuildEngine = buildEngine.Object,
                StaticWebAssets = new TaskItem[]
                {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    }),
                    CreateItem(Path.Combine("wwwroot","css","site.css"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary2",
                        ["RelativePath"] = Path.Combine("css", "site.css"),
                    })
                }
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Fact]
        public void WritesPropsFile_WhenThereIsAtLeastOneStaticAsset()
        {
            // Arrange
            var file = Path.GetTempFileName();
            var expectedDocument = @"<Project>
  <ItemGroup>
    <StaticWebAsset Include=""$(MSBuildThisFileDirectory)..\staticwebassets\**"">
      <SourceType>Package</SourceType>
      <SourceId>MyLibrary</SourceId>
      <ContentRoot>$(MSBuildThisFileDirectory)..\staticwebassets\</ContentRoot>
      <BasePath>_content/mylibrary</BasePath>
      <RelativePath>%(RecursiveDir)%(FileName)%(Extension)</RelativePath>
    </StaticWebAsset>
  </ItemGroup>
</Project>";

            try
            {
                var buildEngine = new Mock<IBuildEngine>();

                var task = new GenerateStaticWebAsssetsPropsFile
                {
                    BuildEngine = buildEngine.Object,
                    TargetPropsFilePath = file,
                    StaticWebAssets = new TaskItem[]
                    {
                    CreateItem(Path.Combine("wwwroot","js","sample.js"), new Dictionary<string,string>
                    {
                        ["SourceType"] = "",
                        ["SourceId"] = "MyLibrary",
                        ["ContentRoot"] = @"$(MSBuildThisFileDirectory)..\staticwebassets",
                        ["BasePath"] = "_content/mylibrary",
                        ["RelativePath"] = Path.Combine("js", "sample.js"),
                    }),
                    }
                };

                // Act
                var result = task.Execute();

                // Assert
                Assert.True(result);
                var document = File.ReadAllText(file);
                Assert.Equal(expectedDocument, document, ignoreLineEndingDifferences: true);
            }
            finally
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
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
