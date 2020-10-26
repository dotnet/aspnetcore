// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc
{
    public class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        public const int DefaultBufferSize = 16 * 1024;

        public TextWriter CreateWriter(Stream stream, Encoding encoding)
        {
            return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize);
        }
    }
}
