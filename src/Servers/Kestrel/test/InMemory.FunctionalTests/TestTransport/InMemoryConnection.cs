// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    internal class InMemoryConnection : StreamBackedTestConnection
    {
        public InMemoryConnection(InMemoryTransportConnection transportConnection)
            : base(new DuplexPipeStream(transportConnection.Output, transportConnection.Input))
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
            TransportConnection.Input.Complete();
            TransportConnection.Output.Complete();
            TransportConnection.OnClosed();
            base.Dispose();
        }
    }
}
