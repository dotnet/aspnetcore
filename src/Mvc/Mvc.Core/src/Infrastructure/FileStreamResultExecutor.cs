// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IActionResultExecutor{FileStreamResult}"/> for a file stream result.
    /// </summary>
    public class FileStreamResultExecutor : FileResultExecutorBase, IActionResultExecutor<FileStreamResult>
    {
        /// <summary>
        /// Initializes a new <see cref="FileStreamResultExecutor"/>.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
        public FileStreamResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileStreamResultExecutor>(loggerFactory))
        {
        }

        /// <inheritdoc />
        public virtual async Task ExecuteAsync(ActionContext context, FileStreamResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            using (result.FileStream)
            {
                Logger.ExecutingFileResult(result);

                long? fileLength = null;
                if (result.FileStream.CanSeek)
                {
                    fileLength = result.FileStream.Length;
                }

                var (range, rangeLength, serveBody) = SetHeadersAndLog(
                    context,
                    result,
                    fileLength,
                    result.EnableRangeProcessing,
                    result.LastModified,
                    result.EntityTag);

                if (!serveBody)
                {
                    return;
                }

                await WriteFileAsync(context, result, range, rangeLength);
            }
        }

        /// <summary>
        /// Write the contents of the FileStreamResult to the response body.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="result">The FileStreamResult to write.</param>
        /// <param name="range">The <see cref="RangeItemHeaderValue"/>.</param>
        /// <param name="rangeLength">The range length.</param>
        protected virtual Task WriteFileAsync(
            ActionContext context,
            FileStreamResult result,
            RangeItemHeaderValue? range,
            long rangeLength)
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

            if (range != null)
            {
                Logger.WritingRangeToBody();
            }

            return WriteFileAsync(context.HttpContext, result.FileStream, range, rangeLength);
        }
    }
}
