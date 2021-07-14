// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed partial class FileContentResult : FileResult, IResult
    {
        /// <summary>
        /// Creates a new <see cref="FileContentResult"/> instance with
        /// the provided <paramref name="fileContents"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileContents">The bytes that represent the file contents.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileContentResult(byte[] fileContents, string? contentType)
            : base(contentType)
        {
            FileContents = fileContents;
        }

        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public byte[] FileContents { get; init; }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<FileContentResult>>();
            Log.ExecutingFileResult(logger, this);

            var fileResultInfo = new FileResultInfo
            {
                ContentType = ContentType,
                EnableRangeProcessing = EnableRangeProcessing,
                EntityTag = EntityTag,
                FileDownloadName = FileDownloadName,
                LastModified = LastModified,
            };

            var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
                httpContext,
                fileResultInfo,
                FileContents.Length,
                EnableRangeProcessing,
                LastModified,
                EntityTag,
                logger);

            if (!serveBody)
            {
                return Task.CompletedTask;
            }

            if (range != null && rangeLength == 0)
            {
                return Task.CompletedTask;
            }

            if (range != null)
            {
                FileResultHelper.Log.WritingRangeToBody(logger);
            }

            var fileContentStream = new MemoryStream(FileContents);
            return FileResultHelper.WriteFileAsync(httpContext, fileContentStream, range, rangeLength);
        }
    }
}
