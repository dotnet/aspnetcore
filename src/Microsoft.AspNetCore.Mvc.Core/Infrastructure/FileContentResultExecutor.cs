// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class FileContentResultExecutor : FileResultExecutorBase, IActionResultExecutor<FileContentResult>
    {
        public FileContentResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileContentResultExecutor>(loggerFactory))
        {
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, FileContentResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                result.FileContents.Length,
                result.EnableRangeProcessing,
                result.LastModified,
                result.EntityTag);

            if (!serveBody)
            {
                return Task.CompletedTask;
            }

            return WriteFileAsync(context, result, range, rangeLength);
        }

        protected virtual Task WriteFileAsync(ActionContext context, FileContentResult result, RangeItemHeaderValue range, long rangeLength)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (range != null && rangeLength == 0)
            {
                return Task.CompletedTask;
            }

            var fileContentStream = new MemoryStream(result.FileContents);
            return WriteFileAsync(context.HttpContext, fileContentStream, range, rangeLength);
        }
    }
}
