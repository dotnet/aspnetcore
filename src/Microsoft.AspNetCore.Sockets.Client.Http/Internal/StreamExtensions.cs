// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal static class StreamExtensions
    {
        public static async Task CopyToEndAsync(this Stream stream, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            try
            {
                await stream.CopyToAsync(writer, cancellationToken);
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
                return;
            }
            writer.Complete();
        }

        /// <summary>
        /// Copies the content of a <see cref="Stream"/> into a <see cref="PipeWriter"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="writer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static Task CopyToAsync(this Stream stream, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            // 81920 is the default bufferSize, there is not stream.CopyToAsync overload that takes only a cancellationToken
            return stream.CopyToAsync(new PipelineWriterStream(writer), bufferSize: 81920, cancellationToken: cancellationToken);
        }

        private class PipelineWriterStream : Stream
        {
            private readonly PipeWriter _writer;

            public PipelineWriterStream(PipeWriter writer)
            {
                _writer = writer;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _writer.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
            }
        }
    }
}
