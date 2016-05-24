// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServer : IServer
    {
        private Stack<IDisposable> _disposables;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;
        private readonly IServerAddressesFeature _serverAddresses;

        public KestrelServer(IOptions<KestrelServerOptions> options, IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Options = options.Value ?? new KestrelServerOptions();
            _applicationLifetime = applicationLifetime;
            _logger = loggerFactory.CreateLogger(typeof(KestrelServer).GetTypeInfo().Namespace);
            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set<IServerAddressesFeature>(_serverAddresses);
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
                    ServerOptions = Options
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

                if (!Constants.ECONNRESET.HasValue)
                {
                    _logger.LogWarning("Unable to determine ECONNRESET value on this platform.");
                }
                if (!Constants.EADDRINUSE.HasValue)
                {
                    _logger.LogWarning("Unable to determine EADDRINUSE value on this platform.");
                }

                engine.Start(threadCount);
                var atLeastOneListener = false;

                foreach (var address in _serverAddresses.Addresses.ToArray())
                {
                    var parsedAddress = ServerAddress.FromUrl(address);
                    if (parsedAddress == null)
                    {
                        throw new FormatException("Unrecognized listening address: " + address);
                    }
                    else
                    {
                        atLeastOneListener = true;

                        if (!parsedAddress.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                        {
                            _disposables.Push(engine.CreateServer(
                                parsedAddress));
                        }
                        else
                        {
                            if (parsedAddress.Port == 0)
                            {
                                throw new InvalidOperationException("Dynamic port binding is not supported when binding to localhost. You must either bind to 127.0.0.1:0 or [::1]:0, or both.");
                            }

                            var ipv4Address = parsedAddress.WithHost("127.0.0.1");
                            var exceptions = new List<UvException>();

                            try
                            {
                                _disposables.Push(engine.CreateServer(ipv4Address));
                            }
                            catch (AggregateException ex)
                            {
                                var uvException = ex.InnerException as UvException;

                                if (uvException != null && uvException.StatusCode != Constants.EADDRINUSE)
                                {
                                    _logger.LogWarning(0, ex, $"Unable to bind to {parsedAddress.ToString()} on the IPv4 loopback interface.");
                                    exceptions.Add(uvException);
                                }
                                else
                                {
                                    throw;
                                }
                            }

                            var ipv6Address = parsedAddress.WithHost("[::1]");

                            try
                            {
                                _disposables.Push(engine.CreateServer(ipv6Address));
                            }
                            catch (AggregateException ex)
                            {
                                var uvException = ex.InnerException as UvException;

                                if (uvException != null && uvException.StatusCode != Constants.EADDRINUSE)
                                {
                                    _logger.LogWarning(0, ex, $"Unable to bind to {parsedAddress.ToString()} on the IPv6 loopback interface.");
                                    exceptions.Add(uvException);
                                }
                                else
                                {
                                    throw;
                                }
                            }

                            if (exceptions.Count == 2)
                            {
                                var ex = new AggregateException(exceptions);
                                _logger.LogError(0, ex, $"Unable to bind to {parsedAddress.ToString()} on any loopback interface.");
                                throw ex;
                            }
                        }

                        // If requested port was "0", replace with assigned dynamic port.
                        _serverAddresses.Addresses.Remove(address);
                        _serverAddresses.Addresses.Add(parsedAddress.ToString());
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
