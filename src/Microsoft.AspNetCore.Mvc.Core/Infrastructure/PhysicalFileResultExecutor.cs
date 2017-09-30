// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class PhysicalFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<PhysicalFileResult>
    {
        public PhysicalFileResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<PhysicalFileResultExecutor>(loggerFactory))
        {
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, PhysicalFileResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var fileInfo = GetFileInfo(result.FileName);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(
                    Resources.FormatFileResult_InvalidPath(result.FileName), result.FileName);
            }

            var lastModified = result.LastModified ?? fileInfo.LastModified;
            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                fileInfo.Length,
                result.EnableRangeProcessing,
                lastModified,
                result.EntityTag);

            if (serveBody)
            {
                return WriteFileAsync(context, result, range, rangeLength);
            }

            return Task.CompletedTask;
        }

        protected virtual Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue range, long rangeLength)
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

            var response = context.HttpContext.Response;
            if (!Path.IsPathRooted(result.FileName))
            {
                throw new NotSupportedException(Resources.FormatFileResult_PathNotRooted(result.FileName));
            }

            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null)
            {
                if (range != null)
                {
                    return sendFile.SendFileAsync(
                        result.FileName,
                        offset: range.From ?? 0L,
                        count: rangeLength,
                        cancellation: default(CancellationToken));
                }

                return sendFile.SendFileAsync(
                    result.FileName,
                    offset: 0,
                    count: null,
                    cancellation: default(CancellationToken));
            }

            return WriteFileAsync(context.HttpContext, GetFileStream(result.FileName), range, rangeLength);
        }

        protected virtual Stream GetFileStream(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        protected virtual FileMetadata GetFileInfo(string path)
        {
            var fileInfo = new FileInfo(path);
            return new FileMetadata
            {
                Exists = fileInfo.Exists,
                Length = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc,
            };
        }

        protected class FileMetadata
        {
            public bool Exists { get; set; }

            public long Length { get; set; }

            public DateTimeOffset LastModified { get; set; }
        }
    }
}
