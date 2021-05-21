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
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_CopiesBuffer_ToOutputStream(actionType, action);
        }

        [Theory]
        [InlineData(0, 4, "Hello", 5)]
        [InlineData(6, 10, "World", 5)]
        [InlineData(null, 5, "World", 5)]
        [InlineData(6, null, "World", 5)]
        public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest
                .WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(start, end, expectedString, contentLength, actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored(actionType, action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest
                .WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(rangeString, actionType, action);
        }

        [Theory]
        [InlineData("bytes = 12-13")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest
                .WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(rangeString, actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_PreconditionFailed_RangeRequestedIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_PreconditionFailed_RangeRequestedIgnored(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.WriteFileAsync_NotModified_RangeRequestedIgnored(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            var actionType = "HttpContext";
            var action = new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context));

            await BaseFileContentResultTest.ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(actionType, action);
        }
    }
}
