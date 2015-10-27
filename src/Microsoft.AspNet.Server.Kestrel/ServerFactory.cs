// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
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
        private readonly ILogger _logger;

        public ServerFactory(IApplicationLifetime appLifetime, ILoggerFactory loggerFactory)
        {
            _appLifetime = appLifetime;
            _logger = loggerFactory.CreateLogger("Microsoft.AspNet.Server.Kestrel");
        }

        public IFeatureCollection Initialize(IConfiguration configuration)
        {
            var information = new KestrelServerInformation();
            information.Initialize(configuration);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set<IKestrelServerInformation>(information);
            serverFeatures.Set<IServerAddressesFeature>(information);
            return serverFeatures;
        }

        public IDisposable Start(IFeatureCollection serverFeatures, Func<IFeatureCollection, Task> application)
        {
            var disposables = new Stack<IDisposable>();
            var disposer = new Disposable(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            });

            try
            {
                var information = (KestrelServerInformation)serverFeatures.Get<IKestrelServerInformation>();
                var dateHeaderValueManager = new DateHeaderValueManager();
                var engine = new KestrelEngine(new ServiceContext
                {
                    AppLifetime = _appLifetime,
                    Log = new KestrelTrace(_logger),
                    DateHeaderValueManager = dateHeaderValueManager,
                    ConnectionFilter = information.ConnectionFilter,
                    NoDelay = information.NoDelay
                });

                disposables.Push(engine);
                disposables.Push(dateHeaderValueManager);

                if (information.ThreadCount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(information.ThreadCount),
                        information.ThreadCount,
                        "ThreadCount cannot be negative");
                }

                engine.Start(information.ThreadCount == 0 ? 1 : information.ThreadCount);
                bool atLeastOneListener = false;

                foreach (var address in information.Addresses)
                {
                    var parsedAddress = ServerAddress.FromUrl(address);
                    if (parsedAddress == null)
                    {
                        throw new FormatException("Unrecognized listening address: " + address);
                    }
                    else
                    {
                        atLeastOneListener = true;
                        disposables.Push(engine.CreateServer(
                            parsedAddress,
                            application));
                    }
                }

                if (!atLeastOneListener)
                {
                    throw new InvalidOperationException("No recognized listening addresses were configured.");
                }

                return disposer;
            }
            catch
            {
                disposer.Dispose();
                throw;
            }
        }
    }
}
