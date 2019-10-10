// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Writeable memory stream backed by a an <see cref="ArrayBuilder{T}"/>.
    /// </summary>
    internal sealed class ArrayBuilderMemoryStream : Stream
    {
        public ArrayBuilderMemoryStream(ArrayBuilder<byte> arrayBuilder)
        {
            ArrayBuilder = arrayBuilder;
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
            ValidateArguments(buffer, offset, count);

            ArrayBuilder.Append(buffer, offset, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateArguments(buffer, offset, count);

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
            // Do nothing.
        }

        /// <inheritdoc />
        public override ValueTask DisposeAsync() => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
            }
        }

        private static class ThrowHelper
        {
            public static void ThrowArgumentNullException(string name)
                => throw new ArgumentNullException(name);

            public static void ThrowArgumentOutOfRangeException(string name)
                => throw new ArgumentOutOfRangeException(name);
        }
    }
}
