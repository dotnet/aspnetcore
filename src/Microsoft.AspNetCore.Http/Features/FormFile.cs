// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;

namespace Microsoft.AspNetCore.Http.Features.Internal
{
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
            get { return Headers["Content-Disposition"]; }
            set { Headers["Content-Disposition"] = value; }
        }

        /// <summary>
        /// Gets the raw Content-Type header of the uploaded file.
        /// </summary>
        public string ContentType
        {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
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
        /// Saves the contents of the uploaded file.
        /// </summary>
        /// <param name="path">The path of the file to create.</param>
        public void SaveAs(string path)
        {
            using (var fileStream = File.Create(path, DefaultBufferSize))
            {
                var inputStream = OpenReadStream();
                inputStream.CopyTo(fileStream, DefaultBufferSize);
            }
        }

        /// <summary>
        /// Asynchronously saves the contents of the uploaded file.
        /// </summary>
        /// <param name="path">The path of the file to create.</param>
        /// <param name="cancellationToken"></param>
        public async Task SaveAsAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var fileStream = File.Create(path, DefaultBufferSize, FileOptions.Asynchronous))
            {
                var inputStream = OpenReadStream();
                await inputStream.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken);
            }
        }
    }
}
