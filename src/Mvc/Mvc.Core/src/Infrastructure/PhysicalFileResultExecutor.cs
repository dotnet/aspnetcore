// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A <see cref="IActionResultExecutor{PhysicalFileResult}"/> for <see cref="PhysicalFileResult"/>.
    /// </summary>
    public class PhysicalFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<PhysicalFileResult>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PhysicalFileResultExecutor"/>.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
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

            Logger.ExecutingFileResult(result, result.FileName);

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

        /// <inheritdoc/>
        protected virtual Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue? range, long rangeLength)
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

            if (range != null)
            {
                Logger.WritingRangeToBody();
            }

            if (range != null)
            {
                return response.SendFileAsync(result.FileName,
                    offset: range.From ?? 0L,
                    count: rangeLength);
            }

            return response.SendFileAsync(result.FileName,
                offset: 0,
                count: null);
        }

        /// <summary>
        /// Obsolete. This API is no longer called.
        /// </summary>
        [Obsolete("This API is no longer called.")]
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


        /// <summary>
        /// Get the file metadata for a path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The <see cref="FileMetadata"/> for the path.</returns>
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

        /// <summary>
        /// Represents metadata for a file.
        /// </summary>
        protected class FileMetadata
        {
            /// <summary>
            /// Whether a file exists.
            /// </summary>
            public bool Exists { get; set; }

            /// <summary>
            /// The file length.
            /// </summary>
            public long Length { get; set; }

            /// <summary>
            /// When the file was last modified.
            /// </summary>
            public DateTimeOffset LastModified { get; set; }
        }
    }
}
