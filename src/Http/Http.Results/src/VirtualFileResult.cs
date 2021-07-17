// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    /// <summary>
    /// A <see cref="FileResult" /> that on execution writes the file specified using a virtual path to the response
    /// using mechanisms provided by the host.
    /// </summary>
    internal sealed class VirtualFileResult : FileResult, IResult
    {
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with the provided <paramref name="fileName"/>
        /// and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, string? contentType)
            : base(contentType)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            [MemberNotNull(nameof(_fileName))]
            set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(HttpContext httpContext)
        {
            var hostingEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<VirtualFileResult>>();

            var fileInfo = GetFileInformation(hostingEnvironment.WebRootFileProvider);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Could not find file: {FileName}.", FileName);
            }

            Log.ExecutingFileResult(logger, this);

            var lastModified = LastModified ?? fileInfo.LastModified;
            var fileResultInfo = new FileResultInfo
            {
                ContentType = ContentType,
                FileDownloadName = FileDownloadName,
                EnableRangeProcessing = EnableRangeProcessing,
                EntityTag = EntityTag,
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

            if (range != null)
            {
                FileResultHelper.Log.WritingRangeToBody(logger);
            }

            var response = httpContext.Response;
            var offset = 0L;
            var count = (long?)null;
            if (range != null)
            {
                offset = range.From ?? 0L;
                count = rangeLength;
            }

            return response.SendFileAsync(
                fileInfo,
                offset,
                count);
        }

        internal IFileInfo GetFileInformation(IFileProvider fileProvider)
        {
            var normalizedPath = FileName;
            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(1);
            }

            var fileInfo = fileProvider.GetFileInfo(normalizedPath);
            return fileInfo;
        }
    }
}
