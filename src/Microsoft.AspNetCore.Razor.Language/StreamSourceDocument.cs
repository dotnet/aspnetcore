// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class StreamSourceDocument : RazorSourceDocument
    {
        // Internal for testing
        internal readonly RazorSourceDocument _innerSourceDocument;

        private readonly byte[] _checksum;

        public StreamSourceDocument(Stream stream, Encoding encoding, string fileName)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _checksum = ComputeChecksum(stream);
            _innerSourceDocument = CreateInnerSourceDocument(stream, encoding, fileName);
        }

        public override char this[int position] => _innerSourceDocument[position];

        public override Encoding Encoding => _innerSourceDocument.Encoding;

        public override string FileName => _innerSourceDocument.FileName;

        public override int Length => _innerSourceDocument.Length;

        public override RazorSourceLineCollection Lines => _innerSourceDocument.Lines;

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            => _innerSourceDocument.CopyTo(sourceIndex, destination, destinationIndex, count);

        public override byte[] GetChecksum()
        {
            var copiedChecksum = new byte[_checksum.Length];
            _checksum.CopyTo(copiedChecksum, 0);

            return copiedChecksum;
        }

        private static byte[] ComputeChecksum(Stream stream)
        {
            using (var hashAlgorithm = SHA1.Create())
            {
                var checksum = hashAlgorithm.ComputeHash(stream);
                stream.Position = 0;

                return checksum;
            }
        }

        private static RazorSourceDocument CreateInnerSourceDocument(Stream stream, Encoding encoding, string fileName)
        {
            var streamLength = (int)stream.Length;
            var content = string.Empty;
            var contentEncoding = encoding ?? Encoding.UTF8;

            if (streamLength > 0)
            {
                var bufferSize = Math.Min(streamLength, LargeObjectHeapLimitInChars);

                var reader = new StreamReader(
                    stream,
                    contentEncoding,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: bufferSize,
                    leaveOpen: true);

                using (reader)
                {
                    reader.Peek();      // Just to populate the encoding

                    if (encoding == null)
                    {
                        contentEncoding = reader.CurrentEncoding;
                    }
                    else if (encoding != reader.CurrentEncoding)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatMismatchedContentEncoding(
                                encoding.EncodingName,
                                reader.CurrentEncoding.EncodingName));
                    }

                    if (streamLength > LargeObjectHeapLimitInChars)
                    {
                        // If the resulting string would end up on the large object heap, then use LargeTextSourceDocument.
                        return new LargeTextSourceDocument(
                            reader,
                            LargeObjectHeapLimitInChars,
                            contentEncoding,
                            fileName);
                    }

                    content = reader.ReadToEnd();
                }
            }

            return new StringSourceDocument(content, contentEncoding, fileName);
        }
    }
}
