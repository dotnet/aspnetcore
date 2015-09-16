// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class Application : IApplication
    {
        private readonly IDisposable _stop;
        private readonly IServiceProvider _services;
        private readonly IFeatureCollection _server;

        public Application(IServiceProvider services, IFeatureCollection server,  IDisposable stop)
        {
            _services = services;
            _server = server;
            _stop = stop;
        }

        public IFeatureCollection ServerFeatures
        {
            get { return _server; }
        }

        public IServiceProvider Services
        {
            get { return _services; }
        }

        public void Dispose()
        {
            _stop.Dispose();
        }
    }
}
