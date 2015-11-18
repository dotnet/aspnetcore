// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelServer : IServer
    {
        private Stack<IDisposable> _disposables;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;

        public KestrelServer(IFeatureCollection features, IApplicationLifetime applicationLifetime, ILogger logger)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
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
        }

        public IFeatureCollection Features { get; }

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
                var information = (KestrelServerInformation)Features.Get<IKestrelServerInformation>();
                var dateHeaderValueManager = new DateHeaderValueManager();
                var engine = new KestrelEngine(new ServiceContext
                {
                    FrameFactory = (context, remoteEP, localEP, prepareRequest) => 
                    {
                        return new Frame<TContext>(application, context, remoteEP, localEP, prepareRequest);
                    },
                    AppLifetime = _applicationLifetime,
                    Log = new KestrelTrace(_logger),
                    DateHeaderValueManager = dateHeaderValueManager,
                    ConnectionFilter = information.ConnectionFilter,
                    NoDelay = information.NoDelay
                });

                _disposables.Push(engine);
                _disposables.Push(dateHeaderValueManager);

                // Actual core count would be a better number 
                // rather than logical cores which includes hyper-threaded cores.
                // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
                // Can be user overriden using IKestrelServerInformation.ThreadCount
                var threadCount = Environment.ProcessorCount >> 1;

                if (threadCount < 1)
                {
                    // Ensure shifted value is at least one
                    threadCount = 1;
                }
                else if (threadCount > 16)
                {
                    // Receive Side Scaling RSS Processor count currently maxes out at 16
                    // would be better to check the NIC's current hardware queues; but xplat...
                    threadCount = 16;
                }

                if (information.ThreadCount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(information.ThreadCount),
                        information.ThreadCount,
                        "ThreadCount cannot be negative");
                }
                else if (information.ThreadCount > 0)
                {
                    // ThreadCount has been user set, use that value
                    threadCount = information.ThreadCount;
                }

                engine.Start(threadCount);
                var atLeastOneListener = false;

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
