// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders.Embedded.Manifest;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public class ManifestEmbeddedFileProviderTests
    {
        [Fact]
        public void GetFileInfo_CanResolveSimpleFiles()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.File("jquery.validate.js"),
                    TestEntry.File("jquery.min.js"),
                    TestEntry.File("site.css")));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var jqueryValidate = provider.GetFileInfo("jquery.validate.js");
            Assert.True(jqueryValidate.Exists);
            Assert.False(jqueryValidate.IsDirectory);
            Assert.Equal("jquery.validate.js", jqueryValidate.Name);
            Assert.Null(jqueryValidate.PhysicalPath);
            Assert.Equal(0, jqueryValidate.Length);

            var jqueryMin = provider.GetFileInfo("jquery.min.js");
            Assert.True(jqueryMin.Exists);
            Assert.False(jqueryMin.IsDirectory);
            Assert.Equal("jquery.min.js", jqueryMin.Name);
            Assert.Null(jqueryMin.PhysicalPath);
            Assert.Equal(0, jqueryMin.Length);

            var siteCss = provider.GetFileInfo("site.css");
            Assert.True(siteCss.Exists);
            Assert.False(siteCss.IsDirectory);
            Assert.Equal("site.css", siteCss.Name);
            Assert.Null(siteCss.PhysicalPath);
            Assert.Equal(0, siteCss.Length);
        }

        [Fact]
        public void GetFileInfo_CanResolveFilesInsideAFolder()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var jqueryValidate = provider.GetFileInfo(Path.Combine("wwwroot", "jquery.validate.js"));
            Assert.True(jqueryValidate.Exists);
            Assert.False(jqueryValidate.IsDirectory);
            Assert.Equal("jquery.validate.js", jqueryValidate.Name);
            Assert.Null(jqueryValidate.PhysicalPath);
            Assert.Equal(0, jqueryValidate.Length);

            var jqueryMin = provider.GetFileInfo(Path.Combine("wwwroot", "jquery.min.js"));
            Assert.True(jqueryMin.Exists);
            Assert.False(jqueryMin.IsDirectory);
            Assert.Equal("jquery.min.js", jqueryMin.Name);
            Assert.Null(jqueryMin.PhysicalPath);
            Assert.Equal(0, jqueryMin.Length);

            var siteCss = provider.GetFileInfo(Path.Combine("wwwroot", "site.css"));
            Assert.True(siteCss.Exists);
            Assert.False(siteCss.IsDirectory);
            Assert.Equal("site.css", siteCss.Name);
            Assert.Null(siteCss.PhysicalPath);
            Assert.Equal(0, siteCss.Length);
        }

        [Fact]
        public void GetFileInfo_ResolveNonExistingFile_ReturnsNotFoundFileInfo()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Act
            var file = provider.GetFileInfo("some/non/existing/file.txt");

            // Assert
            Assert.IsType<NotFoundFileInfo>(file);
        }

        [Fact]
        public void GetFileInfo_ResolveNonExistingDirectory_ReturnsNotFoundFileInfo()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Act
            var file = provider.GetFileInfo("some");

            // Assert
            Assert.IsType<NotFoundFileInfo>(file);
        }

        [Fact]
        public void GetFileInfo_ResolveExistingDirectory_ReturnsNotFoundFileInfo()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Act
            var file = provider.GetFileInfo("wwwroot");

            // Assert
            Assert.IsType<NotFoundFileInfo>(file);
        }

        [Theory]
        [InlineData("WWWROOT", "JQUERY.VALIDATE.JS")]
        [InlineData("WwWRoOT", "JQuERY.VALiDATE.js")]
        public void GetFileInfo_ResolvesFiles_WithDifferentCasing(string folder, string file)
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var jqueryValidate = provider.GetFileInfo(Path.Combine(folder, file));
            Assert.True(jqueryValidate.Exists);
            Assert.False(jqueryValidate.IsDirectory);
            Assert.Equal("jquery.validate.js", jqueryValidate.Name);
            Assert.Null(jqueryValidate.PhysicalPath);
            Assert.Equal(0, jqueryValidate.Length);
        }

        [Fact]
        public void GetFileInfo_AllowsLeadingDots_OnThePath()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var jqueryValidate = provider.GetFileInfo(Path.Combine(".", "wwwroot", "jquery.validate.js"));
            Assert.True(jqueryValidate.Exists);
            Assert.False(jqueryValidate.IsDirectory);
            Assert.Equal("jquery.validate.js", jqueryValidate.Name);
            Assert.Null(jqueryValidate.PhysicalPath);
            Assert.Equal(0, jqueryValidate.Length);
        }

        [Fact]
        public void GetFileInfo_EscapingFromTheRootFolder_ReturnsNotFound()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var jqueryValidate = provider.GetFileInfo(Path.Combine("..", "wwwroot", "jquery.validate.js"));
            Assert.IsType<NotFoundFileInfo>(jqueryValidate);
        }

        [Theory]
        [InlineData("wwwroot/jquery?validate.js")]
        [InlineData("wwwroot/jquery*validate.js")]
        [InlineData("wwwroot/jquery:validate.js")]
        [InlineData("wwwroot/jquery<validate.js")]
        [InlineData("wwwroot/jquery>validate.js")]
        [InlineData("wwwroot/jquery\0validate.js")]
        public void GetFileInfo_ReturnsNotFoundfileInfo_ForPathsWithInvalidCharacters(string path)
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var file = provider.GetFileInfo(path);
            Assert.IsType<NotFoundFileInfo>(file);
            Assert.Equal(path, file.Name);
        }

        [Fact]
        public void GetDirectoryContents_CanEnumerateExistingFolders()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            var expectedContents = new[]
            {
                CreateTestFileInfo("jquery.validate.js"),
                CreateTestFileInfo("jquery.min.js"),
                CreateTestFileInfo("site.css")
            };

            // Act
            var contents = provider.GetDirectoryContents("wwwroot").ToArray();

            // Assert
            Assert.Equal(expectedContents, contents, FileInfoComparer.Instance);
        }

        [Fact]
        public void GetDirectoryContents_EnumeratesOnlyAGivenLevel()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            var expectedContents = new[]
            {
                CreateTestFileInfo("wwwroot", isDirectory: true)
            };

            // Act
            var contents = provider.GetDirectoryContents(".").ToArray();

            // Assert
            Assert.Equal(expectedContents, contents, FileInfoComparer.Instance);
        }

        [Fact]
        public void GetDirectoryContents_EnumeratesFilesAndDirectoriesOnAGivenPath()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot"),
                    TestEntry.File("site.css")));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            var expectedContents = new[]
            {
                CreateTestFileInfo("wwwroot", isDirectory: true),
                CreateTestFileInfo("site.css")
            };

            // Act
            var contents = provider.GetDirectoryContents(".").ToArray();

            // Assert
            Assert.Equal(expectedContents, contents, FileInfoComparer.Instance);
        }

        [Fact]
        public void GetDirectoryContents_ReturnsNoEntries_ForNonExistingDirectories()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot"),
                    TestEntry.File("site.css")));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Act
            var contents = provider.GetDirectoryContents("non-existing");

            // Assert
            Assert.IsType<NotFoundDirectoryContents>(contents);
        }

        [Fact]
        public void GetDirectoryContents_ReturnsNoEntries_ForFilePaths()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot"),
                    TestEntry.File("site.css")));

            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Act
            var contents = provider.GetDirectoryContents("site.css");

            // Assert
            Assert.IsType<NotFoundDirectoryContents>(contents);
        }

        [Theory]
        [InlineData("wwwro*t")]
        [InlineData("wwwro?t")]
        [InlineData("wwwro:t")]
        [InlineData("wwwro<t")]
        [InlineData("wwwro>t")]
        [InlineData("wwwro\0t")]
        public void GetDirectoryContents_ReturnsNotFoundDirectoryContents_ForPathsWithInvalidCharacters(string path)
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js"),
                        TestEntry.File("jquery.min.js"),
                        TestEntry.File("site.css"))));

            // Act
            var provider = new ManifestEmbeddedFileProvider(assembly);

            // Assert
            var directory = provider.GetDirectoryContents(path);
            Assert.IsType<NotFoundDirectoryContents>(directory);
        }

        [Fact]
        public void Contructor_CanScopeManifestToAFolder()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js")),
                    TestEntry.File("site.css")));

            var provider = new ManifestEmbeddedFileProvider(assembly);
            var scopedProvider = new ManifestEmbeddedFileProvider(assembly, provider.Manifest.Scope("wwwroot"), DateTimeOffset.UtcNow);

            // Act
            var jqueryValidate = scopedProvider.GetFileInfo("jquery.validate.js");

            // Assert
            Assert.True(jqueryValidate.Exists);
            Assert.False(jqueryValidate.IsDirectory);
            Assert.Equal("jquery.validate.js", jqueryValidate.Name);
            Assert.Null(jqueryValidate.PhysicalPath);
            Assert.Equal(0, jqueryValidate.Length);          
        }

        [Theory]
        [InlineData("wwwroot/jquery.validate.js")]
        [InlineData("../wwwroot/jquery.validate.js")]
        [InlineData("site.css")]
        [InlineData("../site.css")]
        public void ScopedFileProvider_DoesNotReturnFilesOutOfScope(string path)
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.Directory("wwwroot",
                        TestEntry.File("jquery.validate.js")),
                    TestEntry.File("site.css")));

            var provider = new ManifestEmbeddedFileProvider(assembly);
            var scopedProvider = new ManifestEmbeddedFileProvider(assembly, provider.Manifest.Scope("wwwroot"), DateTimeOffset.UtcNow);

            // Act
            var jqueryValidate = scopedProvider.GetFileInfo(path);

            // Assert
            Assert.IsType<NotFoundFileInfo>(jqueryValidate);
        }

        private IFileInfo CreateTestFileInfo(string name, bool isDirectory = false) =>
            new TestFileInfo(name, isDirectory);
    }
}
