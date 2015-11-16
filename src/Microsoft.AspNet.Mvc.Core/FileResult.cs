// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file as the response.
    /// </summary>
    public abstract class FileResult : ActionResult
    {
        private string _fileDownloadName;

        /// <summary>
        /// Creates a new <see cref="FileResult"/> instance with
        /// the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult(string contentType)
            : this(new MediaTypeHeaderValue(contentType))
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
        }

        /// <summary>
        /// Creates a new <see cref="FileResult"/> instance with
        /// the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult(MediaTypeHeaderValue contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            ContentType = contentType;
        }

        /// <summary>
        /// Gets the <see cref="MediaTypeHeaderValue"/> representing the Content-Type header of the response.
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; }

        /// <summary>
        /// Gets the file name that will be used in the Content-Disposition header of the response.
        /// </summary>
        public string FileDownloadName
        {
            get { return _fileDownloadName ?? string.Empty; }
            set { _fileDownloadName = value; }
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<FileResult>();

            var response = context.HttpContext.Response;
            response.ContentType = ContentType.ToString();

            if (!string.IsNullOrEmpty(FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.SetHttpFileName(FileDownloadName);
                context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            }
            
            logger.FileResultExecuting(FileDownloadName);
            return WriteFileAsync(response);
        }

        /// <summary>
        /// Writes the file to the specified <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that will complete when the file has been written to the response.
        /// </returns>
        protected abstract Task WriteFileAsync(HttpResponse response);
    }
}