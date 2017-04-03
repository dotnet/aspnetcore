// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private KestrelEngine _engine;
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
            Context.TransportContext.ConnectionHandler = new ConnectionHandler<HttpContext>(listenOptions, Context, new DummyApplication(app, httpContextFactory));

            try
            {
                _engine = new KestrelEngine(context.TransportContext, _listenOptions);
                _engine.BindAsync().Wait();
            }
            catch
            {
                _engine.UnbindAsync().Wait();
                _engine.StopAsync().Wait();
                throw;
            }
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
            _engine.UnbindAsync().Wait();
            _engine.StopAsync().Wait();
        }
    }
}
