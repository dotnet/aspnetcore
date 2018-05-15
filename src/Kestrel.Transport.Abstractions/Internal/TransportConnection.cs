// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public partial class TransportConnection : ConnectionContext
    {
        private IDictionary<object, object> _items;

        public TransportConnection()
        {
            _currentIConnectionIdFeature = this;
            _currentIConnectionTransportFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIConnectionItemsFeature = this;
            _currentIApplicationTransportFeature = this;
            _currentIMemoryPoolFeature = this;
            _currentITransportSchedulerFeature = this;
            _currentIConnectionLifetimeFeature = this;
            _currentIBytesWrittenFeature = this;
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
        public virtual long TotalBytesWritten { get; }

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

        public virtual void Abort()
        {
        }
    }
}
