// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Testing;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ConcatenateCssFilesTest
    {
        private static readonly string BundleContent =
@"/* _content/Test/TestFiles/Generated/Counter.razor.rz.scp.css */
.counter {
    font-size: 2rem;
}
/* _content/Test/TestFiles/Generated/Index.razor.rz.scp.css */
.index {
    font-weight: bold;
}
";

        private static readonly string BundleWithImportsContent =
@"@import '_content/Test/TestFiles/Generated/lib.bundle.scp.css';
@import 'TestFiles/Generated/package.bundle.scp.css';

/* _content/Test/TestFiles/Generated/Counter.razor.rz.scp.css */
.counter {
    font-size: 2rem;
}
/* _content/Test/TestFiles/Generated/Index.razor.rz.scp.css */
.index {
    font-weight: bold;
}
";

        private static readonly string UpdatedBundleContent =
@"/* _content/Test/TestFiles/Generated/Counter.razor.rz.scp.css */
.counter {
    font-size: 2rem;
}
/* _content/Test/TestFiles/Generated/FetchData.razor.rz.scp.css */
.fetchData {
    font-family: Helvetica;
}
/* _content/Test/TestFiles/Generated/Index.razor.rz.scp.css */
.index {
    font-weight: bold;
}
";

        [Fact]
        public void BundlesScopedCssFiles_ProducesEmpyBundleIfNoFilesAvailable()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = Array.Empty<ITaskItem>(),
                ProjectBundles = Array.Empty<ITaskItem>(),
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));

            Assert.Empty(File.ReadAllText(expectedFile));
        }

        [Fact]
        public void BundlesScopedCssFiles_ProducesBundle()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        }),
                },
                ProjectBundles = Array.Empty<ITaskItem>(),
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));

            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(BundleContent, actualContents, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void BundlesScopedCssFiles_IncludesOtherBundles()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string, string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string, string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        }),
                },
                ProjectBundles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/lib.bundle.scp.css",
                        new Dictionary<string, string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/lib.bundle.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/package.bundle.scp.css",
                        new Dictionary<string, string>
                        {
                            ["BasePath"] = "",
                            ["RelativePath"] = "TestFiles/Generated/package.bundle.scp.css",
                        }),
                },
                ScopedCssBundleBasePath = "/",
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));

            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(BundleWithImportsContent, actualContents, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData("", "", "TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/", "/", "TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app", "_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app", "/_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app", "/_content/", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/app", "_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/app", "/_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/app", "/_content/", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app/", "_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app/", "/_content", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("app/", "/_content/", "../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/company/app/", "_content", "../../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/company/app/", "/_content", "../../_content/TestFiles/Generated/lib.bundle.scp.css")]
        [InlineData("/company/app/", "/_content/", "../../_content/TestFiles/Generated/lib.bundle.scp.css")]

        public void BundlesScopedCssFiles_HandlesBasePathCombinationsCorrectly(string finalBasePath, string libraryBasePath, string expectedImport)
        {
            // Arrange
            var expectedContent = BundleWithImportsContent
                .Replace("_content/Test/TestFiles/Generated/lib.bundle.scp.css", expectedImport)
                .Replace("@import 'TestFiles/Generated/package.bundle.scp.css';", "")
                .Replace("\r\n", "\n")
                .Replace("\n\n", "\n");

            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        })
                },
                ProjectBundles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/lib.bundle.scp.css",
                        new Dictionary<string, string>
                        {
                            ["BasePath"] = libraryBasePath,
                            ["RelativePath"] = "TestFiles/Generated/lib.bundle.scp.css",
                        }),
                },
                ScopedCssBundleBasePath = finalBasePath,
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));

            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(expectedContent, actualContents, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void BundlesScopedCssFiles_BundlesFilesInOrder()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                },
                ProjectBundles = Array.Empty<ITaskItem>(),
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));

            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(BundleContent, actualContents, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void BundlesScopedCssFiles_DoesNotOverrideBundleForSameContents()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                },
                ProjectBundles = Array.Empty<ITaskItem>(),
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            var lastModified = File.GetLastWriteTimeUtc(expectedFile);

            taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));
            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(BundleContent, actualContents, ignoreLineEndingDifferences: true);

            Assert.Equal(lastModified, File.GetLastWriteTimeUtc(expectedFile));
        }

        [Fact]
        public async System.Threading.Tasks.Task BundlesScopedCssFiles_UpdatesBundleWhenContentsChange()
        {
            // Arrange
            var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid():N}.css");
            var taskInstance = new ConcatenateCssFiles()
            {
                ScopedCssFiles = new[]
                {
                    new TaskItem(
                        "TestFiles/Generated/Index.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                        }),
                    new TaskItem(
                        "TestFiles/Generated/Counter.razor.rz.scp.css",
                        new Dictionary<string,string>
                        {
                            ["BasePath"] = "_content/Test/",
                            ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                        }),
                },
                ProjectBundles = Array.Empty<ITaskItem>(),
                OutputFile = expectedFile
            };

            // Act
            var result = taskInstance.Execute();

            var lastModified = File.GetLastWriteTimeUtc(expectedFile);

            taskInstance.ScopedCssFiles = new[]
            {
                new TaskItem(
                    "TestFiles/Generated/Index.razor.rz.scp.css",
                    new Dictionary<string,string>
                    {
                        ["BasePath"] = "_content/Test/",
                        ["RelativePath"] = "TestFiles/Generated/Index.razor.rz.scp.css",
                    }),
                new TaskItem(
                    "TestFiles/Generated/Counter.razor.rz.scp.css",
                    new Dictionary<string,string>
                    {
                        ["BasePath"] = "_content/Test/",
                        ["RelativePath"] = "TestFiles/Generated/Counter.razor.rz.scp.css",
                    }),
                new TaskItem(
                    "TestFiles/Generated/FetchData.razor.rz.scp.css",
                    new Dictionary<string,string>
                    {
                        ["BasePath"] = "_content/Test/",
                        ["RelativePath"] = "TestFiles/Generated/FetchData.razor.rz.scp.css",
                    }),
            };

            await System.Threading.Tasks.Task.Delay(1000);
            taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(expectedFile));
            var actualContents = File.ReadAllText(expectedFile);
            Assert.Equal(UpdatedBundleContent, actualContents, ignoreLineEndingDifferences: true);
            Assert.NotEqual(lastModified, File.GetLastWriteTimeUtc(expectedFile));
        }
    }
}
