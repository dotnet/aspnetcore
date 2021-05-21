// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class PhysicalFileActionResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");

            // Act
            var result = new PhysicalFileResult(path, "text/plain");

            // Assert
            Assert.Equal(path, result.FileName);
        }

        [Fact]
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new PhysicalFileResult(path, contentType);

            // Assert
            Assert.Equal(path, result.FileName);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 5, "ts�", 5)]
        [InlineData(8, null, "ResultTestFile contents�", 26)]
        public async Task WriteFileAsync_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_WritesRangeRequested(
                start,
                end,
                expectedString,
                contentLength,
                action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(string rangeString)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(rangeString, action);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_RangeRequestedNotSatisfiable(string rangeString)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeRequestedNotSatisfiable(rangeString, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeRequested_PreconditionFailed(action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_NotModified()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeRequested_NotModified(action);
        }

        [Fact]
        public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent(action);
        }

        [Theory]
        [InlineData(0, 3, 4)]
        [InlineData(8, 13, 6)]
        [InlineData(null, 3, 3)]
        [InlineData(8, null, 26)]
        public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, long contentLength)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(start, end, contentLength, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(action);
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithAbsolutePaths()
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_WorksWithAbsolutePaths(action);
        }

        [Theory]
        [InlineData("FilePathResultTestFile.txt")]
        [InlineData("./FilePathResultTestFile.txt")]
        [InlineData(".\\FilePathResultTestFile.txt")]
        [InlineData("~/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles\\FilePathResultTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt")]
        [InlineData("~/SubFolder/SubFolderTestFile.txt")]
        [InlineData("~/SubFolder\\SubFolderTestFile.txt")]
        public async Task ExecuteAsync_ThrowsNotSupported_ForNonRootedPaths(string path)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BasePhysicalFileResultTest.ExecuteAsync_ThrowsNotSupported_ForNonRootedPaths(path, action);
        }

        [Theory]
        [InlineData("/SubFolder/SubFolderTestFile.txt")]
        [InlineData("\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("/SubFolder\\SubFolderTestFile.txt")]
        [InlineData("\\SubFolder/SubFolderTestFile.txt")]
        [InlineData("./SubFolder/SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder\\SubFolderTestFile.txt")]
        [InlineData("./SubFolder\\SubFolderTestFile.txt")]
        [InlineData(".\\SubFolder/SubFolderTestFile.txt")]
        public void ExecuteAsync_ThrowsDirectoryNotFound_IfItCanNotFindTheDirectory_ForRootPaths(string path)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            BasePhysicalFileResultTest
                .ExecuteAsync_ThrowsDirectoryNotFound_IfItCanNotFindTheDirectory_ForRootPaths(path, action);
        }

        [Theory]
        [InlineData("/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt")]
        public void ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(string path)
        {
            var action = new Func<PhysicalFileResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            BasePhysicalFileResultTest
                .ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(path, action);
        }
    }
}
