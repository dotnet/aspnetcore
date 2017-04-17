// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private ITransport _transport;
        private ListenOptions _listenOptions;

        public TestServer(RequestDelegate app)
            : this(app, new TestServiceContext())
        {
        }

        public TestServer(RequestDelegate app, TestServiceContext context)
            : this(app, context, httpContextFactory: null)
        {
        }

        public TestServer(RequestDelegate app, TestServiceContext context, IHttpContextFactory httpContextFactory)
            : this(app, context, new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)), httpContextFactory)
        {
        }

        public TestServer(RequestDelegate app, TestServiceContext context, ListenOptions listenOptions)
            : this(app, context, listenOptions, null)
        {
        }

        public TestServer(RequestDelegate app, TestServiceContext context, ListenOptions listenOptions, IHttpContextFactory httpContextFactory)
        {
            _listenOptions = listenOptions;

            Context = context;

            // Switch this to test on socket transport
            var transportFactory = CreateLibuvTransportFactory(context);
            // var transportFactory = CreateSocketTransportFactory(context);

            _transport = transportFactory.Create(listenOptions, new ConnectionHandler<HttpContext>(listenOptions, context, new DummyApplication(app, httpContextFactory)));

            try
            {
                _transport.BindAsync().Wait();
            }
            catch
            {
                if (_transport != null)
                {
                    _transport.UnbindAsync().Wait();
                    _transport.StopAsync().Wait();
                    _transport = null;
                }
                throw;
            }
        }

        private static ITransportFactory CreateLibuvTransportFactory(TestServiceContext context)
        {
            var transportOptions = new LibuvTransportOptions()
            {
                ThreadCount = 1,
                ShutdownTimeout = TimeSpan.FromSeconds(5)
            };

            var transportFactory = new LibuvTransportFactory(
                Options.Create(transportOptions),
                new LifetimeNotImplemented(),
                new KestrelTestLoggerFactory(new TestApplicationErrorLogger()));

            return transportFactory;
        }

        private static ITransportFactory CreateSocketTransportFactory(TestServiceContext context)
        {
            var options = new SocketTransportOptions();

            return new SocketTransportFactory(Options.Create(options));
        }
        
        public IPEndPoint EndPoint => _listenOptions.IPEndPoint;
        public int Port => _listenOptions.IPEndPoint.Port;
        public AddressFamily AddressFamily => _listenOptions.IPEndPoint.AddressFamily;

        public TestServiceContext Context { get; }

        public TestConnection CreateConnection()
        {
            return new TestConnection(Port, AddressFamily);
        }

        public void Dispose()
        {
            _transport.UnbindAsync().Wait();
            _transport.StopAsync().Wait();
        }
    }
}
