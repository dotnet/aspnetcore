// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Rendering
{
    /// <summary>
    /// Writeable memory stream backed by a an <see cref="ArrayPool{T}"/>.
    /// </summary>
    internal sealed class ArrayBuilderMemoryStream : Stream
    {
        public ArrayBuilderMemoryStream()
        {
            ArrayBuilder = new ArrayBuilder<byte>(2048);
        }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => ArrayBuilder.Count;

        /// <inheritdoc />
        public override long Position
        {
            get => ArrayBuilder.Count;
            set => throw new NotSupportedException();
        }

        public ArrayBuilder<byte> ArrayBuilder { get; }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowArgumentException(buffer, offset, count);

            ArrayBuilder.Append(buffer, offset, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentException(buffer, offset, count);

            ArrayBuilder.Append(buffer, offset, count);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // Do nothing. It's the responsibility of the consumer of this API to dispose the ArrayBuilder
        }

        /// <inheritdoc />
        public override ValueTask DisposeAsync() => default;

        private static void ThrowArgumentException(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }
    }
}
