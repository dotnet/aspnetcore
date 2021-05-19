// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file as the response.
    /// </summary>
    public abstract class FileResult : ActionResult, IResult
    {
        private string? _fileDownloadName;

        /// <summary>
        /// Creates a new <see cref="FileResult"/> instance with
        /// the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult(string contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            ContentType = contentType;
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
            set { _fileDownloadName = value; }
        }

        /// <summary>
        /// Gets or sets the last modified information associated with the <see cref="FileResult"/>.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the etag associated with the <see cref="FileResult"/>.
        /// </summary>
        public EntityTagHeaderValue? EntityTag { get; set; }

        /// <summary>
        /// Gets or sets the value that enables range processing for the <see cref="FileResult"/>.
        /// </summary>
        public bool EnableRangeProcessing { get; set; }

        /// <inheritdoc />
        Task IResult.ExecuteAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<FileResult>();

            var (range, rangeLenth, serveBody) = FileResultExecutorBase.SetHeadersAndLog(
                httpContext,
                this,
                null,
                EnableRangeProcessing,
                LastModified,
                EntityTag,
                logger);

            if (!serveBody || (range != null && rangeLenth == 0))
            {
                // No body to return or incorrect range data
                return Task.CompletedTask;
            }

            if (range != null)
            {
                logger.WritingRangeToBody();
            }

            return FileResultExecutorBase.WriteFileAsync(httpContext, httpContext.Response.Body, range, rangeLenth);
        }
    }
}
