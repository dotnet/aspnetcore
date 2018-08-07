// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    public class InMemoryConnection : StreamBackedTestConnection
    {
        private readonly InMemoryTransportConnection _transportConnection;

        public InMemoryConnection(InMemoryTransportConnection transportConnection)
            : base(new RawStream(transportConnection.Output, transportConnection.Input))
        {
            _transportConnection = transportConnection;
        }

        public override void Reset()
        {
            _transportConnection.Input.Complete(new ConnectionResetException(string.Empty));
            _transportConnection.OnClosed();
        }

        public override void ShutdownSend()
        {
            _transportConnection.Input.Complete();
            _transportConnection.OnClosed();
        }

        public override void Dispose()
        {
            _transportConnection.Input.Complete();
            _transportConnection.Output.Complete();
            _transportConnection.OnClosed();
            base.Dispose();
        }
    }
}
