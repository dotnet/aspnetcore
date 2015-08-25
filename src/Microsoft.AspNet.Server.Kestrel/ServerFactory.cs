// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for ServerFactory
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationShutdown _appShutdownService;

        public ServerFactory(ILibraryManager libraryManager, IApplicationShutdown appShutdownService)
        {
            _libraryManager = libraryManager;
            _appShutdownService = appShutdownService;
        }

        public IFeatureCollection Initialize(IConfiguration configuration)
        {
            var information = new KestrelServerInformation();
            information.Initialize(configuration);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set<IKestrelServerInformation>(information);
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
                var engine = new KestrelEngine(_libraryManager, _appShutdownService);

                disposables.Push(engine);

                engine.Start(information.ThreadCount == 0 ? 1 : information.ThreadCount);

                foreach (var address in information.Addresses)
                {
                    disposables.Push(engine.CreateServer(
                        address.Scheme,
                        address.Host,
                        address.Port,
                        async frame =>
                        {
                            var request = new ServerRequest(frame);
                            await application.Invoke(request.Features).ConfigureAwait(false);
                        }));
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
