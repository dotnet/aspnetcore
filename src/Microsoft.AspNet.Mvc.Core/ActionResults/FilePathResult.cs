// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file from disk to the response using mechanisms provided
    /// by the host.
    /// </summary>
    public class FilePathResult : FileResult
    {
        private const int DefaultBufferSize = 0x1000;

        /// <summary>
        /// Creates a new <see cref="FilePathResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute
        /// path. Relative and virtual paths are not supported.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FilePathResult([NotNull] string fileName, [NotNull] string contentType)
            : base(contentType)
        {
            if (!Path.IsPathRooted(fileName))
            {
                var message = Resources.FormatFileResult_InvalidPathType_RelativeOrVirtualPath(fileName);
                throw new ArgumentException(message, "fileName");
            }

            FileName = fileName;
        }

        /// <summary>
        /// Gets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName { get; private set; }

        /// <inheritdoc />
        protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            var sendFile = response.HttpContext.GetFeature<IHttpSendFileFeature>();
            if (sendFile != null)
            {
                return sendFile.SendFileAsync(
                    FileName,
                    offset: 0,
                    length: null,
                    cancellation: cancellation);
            }
            else
            {
                return CopyStreamToResponse(FileName, response, cancellation);
            }
        }

        private static async Task CopyStreamToResponse(
            string fileName,
            HttpResponse response,
            CancellationToken cancellation)
        {
            var fileStream = new FileStream(
                fileName, FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                await fileStream.CopyToAsync(response.Body, DefaultBufferSize, cancellation);
            }
        }
    }
}