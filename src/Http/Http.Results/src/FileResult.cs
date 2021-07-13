// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result
{
    internal abstract partial class FileResult
    {
        private string? _fileDownloadName;

        /// <summary>
        /// Creates a new <see cref="FileResult"/> instance with
        /// the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult(string? contentType)
        {
            ContentType = contentType ?? "application/octet-stream";
        }

        /// <summary>
        /// Gets the Content-Type header for the response.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the file name that will be used in the Content-Disposition header of the response.
        /// </summary>
        [AllowNull]
        public string FileDownloadName
        {
            get { return _fileDownloadName ?? string.Empty; }
            init { _fileDownloadName = value; }
        }

        /// <summary>
        /// Gets or sets the last modified information associated with the <see cref="FileResult"/>.
        /// </summary>
        public DateTimeOffset? LastModified { get; init; }

        /// <summary>
        /// Gets or sets the etag associated with the <see cref="FileResult"/>.
        /// </summary>
        public EntityTagHeaderValue? EntityTag { get; init; }

        /// <summary>
        /// Gets or sets the value that enables range processing for the <see cref="FileResult"/>.
        /// </summary>
        public bool EnableRangeProcessing { get; init; }

        protected static partial class Log
        {
            public static void ExecutingFileResult(ILogger logger, FileResult fileResult)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    var fileResultType = fileResult.GetType().Name;
                    ExecutingFileResultWithNoFileName(logger, fileResultType, fileResult.FileDownloadName);
                }
            }

            public static void ExecutingFileResult(ILogger logger, FileResult fileResult, string fileName)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    var fileResultType = fileResult.GetType().Name;
                    ExecutingFileResult(logger, fileResultType, fileName, fileResult.FileDownloadName);
                }
            }

            [LoggerMessage(1, LogLevel.Information,
                "Executing {FileResultType}, sending file with download name '{FileDownloadName}'.",
                EventName = "ExecutingFileResultWithNoFileName",
                SkipEnabledCheck = true)]
            private static partial void ExecutingFileResultWithNoFileName(ILogger logger, string fileResultType, string fileDownloadName);

            [LoggerMessage(2, LogLevel.Information,
                "Executing {FileResultType}, sending file '{FileDownloadPath}' with download name '{FileDownloadName}'.",
                EventName = "ExecutingFileResult",
                SkipEnabledCheck = true)]
            private static partial void ExecutingFileResult(ILogger logger, string fileResultType, string fileDownloadPath, string fileDownloadName);
        }
    }
}
