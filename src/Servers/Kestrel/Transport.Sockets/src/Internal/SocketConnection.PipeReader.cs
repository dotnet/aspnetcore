// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection
    {
        private class SocketPipeReader : PipeReader
        {
            private readonly SocketConnection _socketConnection;
            private readonly PipeReader _reader;

            public SocketPipeReader(SocketConnection socketConnection)
            {
                _socketConnection = socketConnection;
                _reader = socketConnection.InnerTransport.Input;
            }

            public override void AdvanceTo(SequencePosition consumed)
            {
                _reader.AdvanceTo(consumed);
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                _reader.AdvanceTo(consumed, examined);
            }

            public override void CancelPendingRead()
            {
                _reader.CancelPendingRead();
            }

            public override void Complete(Exception? exception = null)
            {
                _reader.Complete(exception);
            }

            public override ValueTask CompleteAsync(Exception? exception = null)
            {
                return _reader.CompleteAsync(exception);
            }

            public override Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default)
            {
                _socketConnection.EnsureStarted();
                return _reader.CopyToAsync(destination, cancellationToken);
            }

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
            {
                _socketConnection.EnsureStarted();
                return _reader.CopyToAsync(destination, cancellationToken);
            }

            protected override ValueTask<ReadResult> ReadAtLeastAsyncCore(int minimumSize, CancellationToken cancellationToken)
            {
                _socketConnection.EnsureStarted();
                return _reader.ReadAtLeastAsync(minimumSize, cancellationToken);
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                _socketConnection.EnsureStarted();
                return _reader.ReadAsync(cancellationToken);
            }

            public override bool TryRead(out ReadResult result)
            {
                _socketConnection.EnsureStarted();
                return _reader.TryRead(out result);
            }
        }
    }
}
