// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class FormFile : IFormFile
    {
        private Stream _baseStream;
        private long _baseStreamOffset;
        private long _length;

        public FormFile(Stream baseStream, long baseStreamOffset, long length)
        {
            _baseStream = baseStream;
            _baseStreamOffset = baseStreamOffset;
            _length = length;
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

        public long Length
        {
            get { return _length; }
        }

        public Stream OpenReadStream()
        {
            return new ReferenceReadStream(_baseStream, _baseStreamOffset, _length);
        }
    }
}