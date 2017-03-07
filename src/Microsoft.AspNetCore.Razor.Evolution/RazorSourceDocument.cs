// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class RazorSourceDocument
    {
        private const int LargeObjectHeapLimitInChars = 40 * 1024; // 40K Unicode chars is 80KB which is less than the large object heap limit.

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

        public static RazorSourceDocument ReadFrom(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var path = projectItem.PhysicalPath;
            if (string.IsNullOrEmpty(path))
            {
                path = projectItem.Path;
            }

            using (var inputStream = projectItem.Read())
            {
                return ReadFrom(inputStream, path);
            }
        }

        private static RazorSourceDocument ReadFromInternal(Stream stream, string filename, Encoding encoding)
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
                        // If the resulting string would end up on the large object heap, then use LargeTextRazorSourceDocument.
                        return new LargeTextRazorSourceDocument(
                            reader,
                            LargeObjectHeapLimitInChars,
                            contentEncoding,
                            filename);
                    }

                    content = reader.ReadToEnd();
                }
            }

            return new DefaultRazorSourceDocument(content, contentEncoding, filename);
        }
    }
}
