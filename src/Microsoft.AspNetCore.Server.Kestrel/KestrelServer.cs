// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServer : IServer
    {
        private Stack<IDisposable> _disposables;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;

        public KestrelServer(IFeatureCollection features, KestrelServerOptions options, IApplicationLifetime applicationLifetime, ILogger logger)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _applicationLifetime = applicationLifetime;
            _logger = logger;
            Features = features;
            Options = options;
        }

        public IFeatureCollection Features { get; }

        public KestrelServerOptions Options { get; }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            if (_disposables != null)
            {
                // The server has already started and/or has not been cleaned up yet
                throw new InvalidOperationException("Server has already started.");
            }
            _disposables = new Stack<IDisposable>();

            try
            {
                var componentFactory = Features.Get<IHttpComponentFactory>();
                var dateHeaderValueManager = new DateHeaderValueManager();
                var trace = new KestrelTrace(_logger);
                var engine = new KestrelEngine(new ServiceContext
                {
                    FrameFactory = context =>
                    {
                        return new Frame<TContext>(application, context);
                    },
                    AppLifetime = _applicationLifetime,
                    Log = trace,
                    ThreadPool = new LoggingThreadPool(trace),
                    DateHeaderValueManager = dateHeaderValueManager,
                    ServerOptions = Options,
                    HttpComponentFactory = componentFactory
                });

                _disposables.Push(engine);
                _disposables.Push(dateHeaderValueManager);

                var threadCount = Options.ThreadCount;

                if (threadCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(threadCount),
                        threadCount,
                        "ThreadCount must be positive.");
                }

                engine.Start(threadCount);
                var atLeastOneListener = false;

                var addressesFeature = Features.Get<IServerAddressesFeature>();
                if (addressesFeature == null)
                {
                    throw new InvalidOperationException($"{nameof(IServerAddressesFeature)} is missing.");
                }

                foreach (var address in addressesFeature.Addresses)
                {
                    var parsedAddress = ServerAddress.FromUrl(address);
                    if (parsedAddress == null)
                    {
                        throw new FormatException("Unrecognized listening address: " + address);
                    }
                    else
                    {
                        atLeastOneListener = true;
                        _disposables.Push(engine.CreateServer(
                            parsedAddress));
                    }
                }

                if (!atLeastOneListener)
                {
                    throw new InvalidOperationException("No recognized listening addresses were configured.");
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposables != null)
            {
                while (_disposables.Count > 0)
                {
                    _disposables.Pop().Dispose();
                }
                _disposables = null;
            }
        }
    }
}
