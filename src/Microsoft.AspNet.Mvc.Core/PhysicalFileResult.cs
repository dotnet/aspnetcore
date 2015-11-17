// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="FileResult"/> on execution will write a file from disk to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class PhysicalFileResult : FileResult
    {
        private const int DefaultBufferSize = 0x1000;
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
        }

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fileName = value;
            }
        }

        /// <inheritdoc />
        protected override async Task WriteFileAsync(HttpResponse response)
        {
            if (!Path.IsPathRooted(FileName))
            {
                throw new NotSupportedException(Resources.FormatFileResult_PathNotRooted(FileName));
            }

            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null)
            {
                await sendFile.SendFileAsync(
                    FileName,
                    offset: 0,
                    length: null,
                    cancellation: default(CancellationToken));
            }
            else
            {
                var fileStream = GetFileStream(FileName);

                using (fileStream)
                {
                    await fileStream.CopyToAsync(response.Body, DefaultBufferSize);
                }
            }
        }

        /// <summary>
        /// Returns <see cref="Stream"/> for the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path for which the <see cref="FileStream"/> is needed.</param>
        /// <returns><see cref="FileStream"/> for the specified <paramref name="path"/>.</returns>
        protected virtual Stream GetFileStream(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    DefaultBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}
