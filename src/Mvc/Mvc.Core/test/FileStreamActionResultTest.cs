// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class FileStreamActionResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange
            var stream = Stream.Null;

            // Act
            var result = new FileStreamResult(stream, "text/plain");

            // Assert
            Assert.Equal(stream, result.FileStream);
        }

        [Fact]
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var stream = Stream.Null;
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new FileStreamResult(stream, contentType);

            // Assert
            Assert.Equal(stream, result.FileStream);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Fact]
        public void Constructor_SetsLastModifiedAndEtag()
        {
            // Arrange
            var stream = Stream.Null;
            var contentType = "text/plain";
            var expectedMediaType = contentType;
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");

            // Act
            var result = new FileStreamResult(stream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };

            // Assert
            Assert.Equal(lastModified, result.LastModified);
            Assert.Equal(entityTag, result.EntityTag);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Theory]
        [InlineData(0, 4, "Hello", 5)]
        [InlineData(6, 10, "World", 5)]
        [InlineData(null, 5, "World", 5)]
        [InlineData(6, null, "World", 5)]
        public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(
                start,
                end,
                expectedString,
                contentLength,
                action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(rangeString, action);
        }

        [Theory]
        [InlineData("bytes = 12-13")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(rangeString, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeRequested_PreconditionFailed(action);
        }

        [Fact]
        public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_NotModified_RangeRequestedIgnored(action);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(null)]
        public async Task WriteFileAsync_RangeRequested_FileLengthZeroOrNull(long? fileLength)
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_RangeRequested_FileLengthZeroOrNull(fileLength, action);
        }

        [Fact]
        public async Task WriteFileAsync_WritesResponse_InChunksOfFourKilobytes()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_WritesResponse_InChunksOfFourKilobytes(action);
        }

        [Fact]
        public async Task WriteFileAsync_CopiesProvidedStream_ToOutputStream()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.WriteFileAsync_CopiesProvidedStream_ToOutputStream(action);
        }

        [Fact]
        public async Task SetsSuppliedContentTypeAndEncoding()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.SetsSuppliedContentTypeAndEncoding(action);
        }

        [Fact]
        public async Task HeadRequest_DoesNotWriteToBody_AndClosesReadStream()
        {
            var action = new Func<FileStreamResult, ActionContext, Task>(async (result, context) => await result.ExecuteResultAsync(context));

            await BaseFileStreamResultTest.HeadRequest_DoesNotWriteToBody_AndClosesReadStream(action);
        }
    }
}
