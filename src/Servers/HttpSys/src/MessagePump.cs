// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class MessagePump : IServer
    {
        private readonly ILogger _logger;
        private readonly HttpSysOptions _options;

        private int _maxAccepts;
        private int _acceptorCounts;
        private IWorkerFactory _workerFactory;

        private volatile int _stopping;
        private int _outstandingRequests;
        private readonly TaskCompletionSource<object> _shutdownSignal = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _shutdownSignalCompleted;

        private readonly ServerAddressesFeature _serverAddresses;

        public MessagePump(IOptions<HttpSysOptions> options, ILoggerFactory loggerFactory, IAuthenticationSchemeProvider authentication)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            _options = options.Value;
            Listener = new HttpSysListener(_options, loggerFactory);
            _logger = LogHelper.CreateLogger(loggerFactory, typeof(MessagePump));

            if (_options.Authentication.Schemes != AuthenticationSchemes.None)
            {
                authentication.AddScheme(new AuthenticationScheme(HttpSysDefaults.AuthenticationScheme, displayName: null, handlerType: typeof(AuthenticationHandler)));
            }

            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set<IServerAddressesFeature>(_serverAddresses);

            _maxAccepts = _options.MaxAccepts;
        }

        internal HttpSysListener Listener { get; }

        public IFeatureCollection Features { get; }

        private bool Stopping => _stopping == 1;

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var hostingUrlsPresent = _serverAddresses.Addresses.Count > 0;

            if (_serverAddresses.PreferHostingUrls && hostingUrlsPresent)
            {
                if (_options.UrlPrefixes.Count > 0)
                {
                    LogHelper.LogWarning(_logger, $"Overriding endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} since {nameof(IServerAddressesFeature.PreferHostingUrls)} is set to true." +
                        $" Binding to address(es) '{string.Join(", ", _serverAddresses.Addresses)}' instead. ");

                    Listener.Options.UrlPrefixes.Clear();
                }

                foreach (var value in _serverAddresses.Addresses)
                {
                    Listener.Options.UrlPrefixes.Add(value);
                }
            }
            else if (_options.UrlPrefixes.Count > 0)
            {
                if (hostingUrlsPresent)
                {
                    LogHelper.LogWarning(_logger, $"Overriding address(es) '{string.Join(", ", _serverAddresses.Addresses)}'. " +
                        $"Binding to endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} instead.");

                    _serverAddresses.Addresses.Clear();
                }

                foreach (var prefix in _options.UrlPrefixes)
                {
                    _serverAddresses.Addresses.Add(prefix.FullPrefix);
                }
            }
            else if (hostingUrlsPresent)
            {
                foreach (var value in _serverAddresses.Addresses)
                {
                    Listener.Options.UrlPrefixes.Add(value);
                }
            }
            else
            {
                LogHelper.LogDebug(_logger, $"No listening endpoints were configured. Binding to {Constants.DefaultServerAddress} by default.");

                _serverAddresses.Addresses.Add(Constants.DefaultServerAddress);
                Listener.Options.UrlPrefixes.Add(Constants.DefaultServerAddress);
            }

            // Can't call Start twice
            Contract.Assert(_workerFactory == null);

            Contract.Assert(application != null);

            _workerFactory = new WorkerFactory<TContext>(this, application);

            Listener.Start();

            ActivateRequestProcessingLimits();

            return Task.CompletedTask;
        }

        private void ActivateRequestProcessingLimits()
        {
            for (int i = _acceptorCounts; i < _maxAccepts; i++)
            {
                _ = ProcessRequestsWorker();
            }
        }

        // The message pump.
        // When we start listening for the next request on one thread, we may need to be sure that the
        // completion continues on another thread as to not block the current request processing.
        // The awaits will manage stack depth for us.
        private async Task ProcessRequestsWorker()
        {
            int workerIndex = Interlocked.Increment(ref _acceptorCounts);
            while (!Stopping && workerIndex <= _maxAccepts)
            {
                try
                {
                    // Receive a request
                    var requestContext = await Listener.AcceptAsync().SupressContext();
                    try
                    {
                        var worker = _workerFactory.Get(requestContext.Server, requestContext.MemoryBlob);
                        ThreadPool.UnsafeQueueUserWorkItem(worker, preferLocal: false);
                    }
                    catch (Exception ex)
                    {
                        // Request processing failed to be queued in threadpool
                        // Log the error message, release throttle and move on
                        LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                    }
                }
                catch (Exception exception)
                {
                    Contract.Assert(Stopping);
                    if (Stopping)
                    {
                        LogHelper.LogDebug(_logger, "ListenForNextRequestAsync-Stopping", exception);
                    }
                    else
                    {
                        LogHelper.LogException(_logger, "ListenForNextRequestAsync", exception);
                    }
                }
            }
            Interlocked.Decrement(ref _acceptorCounts);
        }

        private static void SetFatalResponse(RequestContext context, int status)
        {
            context.Response.StatusCode = status;
            context.Response.ContentLength = 0;
            context.Dispose();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            void RegisterCancelation()
            {
                cancellationToken.Register(() =>
                {
                    if (Interlocked.Exchange(ref _shutdownSignalCompleted, 1) == 0)
                    {
                        LogHelper.LogInfo(_logger, "Canceled, terminating " + _outstandingRequests + " request(s).");
                        _shutdownSignal.TrySetResult(null);
                    }
                });
            }

            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                RegisterCancelation();

                return _shutdownSignal.Task;
            }

            try
            {
                // Wait for active requests to drain
                if (_outstandingRequests > 0)
                {
                    LogHelper.LogInfo(_logger, "Stopping, waiting for " + _outstandingRequests + " request(s) to drain.");
                    RegisterCancelation();
                }
                else
                {
                    _shutdownSignal.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                _shutdownSignal.TrySetException(ex);
            }

            return _shutdownSignal.Task;
        }

        public void Dispose()
        {
            _stopping = 1;
            _shutdownSignal.TrySetResult(null);

            Listener.Dispose();
        }

        private interface IWorkerFactory
        {
            IThreadPoolWorkItem Get(HttpSysListener server, NativeRequestContext requestContext);
        }

        private sealed class WorkerFactory<TContext> : IWorkerFactory
        {
            private const int _maxPooledContexts = 512;
            private readonly static ConcurrentQueueSegment<ApplicationWorker> _workers = new ConcurrentQueueSegment<ApplicationWorker>(_maxPooledContexts);

            private MessagePump _messagePump;
            private IHttpApplication<TContext> _application;

            public WorkerFactory(MessagePump messagePump, IHttpApplication<TContext> application)
            {
                _messagePump = messagePump;
                _application = application;
            }

            public IThreadPoolWorkItem Get(HttpSysListener server, NativeRequestContext requestContext)
            {

                if (!_workers.TryDequeue(out var worker))
                {
                    worker = new ApplicationWorker(this);
                }

                worker.Initialize(server, requestContext, _messagePump, _application);
                return worker;
            }

            private void Return(ApplicationWorker worker)
            {
                worker.Reset();

                _workers.TryEnqueue(worker);
            }

            private sealed class ApplicationWorker : IThreadPoolWorkItem
            {
                private readonly WorkerFactory<TContext> _ownerFactory;
                private readonly RequestContext<TContext> _requestContext;

                private MessagePump _messagePump;
                private IHttpApplication<TContext> _application;

                public ApplicationWorker(WorkerFactory<TContext> ownerFactory)
                {
                    _ownerFactory = ownerFactory;
                    _requestContext = new RequestContext<TContext>();
                }

                public void Initialize(HttpSysListener server, NativeRequestContext nativeRequestContext, MessagePump messagePump, IHttpApplication<TContext> application)
                {
                    _messagePump = messagePump;
                    _application = application;
                    _requestContext.Initialize(server, nativeRequestContext);
                }

                public void Reset()
                {
                    _requestContext.Reset();
                    _messagePump = null;
                    _application = null;
                }

                public void Execute() => _ = ProcessRequestAsync();

                private async Task ProcessRequestAsync()
                {
                    var requestContext = _requestContext;
                    var messagePump = _messagePump;
                    try
                    {
                        if (messagePump.Stopping)
                        {
                            SetFatalResponse(requestContext, 503);
                            return;
                        }

                        TContext context = default;
                        Interlocked.Increment(ref messagePump._outstandingRequests);
                        try
                        {
                            var featureContext = requestContext.FeatureContext;
                            context = _application.CreateContext(featureContext.Features);
                            try
                            {
                                await _application.ProcessRequestAsync(context).SupressContext();
                                await featureContext.OnResponseStart().SupressContext();
                            }
                            finally
                            {
                                await featureContext.OnCompleted().SupressContext();
                            }
                            _application.DisposeContext(context, null);
                            requestContext.Dispose();
                            // null out the request context as we no longer own it so shouldn't call Abort in the finally blocks.
                            requestContext = null;

                            _ownerFactory.Return(this);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(messagePump._logger, "ProcessRequestAsync", ex);
                            _application.DisposeContext(context, ex);
                            if (requestContext.Response.HasStarted)
                            {
                                requestContext.Abort();
                            }
                            else
                            {
                                // We haven't sent a response yet, try to send a 500 Internal Server Error
                                requestContext.Response.Headers.Clear();
                                SetFatalResponse(requestContext, 500);
                            }
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref messagePump._outstandingRequests) == 0 && messagePump.Stopping)
                            {
                                LogHelper.LogInfo(messagePump._logger, "All requests drained.");
                                messagePump._shutdownSignal.TrySetResult(0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(messagePump._logger, "ProcessRequestAsync", ex);
                        requestContext?.Abort();
                    }
                }
            }
        }
    }
}
