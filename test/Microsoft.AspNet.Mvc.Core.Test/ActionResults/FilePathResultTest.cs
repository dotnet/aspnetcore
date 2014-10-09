// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class FilePathResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange & Act
            var path = Path.GetFullPath("helllo.txt");
            var result = new FilePathResult(path, "text/plain");

            // Act & Assert
            Assert.Equal(path, result.FileName);
        }

        [Fact]
        public async Task ExecuteResultAsync_FallsbackToStreamCopy_IfNoIHttpSendFilePresent()
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("."));
            var result = new FilePathResult(path, "text/plain", fileSystem);

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents", contents);
        }

        [Fact]
        public async Task ExecuteResultAsync_FallsBackToThePhysicalFileSystem_IfNoFileSystemIsPresent()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new FilePathResult(path, "text/plain");

            var appEnvironment = new Mock<IHostingEnvironment>();
            appEnvironment.Setup(app => app.WebRoot)
                .Returns(Directory.GetCurrentDirectory());

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = new ServiceCollection()
                .AddInstance<IHostingEnvironment>(appEnvironment.Object)
                .BuildServiceProvider();

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents", contents);
        }

        [Fact]
        public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("."));
            var result = new FilePathResult(path, "text/plain", fileSystem);

            var sendFileMock = new Mock<IHttpSendFileFeature>();
            sendFileMock
                .Setup(s => s.SendFileAsync(path, 0, null, CancellationToken.None))
                .Returns(Task.FromResult<int>(0));

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature(sendFileMock.Object);

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            sendFileMock.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithAbsolutePaths_UsingBackSlash()
        {
            // Arrange
            // path will be C:\...\TestFiles\FilePathResultTestFile.txt
            var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt"));
            // We want ot ensure that the path that we provide has backslashes to ensure they get normalized into
            // forward slashes.
            path = path.Replace('/', '\\');

            // Point the FileSystemRoot to a subfolder
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("Utils"));
            var result = new FilePathResult(path, "text/plain", fileSystem);

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents", contents);
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithAbsolutePaths_UsingForwardSlash()
        {
            // Arrange
            // path will be C:/.../TestFiles/FilePathResultTestFile.txt
            var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt"));
            path = path.Replace(@"\", "/");

            // Point the FileSystemRoot to a subfolder
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("Utils"));
            var result = new FilePathResult(path, "text/plain", fileSystem);

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents", contents);
        }

        [Theory]
        // Root of the file system, forward slash and back slash
        [InlineData("FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("/FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        // Paths with subfolders and mixed slash kinds
        [InlineData("/SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("/SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        // '.' has no special meaning
        [InlineData("./FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData(".\\FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("./SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("./SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        // Traverse to the parent directory and back to the file system directory
        [InlineData("..\\TestFiles/FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles\\FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        // '~/' and '~\' mean the application root folder
        [InlineData("~/FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("~/SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]       
        [InlineData("~/SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        public void GetFilePath_Resolves_RelativePaths(string path, string relativePathToFile)
        {
            // Arrange
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("./TestFiles"));
            var expectedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePathToFile));
            var filePathResult = new FilePathResult(path, "text/plain", fileSystem);

            // Act
            var result = filePathResult.ResolveFilePath(fileSystem);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Theory]
        [InlineData("~\\FilePathResultTestFile.txt", "TestFiles/FilePathResultTestFile.txt")]
        [InlineData("~\\SubFolder\\SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("~\\SubFolder/SubFolderTestFile.txt", "TestFiles/SubFolder/SubFolderTestFile.txt")]
        public void GetFilePath_FailsToResolve_InvalidVirtualPaths(string path, string relativePathToFile)
        {
            // Arrange
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("./TestFiles"));
            var expectedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePathToFile));
            var filePathResult = new FilePathResult(path, "text/plain", fileSystem);

            // Act
            var ex = Assert.Throws<FileNotFoundException>(() => filePathResult.ResolveFilePath(fileSystem));

            // Assert
            Assert.Equal("Could not find file: " + path, ex.Message);
            Assert.Equal(path, ex.FileName);
        }

        [Theory]
        // Root of the file system, forward slash and back slash
        [InlineData("FilePathResultTestFile.txt")]
        [InlineData("/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt")]
        // Paths with subfolders and mixed slash kinds
        [InlineData("/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("\\SubFolder/SubFolderTestFile.txt")]
        // '.' has no special meaning
        [InlineData("./FilePathResultTestFile.txt")]
        [InlineData(".\\FilePathResultTestFile.txt")]
        [InlineData("./SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("./SubFolder\\SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder/SubFolderTestFile.txt")]
        // Traverse to the parent directory and back to the file system directory
        [InlineData("..\\TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles\\FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt")]
        // '~/' and '~\' mean the application root folder
        [InlineData("~/FilePathResultTestFile.txt")]
        [InlineData("~/SubFolder/SubFolderTestFile.txt")]
        [InlineData("~/SubFolder\\SubFolderTestFile.txt")]
        public void GetFilePath_ThrowsFileNotFound_IfItCanNotFindTheFile(string path)
        {
            // Arrange

            // Point the IFileSystem root to a different subfolder
            var fileSystem = new PhysicalFileSystem(Path.GetFullPath("./Utils"));
            var filePathResult = new FilePathResult(path, "text/plain", fileSystem);
            var expectedFileName = path.TrimStart('~').Replace('\\', '/');
            var expectedMessage = "Could not find file: " + expectedFileName;

            // Act
            var ex = Assert.Throws<FileNotFoundException>(() => filePathResult.ResolveFilePath(fileSystem));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
            Assert.Equal(expectedFileName, ex.FileName);
        }

        [Theory]
        [InlineData("FilePathResultTestFile.txt", "FilePathResultTestFile.txt")]
        [InlineData("/FilePathResultTestFile.txt", "/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt", "/FilePathResultTestFile.txt")]
        // Paths with subfolders and mixed slash kinds
        [InlineData("/SubFolder/SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder\\SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        [InlineData("/SubFolder\\SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder/SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        // '.' has no special meaning
        [InlineData("./FilePathResultTestFile.txt", "./FilePathResultTestFile.txt")]
        [InlineData(".\\FilePathResultTestFile.txt", "./FilePathResultTestFile.txt")]
        [InlineData("./SubFolder/SubFolderTestFile.txt", "./SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder\\SubFolderTestFile.txt", "./SubFolder/SubFolderTestFile.txt")]
        [InlineData("./SubFolder\\SubFolderTestFile.txt", "./SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder/SubFolderTestFile.txt", "./SubFolder/SubFolderTestFile.txt")]
        // Traverse to the parent directory and back to the file system directory
        [InlineData("..\\TestFiles/FilePathResultTestFile.txt", "../TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles\\FilePathResultTestFile.txt", "../TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt", "../TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt", "../TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt", "../TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt", "../TestFiles/SubFolder/SubFolderTestFile.txt")]
        // Absolute paths
        [InlineData("C:\\Folder\\SubFolder\\File.txt", "C:/Folder/SubFolder/File.txt")]
        [InlineData("C:/Folder/SubFolder/File.txt", "C:/Folder/SubFolder/File.txt")]
        [InlineData("\\\\NetworkLocation\\Folder\\SubFolder\\File.txt", "//NetworkLocation/Folder/SubFolder/File.txt")]
        [InlineData("//NetworkLocation/Folder/SubFolder/File.txt", "//NetworkLocation/Folder/SubFolder/File.txt")]
        public void NormalizePath_ConvertsBackSlashes_IntoForwardSlashes(string path, string expectedPath)
        {
            // Arrange
            var fileResult = new FilePathResult(path, "text/plain", Mock.Of<IFileSystem>());

            // Act
            var normalizedPath = fileResult.NormalizePath(path);

            // Assert
            Assert.Equal(expectedPath, normalizedPath);
        }

        [Theory]
        // '~/' and '~\' mean the application root folder
        [InlineData("~/FilePathResultTestFile.txt", "/FilePathResultTestFile.txt")]
        [InlineData("~/SubFolder/SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        [InlineData("~/SubFolder\\SubFolderTestFile.txt", "/SubFolder/SubFolderTestFile.txt")]
        public void NormalizePath_ConvertsVirtualPaths_IntoRelativePaths(string path, string expectedPath)
        {
            // Arrange
            var fileResult = new FilePathResult(path, "text/plain", Mock.Of<IFileSystem>());

            // Act
            var normalizedPath = fileResult.NormalizePath(path);

            // Assert
            Assert.Equal(expectedPath, normalizedPath);
        }

        [Theory]
        // '~/' and '~\' mean the application root folder
        [InlineData("~\\FilePathResultTestFile.txt")]
        [InlineData("~\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("~\\SubFolder/SubFolderTestFile.txt")]
        public void NormalizePath_DoesNotConvert_InvalidVirtualPathsIntoRelativePaths(string path)
        {
            // Arrange
            var fileResult = new FilePathResult(path, "text/plain", Mock.Of<IFileSystem>());

            // Act
            var normalizedPath = fileResult.NormalizePath(path);

            // Assert
            Assert.Equal(path, normalizedPath);
        }

        [Theory]
        [InlineData("C:\\Folder\\SubFolder\\File.txt")]
        [InlineData("C:/Folder/SubFolder/File.txt")]
        [InlineData("\\\\NetworkLocation\\Folder\\SubFolder\\File.txt")]
        [InlineData("//NetworkLocation/Folder/SubFolder/File.txt")]
        public void IsPathRooted_ReturnsTrue_ForAbsolutePaths(string path)
        {
            // Arrange
            var fileResult = new FilePathResult(path, "text/plain", Mock.Of<IFileSystem>());

            // Act
            var isRooted = fileResult.IsPathRooted(path);

            // Assert
            Assert.True(isRooted);
        }

        [Theory]
        [InlineData("FilePathResultTestFile.txt")]
        [InlineData("/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt")]
        // Paths with subfolders and mixed slash kinds
        [InlineData("/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("\\SubFolder/SubFolderTestFile.txt")]
        // '.' has no special meaning
        [InlineData("./FilePathResultTestFile.txt")]
        [InlineData(".\\FilePathResultTestFile.txt")]
        [InlineData("./SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("./SubFolder\\SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder/SubFolderTestFile.txt")]
        // Traverse to the parent directory and back to the file system directory
        [InlineData("..\\TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles\\FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt")]
        // '~/' and '~\' mean the application root folder
        [InlineData("~/FilePathResultTestFile.txt")]
        [InlineData("~\\FilePathResultTestFile.txt")]
        [InlineData("~/SubFolder/SubFolderTestFile.txt")]
        [InlineData("~\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("~/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("~\\SubFolder/SubFolderTestFile.txt")]
        public void IsPathRooted_ReturnsFalse_ForRelativePaths(string path)
        {
            // Arrange
            var fileResult = new FilePathResult(path, "text/plain", Mock.Of<IFileSystem>());

            // Act
            var isRooted = fileResult.IsPathRooted(path);

            // Assert
            Assert.False(isRooted);
        }
    }
}