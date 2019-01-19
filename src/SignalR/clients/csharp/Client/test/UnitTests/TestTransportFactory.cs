// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    internal class TestTransportFactory : ITransportFactory
    {
        private readonly ITransport _transport;

        public TestTransportFactory(ITransport transport)
        {
            _transport = transport;
        }

        public ITransport CreateTransport(HttpTransportType availableServerTransports)
        {
            return _transport;
        }
    }
}
