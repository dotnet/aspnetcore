// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class FormFile : IFormFile
    {
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

        public string ContentDisposition
        {
            get { return Headers["Content-Disposition"]; }
            set { Headers["Content-Disposition"] = value; }
        }

        public string ContentType
        {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
        }

        public IHeaderDictionary Headers { get; set; }

        public long Length { get; }

        public string Name { get; }

        public string FileName { get; }

        public Stream OpenReadStream()
        {
            return new ReferenceReadStream(_baseStream, _baseStreamOffset, Length);
        }
    }
}