// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    /// <summary>
    /// Summary description for ServerFactory
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private readonly IApplicationLifetime _appLifetime;
        private readonly ILoggerFactory _loggerFactory;

        public ServerFactory(IApplicationLifetime appLifetime, ILoggerFactory loggerFactory)
        {
            _appLifetime = appLifetime;
            _loggerFactory = loggerFactory;
        }

        public IServer CreateServer(IConfiguration configuration)
        {
            var information = new KestrelServerInformation(configuration);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set<IKestrelServerInformation>(information);
            serverFeatures.Set<IServerAddressesFeature>(information);
            return new KestrelServer(serverFeatures, _appLifetime, _loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
        }
    }
}
