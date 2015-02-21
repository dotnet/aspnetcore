// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
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
        {
            ContentType = contentType;
        }

        /// <summary>
        /// Gets or sets the Content-Type header value that will be written to the response.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the file name that will be used in the Content-Disposition header of the response.
        /// </summary>
        public string FileDownloadName
        {
            get { return _fileDownloadName ?? string.Empty; }
            set { _fileDownloadName = value; }
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;

            if (ContentType != null)
            {
                response.ContentType = ContentType;
            }

            if (!string.IsNullOrEmpty(FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                var cd = new ContentDispositionHeaderValue("attachment");
                cd.SetHttpFileName(FileDownloadName);
                context.HttpContext.Response.Headers.Set(HeaderNames.ContentDisposition, cd.ToString());
            }

            // We aren't flowing the cancellation token appropriately, see
            // https://github.com/aspnet/Mvc/issues/743 for details.
            return WriteFileAsync(response, CancellationToken.None);
        }

        /// <summary>
        /// Writes the file to the response.
        /// </summary>
        /// <param name="response">
        /// The <see cref="HttpResponse"/> where the file will be written
        /// </param>
        /// <param name="cancellation">The <see cref="CancellationToken"/>to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that will complete when the file has been written to the response.
        /// </returns>
        protected abstract Task WriteFileAsync(HttpResponse response, CancellationToken cancellation);
    }
}