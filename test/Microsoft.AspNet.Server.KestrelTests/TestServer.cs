// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private KestrelEngine _engine;
        private IDisposable _server;

        public TestServer(Func<IFeatureCollection, Task> app)
            : this(app, new TestServiceContext())
        {
        }

        public TestServer(Func<IFeatureCollection, Task> app, ServiceContext context)
            : this(app, context, "http://localhost:54321/")
        {
        }
        public TestServer(Func<IFeatureCollection, Task> app, ServiceContext context, string serverAddress)
        {
            Create(app, context, serverAddress);
        }


        public void Create(Func<IFeatureCollection, Task> app, ServiceContext context, string serverAddress)
        {
            _engine = new KestrelEngine(context);
            _engine.Start(1);
            _server = _engine.CreateServer(
                ServerAddress.FromUrl(serverAddress),
                app);
        }

        public void Dispose()
        {
            _server.Dispose();
            _engine.Dispose();
        }
    }
}