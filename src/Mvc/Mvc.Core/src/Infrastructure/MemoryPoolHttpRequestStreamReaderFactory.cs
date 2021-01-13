// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IHttpRequestStreamReaderFactory"/> that uses pooled buffers.
    /// </summary>
    internal class MemoryPoolHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory
    {
        /// <summary>
        /// The default size of created char buffers.
        /// </summary>
        public static readonly int DefaultBufferSize = 1024; // 1KB - results in a 4KB byte array for UTF8.

        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="MemoryPoolHttpRequestStreamReaderFactory"/>.
        /// </summary>
        /// <param name="bytePool">
        /// The <see cref="ArrayPool{Byte}"/> for creating <see cref="T:byte[]"/> buffers.
        /// </param>
        /// <param name="charPool">
        /// The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.
        /// </param>
        public MemoryPoolHttpRequestStreamReaderFactory(
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            if (bytePool == null)
            {
                throw new ArgumentNullException(nameof(bytePool));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            _bytePool = bytePool;
            _charPool = charPool;
        }

        /// <inheritdoc />
        public TextReader CreateReader(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return new HttpRequestStreamReader(stream, encoding, DefaultBufferSize, _bytePool, _charPool);
        }
    }
}
