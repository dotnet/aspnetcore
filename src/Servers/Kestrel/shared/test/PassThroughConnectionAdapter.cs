// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;

namespace Microsoft.AspNetCore.Testing
{
    public class PassThroughConnectionAdapter : IConnectionAdapter
    {
        public bool IsHttps => false;

        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
        {
            var adapted = new AdaptedConnection(new PassThroughStream(context.ConnectionStream));
            return Task.FromResult<IAdaptedConnection>(adapted);
        }

        private class AdaptedConnection : IAdaptedConnection
        {
            public AdaptedConnection(Stream stream)
            {
                ConnectionStream = stream;
            }

            public Stream ConnectionStream { get; }

            public void Dispose()
            {
            }
        }

        private class PassThroughStream : Stream
        {
            private readonly Stream _innerStream;

            public PassThroughStream(Stream innerStream)
            {
                _innerStream = innerStream;
            }

            public override bool CanRead => _innerStream.CanRead;

            public override bool CanSeek => _innerStream.CanSeek;

            public override bool CanTimeout => _innerStream.CanTimeout;

            public override bool CanWrite => _innerStream.CanWrite;

            public override long Length => _innerStream.Length;

            public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

            public override int ReadTimeout { get => _innerStream.ReadTimeout; set => _innerStream.ReadTimeout = value; }

            public override int WriteTimeout { get => _innerStream.WriteTimeout; set => _innerStream.WriteTimeout = value; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _innerStream.Read(buffer, offset, count);
            }

            public override int ReadByte()
            {
                return _innerStream.ReadByte();
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return _innerStream.BeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return _innerStream.EndRead(asyncResult);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _innerStream.Write(buffer, offset, count);
            }


            public override void WriteByte(byte value)
            {
                _innerStream.WriteByte(value);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return _innerStream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                _innerStream.EndWrite(asyncResult);
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            public override void Flush()
            {
                _innerStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _innerStream.FlushAsync();

            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Close()
            {
                _innerStream.Close();
            }

            public override int Read(Span<byte> buffer)
            {
                return _innerStream.Read(buffer);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _innerStream.ReadAsync(buffer, cancellationToken);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                _innerStream.Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _innerStream.WriteAsync(buffer, cancellationToken);
            }

            public override void CopyTo(Stream destination, int bufferSize)
            {
                _innerStream.CopyTo(destination, bufferSize);
            }
        }
    }
}
