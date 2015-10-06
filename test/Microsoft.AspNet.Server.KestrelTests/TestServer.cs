// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
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

        ILibraryManager LibraryManager
        {
            get
            {
                try
                {
                    var locator = CallContextServiceLocator.Locator;
                    if (locator == null)
                    {
                        return null;
                    }
                    var services = locator.ServiceProvider;
                    if (services == null)
                    {
                        return null;
                    }
                    return (ILibraryManager)services.GetService(typeof(ILibraryManager));
                }
                catch (NullReferenceException) { return null; }
            }
        }

        public void Create(Func<IFeatureCollection, Task> app, ServiceContext context, string serverAddress)
        {
            _engine = new KestrelEngine(
                LibraryManager, 
                context);
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