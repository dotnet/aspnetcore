// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Default implementation of <see cref="IFormFile"/>.
    /// </summary>
    public class FormFile : IFormFile
    {
        // Stream.CopyTo method uses 80KB as the default buffer size.
        private const int DefaultBufferSize = 80 * 1024;

        private readonly Stream _baseStream;
        private readonly long _baseStreamOffset;

        public FormFile(Stream baseStream, long baseStreamOffset, long length, string name, string fileName)
        {
            _baseStream = baseStream;
            _baseStreamOffset = baseStreamOffset;
            Length = length;
            Name = name;
            FileName = fileName;
        }

        /// <summary>
        /// Gets the raw Content-Disposition header of the uploaded file.
        /// </summary>
        public string ContentDisposition
        {
            get { return Headers[HeaderNames.ContentDisposition]; }
            set { Headers[HeaderNames.ContentDisposition] = value; }
        }

        /// <summary>
        /// Gets the raw Content-Type header of the uploaded file.
        /// </summary>
        public string ContentType
        {
            get { return Headers[HeaderNames.ContentType]; }
            set { Headers[HeaderNames.ContentType] = value; }
        }

        /// <summary>
        /// Gets the header dictionary of the uploaded file.
        /// </summary>
        public IHeaderDictionary Headers { get; set; }

        /// <summary>
        /// Gets the file length in bytes.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Gets the name from the Content-Disposition header.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the file name from the Content-Disposition header.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Opens the request stream for reading the uploaded file.
        /// </summary>
        public Stream OpenReadStream()
        {
            return new ReferenceReadStream(_baseStream, _baseStreamOffset, Length);
        }

        /// <summary>
        /// Copies the contents of the uploaded file to the <paramref name="target"/> stream.
        /// </summary>
        /// <param name="target">The stream to copy the file contents to.</param>
        public void CopyTo(Stream target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            using (var readStream = OpenReadStream())
            {
                readStream.CopyTo(target, DefaultBufferSize);
            }
        }

        /// <summary>
        /// Asynchronously copies the contents of the uploaded file to the <paramref name="target"/> stream.
        /// </summary>
        /// <param name="target">The stream to copy the file contents to.</param>
        /// <param name="cancellationToken"></param>
        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            using (var readStream = OpenReadStream())
            {
                await readStream.CopyToAsync(target, DefaultBufferSize, cancellationToken);
            }
        }
    }
}
