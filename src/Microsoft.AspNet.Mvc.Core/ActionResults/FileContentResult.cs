// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a binary file to the response.
    /// </summary>
    public class FileContentResult : FileResult
    {
        /// <summary>
        /// Creates a new <see cref="FileContentResult"/> instance with
        /// the provided <paramref name="fileContents"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileContents">The bytes that represent the file contents.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileContentResult([NotNull] byte[] fileContents, string contentType)
            : base(contentType)
        {
            FileContents = fileContents;
        }

        /// <summary>
        /// Gets the file contents.
        /// </summary>
        public byte[] FileContents { get; private set; }

        /// <inheritdoc />
        protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            return response.Body.WriteAsync(FileContents, 0, FileContents.Length, cancellation);
        }
    }
}