// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    public class InMemoryConnection : StreamBackedTestConnection
    {

        public InMemoryConnection(InMemoryTransportConnection transportConnection)
            : base(new RawStream(transportConnection.Output, transportConnection.Input))
        {
            TransportConnection = transportConnection;
        }

        public InMemoryTransportConnection TransportConnection { get; }

        public override void Reset()
        {
            TransportConnection.Input.Complete(new ConnectionResetException(string.Empty));
            TransportConnection.OnClosed();
        }

        public override void ShutdownSend()
        {
            TransportConnection.Input.Complete();
            TransportConnection.OnClosed();
        }

        public override void Dispose()
        {
            if (KestrelEventSource.Log.IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                TransportConnection.Log.LogDebug("InMemoryConnection.Dispose() started");

                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
                ThreadPool.GetAvailableThreads(out int freeWorkerThreads, out int freeIoThreads);
                ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);

                int busyIoThreads = maxIoThreads - freeIoThreads;
                int busyWorkerThreads = maxWorkerThreads - freeWorkerThreads;

                TransportConnection.Log.LogDebug("(Busy={busyIoThreads},Free={freeIoThreads},Min={minIoThreads},Max={maxIoThreads})", busyIoThreads, freeIoThreads, minIoThreads, maxIoThreads);
                TransportConnection.Log.LogDebug("(Busy={busyWorkerThreads},Free={freeWorkerThreads},Min={minWorkerThreads},Max={maxWorkerThreads})", busyWorkerThreads, freeWorkerThreads, minWorkerThreads, maxWorkerThreads);
            }

            TransportConnection.Input.Complete();
            TransportConnection.Output.Complete();
            TransportConnection.OnClosed();
            base.Dispose();

            if (KestrelEventSource.Log.IsEnabled(EventLevel.Verbose, EventKeywords.None))
            {
                TransportConnection.Log.LogDebug("InMemoryConnection.Dispose() complete");
            }
        }
    }
}
