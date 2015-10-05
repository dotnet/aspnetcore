// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.MemoryPool;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IHttpResponseStreamWriterFactory"/> that uses pooled buffers.
    /// </summary>
    public class MemoryPoolHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        /// <summary>
        /// The default size of created char buffers.
        /// </summary>
        public static readonly int DefaultBufferSize = 1024; // 1KB - results in a 4KB byte array for UTF8.

        private readonly IArraySegmentPool<byte> _bytePool;
        private readonly IArraySegmentPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="MemoryPoolHttpResponseStreamWriterFactory"/>.
        /// </summary>
        /// <param name="bytePool">
        /// The <see cref="IArraySegmentPool{byte}"/> for creating <see cref="byte"/> buffers.
        /// </param>
        /// <param name="charPool">
        /// The <see cref="IArraySegmentPool{char}"/> for creating <see cref="char"/> buffers.
        /// </param>
        public MemoryPoolHttpResponseStreamWriterFactory(
            IArraySegmentPool<byte> bytePool,
            IArraySegmentPool<char> charPool)
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

            LeasedArraySegment<byte> bytes = null;
            LeasedArraySegment<char> chars = null;

            try
            {
                chars = _charPool.Lease(DefaultBufferSize);

                // We need to compute the minimum size of the byte buffer based on the size of the char buffer,
                // so that we have enough room to encode the buffer in one shot.
                var minimumSize = encoding.GetMaxByteCount(DefaultBufferSize);
                bytes = _bytePool.Lease(minimumSize);

                return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize, bytes, chars);
            }
            catch
            {
                if (bytes != null)
                {
                    bytes.Owner.Return(bytes);
                }

                if (chars != null)
                {
                    chars.Owner.Return(chars);
                }

                throw;
            }
        }
    }
}
