// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorSourceDocument
    {
        internal static readonly RazorSourceDocument[] EmptyArray = new RazorSourceDocument[0];

        public abstract Encoding Encoding { get; }

        public abstract string Filename { get; }

        public abstract char this[int position] { get; }

        public abstract int Length { get; }

        public abstract RazorSourceLineCollection Lines { get; }

        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        public static RazorSourceDocument ReadFrom(Stream stream, string filename)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ReadFromInternal(stream, filename, encoding: null);
        }

        public static RazorSourceDocument ReadFrom(Stream stream, string filename, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return ReadFromInternal(stream, filename, encoding);
        }

        private static RazorSourceDocument ReadFromInternal(Stream stream, string filename, Encoding encoding)
        {
            var reader = new StreamReader(
                stream,
                encoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: (int)stream.Length,
                leaveOpen: true);

            using (reader)
            {
                var content = reader.ReadToEnd();

                if (encoding == null)
                {
                    encoding = reader.CurrentEncoding;
                }
                else if (encoding != reader.CurrentEncoding)
                {
                    throw new InvalidOperationException(
                        Resources.FormatMismatchedContentEncoding(
                            encoding.EncodingName,
                            reader.CurrentEncoding.EncodingName));
                }

                return new DefaultRazorSourceDocument(content, encoding, filename);
            }
        }
    }
}
