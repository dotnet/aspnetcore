// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class FileContentResultTest
    {
        [Fact]
        public async Task WriteFileAsync_CopiesBuffer_ToOutputStream()
        {var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));
            await BaseFileContentResultTest.WriteFileAsync_CopiesBuffer_ToOutputStream(action);
        }

        [Theory]
        [InlineData(0, 4, "Hello", 5)]
        [InlineData(6, 10, "World", 5)]
        [InlineData(null, 5, "World", 5)]
        [InlineData(6, null, "World", 5)]
        public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest
                .WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(start, end, expectedString, contentLength, action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest(action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored(action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored(action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(rangeString, action);
        }

        [Theory]
        [InlineData("bytes = 12-13")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(rangeString, action);
        }

        [Fact]
        public async Task WriteFileAsync_PreconditionFailed_RangeRequestedIgnored()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_PreconditionFailed_RangeRequestedIgnored(action);
        }

        [Fact]
        public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.WriteFileAsync_NotModified_RangeRequestedIgnored(action);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            var action = new Func<FileContentResult, HttpContext, Task>(async (result, context) => await ((IResult)result).ExecuteAsync(context));

            await BaseFileContentResultTest.ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(action);
        }
    }
}
