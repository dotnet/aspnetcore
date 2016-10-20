// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorSourceDocument : RazorSourceDocument
    {
        private MemoryStream _stream;

        public DefaultRazorSourceDocument(MemoryStream stream, Encoding encoding, string filename)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            Encoding = encoding;
            Filename = filename;
        }

        public Encoding Encoding { get; }

        public override string Filename { get; }

        public override TextReader CreateReader()
        {
            var copy = new MemoryStream(_stream.ToArray());

            return Encoding == null
                ? new StreamReader(copy, detectEncodingFromByteOrderMarks: true)
                : new StreamReader(copy, Encoding);
        }
    }
}
