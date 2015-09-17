// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for ServerFactory
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationShutdown _appShutdownService;
        private readonly ILogger _logger;

        public ServerFactory(ILibraryManager libraryManager, IApplicationShutdown appShutdownService, ILoggerFactory loggerFactory)
        {
            _libraryManager = libraryManager;
            _appShutdownService = appShutdownService;
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
                var engine = new KestrelEngine(_libraryManager, new ServiceContext { AppShutdown = _appShutdownService, Log = new KestrelTrace(_logger) });

                disposables.Push(engine);

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
                            parsedAddress.Scheme,
                            parsedAddress.Host,
                            parsedAddress.Port,
                            async frame =>
                            {
                                var request = new ServerRequest(frame);
                                await application.Invoke(request.Features).ConfigureAwait(false);
                            }));
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
