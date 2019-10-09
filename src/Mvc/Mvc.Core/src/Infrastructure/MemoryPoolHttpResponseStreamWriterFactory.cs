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
    /// An <see cref="IHttpResponseStreamWriterFactory"/> that uses pooled buffers.
    /// </summary>
    internal class MemoryPoolHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        /// <summary>
        /// The default size of buffers <see cref="HttpResponseStreamWriter"/>s will allocate.
        /// </summary>
        /// <value>
        /// 16K causes each <see cref="HttpResponseStreamWriter"/> to allocate one 16K
        /// <see langword="char"/> array and one 32K (for UTF8) <see langword="byte"/> array.
        /// </value>
        /// <remarks>
        /// <see cref="MemoryPoolHttpResponseStreamWriterFactory"/> maintains <see cref="ArrayPool{T}"/>s
        /// for these arrays.
        /// </remarks>
        public static readonly int DefaultBufferSize = 16 * 1024;

        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="MemoryPoolHttpResponseStreamWriterFactory"/>.
        /// </summary>
        /// <param name="bytePool">
        /// The <see cref="ArrayPool{Byte}"/> for creating <see cref="byte"/> buffers.
        /// </param>
        /// <param name="charPool">
        /// The <see cref="ArrayPool{Char}"/> for creating <see cref="char"/> buffers.
        /// </param>
        public MemoryPoolHttpResponseStreamWriterFactory(
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
        public TextWriter CreateWriter(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize, _bytePool, _charPool);
        }
    }
}
