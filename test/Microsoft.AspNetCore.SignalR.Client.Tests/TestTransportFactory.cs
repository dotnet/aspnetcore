// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestTransportFactory : ITransportFactory
    {
        private readonly ITransport _transport;

        public TestTransportFactory(ITransport transport)
        {
            _transport = transport;
        }

        public ITransport CreateTransport(TransportType availableServerTransports)
        {
            return _transport;
        }
    }
}
