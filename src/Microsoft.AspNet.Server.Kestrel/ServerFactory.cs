// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel
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
            var threadCount = GetThreadCount();
            var information = new KestrelServerInformation(configuration, threadCount);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set<IKestrelServerInformation>(information);
            serverFeatures.Set<IServerAddressesFeature>(information);
            return new KestrelServer(serverFeatures, _appLifetime, _loggerFactory.CreateLogger("Microsoft.AspNet.Server.Kestrel"));
        }

        private static int GetThreadCount()
        {
            // Actual core count would be a better number
            // rather than logical cores which includes hyper-threaded cores.
            // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
            // Can be user overriden using IKestrelServerInformation.ThreadCount
            var threadCount = Environment.ProcessorCount >> 1;

            if (threadCount < 1)
            {
                // Ensure shifted value is at least one
                return 1;
            }

            if (threadCount > 16)
            {
                // Receive Side Scaling RSS Processor count currently maxes out at 16
                // would be better to check the NIC's current hardware queues; but xplat...
                return 16;
            }

            return threadCount;
        }
    }
}
