// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileStreamResultExecutor : FileResultExecutorBase
    {
        public FileStreamResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<VirtualFileResultExecutor>(loggerFactory))
        {
        }

        public virtual Task ExecuteAsync(ActionContext context, FileStreamResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            long? fileLength = null;
            if (result.FileStream.CanSeek)
            {
                fileLength = result.FileStream.Length;
            }

            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                fileLength,
                result.LastModified,
                result.EntityTag);

            if (!serveBody)
            {
                return Task.CompletedTask;
            }

            return WriteFileAsync(context, result, range, rangeLength);
        }

        protected virtual Task WriteFileAsync(ActionContext context, FileStreamResult result, RangeItemHeaderValue range, long rangeLength)
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

            return WriteFileAsync(context.HttpContext, result.FileStream, range, rangeLength);
        }
    }
}
