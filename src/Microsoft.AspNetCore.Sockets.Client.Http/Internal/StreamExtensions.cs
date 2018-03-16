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
                // REVIEW: Should we use the default buffer size here?
                // 81920 is the default bufferSize, there is no stream.CopyToAsync overload that takes only a cancellationToken
                await stream.CopyToAsync(new PipelineWriterStream(writer), bufferSize: 81920, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
                return;
            }
            writer.Complete();
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
