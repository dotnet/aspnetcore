// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class PhysicalFileResultTest
    {
        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 5, "ts�", 5)]
        [InlineData(8, null, "ResultTestFile contents�", 26)]
        public async Task WriteFileAsync_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_WritesRangeRequested(
                start,
                end,
                expectedString,
                contentLength,
                actionType,
                action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(actionType, action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(string rangeString)
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest
                .WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(rangeString, actionType, action);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_RangeRequestedNotSatisfiable(string rangeString)
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest
                .WriteFileAsync_RangeRequestedNotSatisfiable(rangeString, actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeRequested_PreconditionFailed(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_NotModified()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.WriteFileAsync_RangeRequested_NotModified(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent(actionType, action);
        }

        [Theory]
        [InlineData(0, 3, 4)]
        [InlineData(8, 13, 6)]
        [InlineData(null, 3, 3)]
        [InlineData(8, null, 26)]
        public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, long contentLength)
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(
                start,
                end,
                contentLength,
                actionType,
                action);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithAbsolutePaths()
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest.ExecuteResultAsync_WorksWithAbsolutePaths(actionType, action);
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
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BasePhysicalFileResultTest
                .ExecuteAsync_ThrowsNotSupported_ForNonRootedPaths(path, actionType, action);
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
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            BasePhysicalFileResultTest
                .ExecuteAsync_ThrowsDirectoryNotFound_IfItCanNotFindTheDirectory_ForRootPaths(path, actionType, action);
        }

        [Theory]
        [InlineData("/FilePathResultTestFile.txt")]
        [InlineData("\\FilePathResultTestFile.txt")]
        public void ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(string path)
        {
            var actionType = "HttpContext";
            var action = new Func<PhysicalFileResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            BasePhysicalFileResultTest
                .ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(path, actionType, action);
        }
    }
}
