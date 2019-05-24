// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    internal class InMemoryTransportConnection : TransportConnection
    {
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private readonly ILogger _logger;
        private bool _isClosed;

        public InMemoryTransportConnection(MemoryPool<byte> memoryPool, ILogger logger, PipeScheduler scheduler = null)
        {
            MemoryPool = memoryPool;
            _logger = logger;

            LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

            var pair = DuplexPipe.CreateConnectionPair(new PipeOptions(memoryPool, readerScheduler: scheduler), new PipeOptions(memoryPool, writerScheduler: scheduler));
            Application = pair.Application;
            Transport = pair.Transport;

            ConnectionClosed = _connectionClosedTokenSource.Token;
        }

        public override MemoryPool<byte> MemoryPool { get; }

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

        public override ValueTask DisposeAsync()
        {
            _connectionClosedTokenSource.Dispose();

            Transport.Input.Complete();
            Transport.Output.Complete();

            return base.DisposeAsync();
        }
    }
}
