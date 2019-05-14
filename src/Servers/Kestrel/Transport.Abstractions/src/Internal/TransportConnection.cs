// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public abstract partial class TransportConnection : ConnectionContext
    {
        private IDictionary<object, object> _items;
        private List<(Action<object> handler, object state)> _heartbeatHandlers;
        private readonly object _heartbeatLock = new object();
        protected readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();

        public TransportConnection()
        {
            FastReset();

            ConnectionClosedRequested = _connectionClosingCts.Token;
        }

        public IPAddress RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public IPAddress LocalAddress { get; set; }
        public int LocalPort { get; set; }

        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features => this;

        public virtual MemoryPool<byte> MemoryPool { get; }
        public virtual PipeScheduler InputWriterScheduler { get; }
        public virtual PipeScheduler OutputReaderScheduler { get; }

        public override IDuplexPipe Transport { get; set; }
        public IDuplexPipe Application { get; set; }

        public override IDictionary<object, object> Items
        {
            get
            {
                // Lazily allocate connection metadata
                return _items ?? (_items = new ConnectionItems());
            }
            set
            {
                _items = value;
            }
        }

        public PipeWriter Input => Application.Output;
        public PipeReader Output => Application.Input;

        public CancellationToken ConnectionClosed { get; set; }

        public CancellationToken ConnectionClosedRequested { get; set; }

        public void TickHeartbeat()
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    return;
                }

                foreach (var (handler, state) in _heartbeatHandlers)
                {
                    handler(state);
                }
            }
        }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }

                _heartbeatHandlers.Add((action, state));
            }
        }

        // DO NOT remove this override to ConnectionContext.Abort. Doing so would cause
        // any TransportConnection that does not override Abort or calls base.Abort
        // to stack overflow when IConnectionLifetimeFeature.Abort() is called.
        // That said, all derived types should override this method should override
        // this implementation of Abort because canceling pending output reads is not
        // sufficient to abort the connection if there is backpressure.
        public override void Abort(ConnectionAbortedException abortReason)
        {
            Output.CancelPendingRead();
        }

        public void RequestClose()
        {
            try
            {
                _connectionClosingCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // There's a race where the token could be disposed
                // swallow the exception and no-op
            }
        }
    }
}
