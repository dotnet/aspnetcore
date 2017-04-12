// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.TestHelpers
{
    public class TimeoutTestServer : IDisposable
    {
        private readonly KestrelServer _server;
        private readonly ListenOptions _listenOptions;

        public TimeoutTestServer(RequestDelegate app, KestrelServerOptions serverOptions)
        {
            var loggerFactory = new KestrelTestLoggerFactory(new TestApplicationErrorLogger());
            var libuvTransportFactory = new LibuvTransportFactory(Options.Create(new LibuvTransportOptions()), new LifetimeNotImplemented(), loggerFactory);

            _listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            serverOptions.ListenOptions.Add(_listenOptions);
            _server = new KestrelServer(Options.Create(serverOptions), libuvTransportFactory, loggerFactory);

            try
            {
                _server.Start(new DummyApplication(app));
            }
            catch
            {
                _server.Dispose();
                throw;
            }
        }

        public TestConnection CreateConnection()
        {
            return new TestConnection(_listenOptions.IPEndPoint.Port, _listenOptions.IPEndPoint.AddressFamily);
        }

        public void Dispose()
        {
            _server?.Dispose();
        }
    }
}
