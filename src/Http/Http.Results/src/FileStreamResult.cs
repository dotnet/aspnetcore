// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    /// <summary>
    /// Represents an <see cref="FileResult"/> that when executed will
    /// write a file from a stream to the response.
    /// </summary>
    internal sealed class FileStreamResult : FileResult, IResult
    {
        /// <summary>
        /// Creates a new <see cref="FileStreamResult"/> instance with
        /// the provided <paramref name="fileStream"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileStream">The stream with the file.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileStreamResult(Stream fileStream, string? contentType)
            : base(contentType)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            FileStream = fileStream;
        }

        /// <summary>
        /// Gets or sets the stream with the file that will be sent back as the response.
        /// </summary>
        public Stream FileStream { get; }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<FileStreamResult>>();
            await using (FileStream)
            {
                Log.ExecutingFileResult(logger, this);

                long? fileLength = null;
                if (FileStream.CanSeek)
                {
                    fileLength = FileStream.Length;
                }

                var fileResultInfo = new FileResultInfo
                {
                    ContentType = ContentType,
                    EnableRangeProcessing = EnableRangeProcessing,
                    EntityTag = EntityTag,
                    FileDownloadName = FileDownloadName,
                };

                var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
                    httpContext,
                    fileResultInfo,
                    fileLength,
                    EnableRangeProcessing,
                    LastModified,
                    EntityTag,
                    logger);

                if (!serveBody)
                {
                    return;
                }

                if (range != null && rangeLength == 0)
                {
                    return;
                }

                if (range != null)
                {
                    FileResultHelper.Log.WritingRangeToBody(logger);
                }

                await FileResultHelper.WriteFileAsync(httpContext, FileStream, range, rangeLength);
            }
        }
    }
}
