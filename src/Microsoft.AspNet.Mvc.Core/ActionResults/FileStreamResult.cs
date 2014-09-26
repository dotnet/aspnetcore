// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file from a stream to the response.
    /// </summary>
    public class FileStreamResult : FileResult
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;

        /// <summary>
        /// Creates a new <see cref="FileStreamResult"/> instance with
        /// the provided <paramref name="fileStream"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileStream">The stream with the file.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileStreamResult([NotNull] Stream fileStream, string contentType)
            : base(contentType)
        {
            FileStream = fileStream;
        }

        /// <summary>
        /// Gets the stream with the file that will be sent back as the response.
        /// </summary>
        public Stream FileStream { get; private set; }

        /// <inheritdoc />
        protected async override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            var outputStream = response.Body;

            using (FileStream)
            {
                await FileStream.CopyToAsync(outputStream, BufferSize, cancellation);
            }
        }
    }
}