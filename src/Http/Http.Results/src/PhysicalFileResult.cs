// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    /// <summary>
    /// A <see cref="PhysicalFileResult"/> on execution will write a file from disk to the response
    /// using mechanisms provided by the host.
    /// </summary>
    internal sealed partial class PhysicalFileResult : FileResult, IResult
    {
        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, string? contentType)
            : base(contentType)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName { get; }

        // For testing
        public Func<string, FileInfoWrapper> GetFileInfoWrapper { get; init; } =
            static path => new FileInfoWrapper(path);

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var fileInfo = GetFileInfoWrapper(FileName);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Could not find file: {FileName}", FileName);
            }

            var logger = httpContext.RequestServices.GetRequiredService<ILogger<PhysicalFileResult>>();

            Log.ExecutingFileResult(logger, this, FileName);

            var lastModified = LastModified ?? fileInfo.LastWriteTimeUtc;
            var fileResultInfo = new FileResultInfo
            {
                ContentType = ContentType,
                EnableRangeProcessing = EnableRangeProcessing,
                EntityTag = EntityTag,
                FileDownloadName = FileDownloadName,
                LastModified = lastModified,
            };

            var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
                httpContext,
                fileResultInfo,
                fileInfo.Length,
                EnableRangeProcessing,
                lastModified,
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

            var response = httpContext.Response;
            if (!Path.IsPathRooted(FileName))
            {
                throw new NotSupportedException($"Path '{FileName}' was not rooted.");
            }

            if (range != null)
            {
                FileResultHelper.Log.WritingRangeToBody(logger);
            }

            var offset = 0L;
            var count = (long?)null;
            if (range != null)
            {
                offset = range.From ?? 0L;
                count = rangeLength;
            }

            return response.SendFileAsync(
                FileName,
                offset: offset,
                count: count);
        }

        internal readonly struct FileInfoWrapper
        {
            public FileInfoWrapper(string path)
            {
                var fileInfo = new FileInfo(path);
                Exists = fileInfo.Exists;
                Length = fileInfo.Length;
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
            }

            public bool Exists { get; init; }

            public long Length { get; init; }

            public DateTimeOffset LastWriteTimeUtc { get; init; }
        }
    }
}
