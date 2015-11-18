// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private KestrelEngine _engine;
        private IDisposable _server;

        public TestServer(RequestDelegate app)
            : this(app, new TestServiceContext())
        {
        }

        public TestServer(RequestDelegate app, ServiceContext context)
            : this(app, context, "http://localhost:54321/")
        {
        }
        public TestServer(RequestDelegate app, ServiceContext context, string serverAddress)
        {
            Create(app, context, serverAddress);
        }


        public void Create(RequestDelegate app, ServiceContext context, string serverAddress)
        {
            context.FrameFactory = (connectionContext, remoteEP, localEP, prepareRequest) => 
            {
                return new Frame<HttpContext>(new DummyApplication(app), connectionContext, remoteEP, localEP, prepareRequest);
            };
            _engine = new KestrelEngine(context);
            _engine.Start(1);
            _server = _engine.CreateServer(
                ServerAddress.FromUrl(serverAddress));
        }

        public void Dispose()
        {
            _server.Dispose();
            _engine.Dispose();
        }
    }
}