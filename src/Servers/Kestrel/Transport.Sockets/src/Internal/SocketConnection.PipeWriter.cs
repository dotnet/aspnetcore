// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection
    {
        private class SocketPipeWriter : PipeWriter
        {
            private readonly SocketConnection _socketConnection;
            private readonly PipeWriter _writer;

            public SocketPipeWriter(SocketConnection socketConnection)
            {
                _socketConnection = socketConnection;
                _writer = socketConnection.InnerTransport.Output;
            }

            public override bool CanGetUnflushedBytes => _writer.CanGetUnflushedBytes;

            public override long UnflushedBytes => _writer.UnflushedBytes;

            public override void Advance(int bytes)
            {
                _writer.Advance(bytes);
            }

            public override void CancelPendingFlush()
            {
                _writer.CancelPendingFlush();
            }

            public override void Complete(Exception? exception = null)
            {
                _writer.Complete(exception);
            }

            public override ValueTask CompleteAsync(Exception? exception = null)
            {
                return _writer.CompleteAsync(exception);
            }

            public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                _socketConnection.EnsureStarted();
                return _writer.WriteAsync(source, cancellationToken);
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                _socketConnection.EnsureStarted();
                return _writer.FlushAsync(cancellationToken);
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                return _writer.GetMemory(sizeHint);
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                return _writer.GetSpan(sizeHint);
            }
        }
    }
}
