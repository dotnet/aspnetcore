// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private KestrelEngine _engine;
        private IDisposable _server;
        private ListenOptions _listenOptions;
        private Frame<HttpContext> _frame;

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
            Context = context;
            _listenOptions = listenOptions;

            context.FrameFactory = connectionContext =>
            {
                _frame = new Frame<HttpContext>(new DummyApplication(app, httpContextFactory), connectionContext);
                return _frame;
            };

            try
            {
                _engine = new KestrelEngine(context);
                _engine.Start(1);
                _server = _engine.CreateServer(_listenOptions);
            }
            catch
            {
                _server?.Dispose();
                _engine?.Dispose();
                throw;
            }
        }

        public int Port => _listenOptions.IPEndPoint.Port;

        public Frame<HttpContext> Frame => _frame;

        public TestServiceContext Context { get; }

        public TestConnection CreateConnection()
        {
            return new TestConnection(Port);
        }

        public void Dispose()
        {
            _server.Dispose();
            _engine.Dispose();
        }
    }
}