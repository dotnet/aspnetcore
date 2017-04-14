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

            _transport = s_transportFactory.Create(listenOptions, new ConnectionHandler<HttpContext>(listenOptions, context, new DummyApplication(app, httpContextFactory)));

            try
            {
                _transport.BindAsync().Wait();
            }
            catch
            {
                _transport.UnbindAsync().Wait();
                _transport.StopAsync().Wait();
                _transport = null;
                throw;
            }
        }

        // Switch this to test on socket transport
        private static readonly ITransportFactory s_transportFactory = CreateLibuvTransportFactory();
//        private static readonly ITransportFactory s_transportFactory = CreateSocketTransportFactory();

        private static ITransportFactory CreateLibuvTransportFactory()
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

        private static ITransportFactory CreateSocketTransportFactory()
        {
            // For now, force the socket transport to do threadpool dispatch for tests.
            // There are a handful of tests that deadlock due to test issues if we don't do dispatch.
            // We should clean these up, but for now, make them work by forcing dispatch.
            return new SocketTransportFactory(true);
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
