// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    public class InMemoryTransportConnection : TransportConnection, IDisposable
    {
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private readonly ILogger _logger;
        private bool _isClosed;

        public InMemoryTransportConnection(MemoryPool<byte> memoryPool, ILogger logger)
        {
            MemoryPool = memoryPool;
            _logger = logger;

            LocalAddress = IPAddress.Loopback;
            RemoteAddress = IPAddress.Loopback;

            ConnectionClosed = _connectionClosedTokenSource.Token;
        }

        public override MemoryPool<byte> MemoryPool { get; }

        public override PipeScheduler InputWriterScheduler => PipeScheduler.ThreadPool;
        public override PipeScheduler OutputReaderScheduler => PipeScheduler.ThreadPool;

        public ConnectionAbortedException AbortReason { get; private set; }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _logger.LogDebug(@"Connection id ""{ConnectionId}"" closing because: ""{Message}""", ConnectionId, abortReason?.Message);

            Input.Complete(abortReason);

            AbortReason = abortReason;
        }

        public void OnClosed()
        {
            if (_isClosed)
            {
                return;
            }

            _isClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                var self = (InMemoryTransportConnection)state;
                self._connectionClosedTokenSource.Cancel();
            }, this);
        }

        public void Dispose()
        {
            _connectionClosedTokenSource.Dispose();
        }
    }
}
