// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class MessagePump : IServer
    {
        private readonly ILogger _logger;
        private readonly HttpSysOptions _options;

        private int _maxAccepts;
        private int _acceptorCounts;
        private Func<HttpSysListener, NativeRequestContext, IThreadPoolWorkItem> _getRequestWorker;

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
            Contract.Assert(_getRequestWorker == null);

            Contract.Assert(application != null);

            _getRequestWorker = (server, nativeRequestContext) => ApplicationWrapper<TContext>.RentWrapper(server, nativeRequestContext, this, application);

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
                        var worker = _getRequestWorker(requestContext.Server, requestContext.MemoryBlob);
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

        private sealed class ApplicationWrapper<TContext> : IThreadPoolWorkItem
        {
            private const int _maxPooledContexts = 512;
            private readonly static ConcurrentQueueSegment<ApplicationWrapper<TContext>> _requestContexts = new ConcurrentQueueSegment<ApplicationWrapper<TContext>>(_maxPooledContexts);

            private readonly RequestContext<TContext> _requestContext;

            private MessagePump _messagePump;
            private IHttpApplication<TContext> _application;

            private ApplicationWrapper()
            {
                _requestContext = new RequestContext<TContext>();
            }

            private void Initialize(HttpSysListener server, NativeRequestContext nativeRequestContext, MessagePump messagePump, IHttpApplication<TContext> application)
            {
                _messagePump = messagePump;
                _application = application;
                _requestContext.Initialize(server, nativeRequestContext);
            }

            public void Execute() => _ = ProcessRequestAsync();

            private async Task ProcessRequestAsync()
            {
                try
                {
                    if (_messagePump.Stopping)
                    {
                        SetFatalResponse(_requestContext, 503);
                        return;
                    }

                    TContext context = default;
                    Interlocked.Increment(ref _messagePump._outstandingRequests);
                    try
                    {
                        var featureContext = _requestContext.FeatureContext;
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
                        _requestContext.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(_messagePump._logger, "ProcessRequestAsync", ex);
                        _application.DisposeContext(context, ex);
                        if (_requestContext.Response.HasStarted)
                        {
                            _requestContext.Abort();
                        }
                        else
                        {
                            // We haven't sent a response yet, try to send a 500 Internal Server Error
                            _requestContext.Response.Headers.Clear();
                            SetFatalResponse(_requestContext, 500);
                        }
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref _messagePump._outstandingRequests) == 0 && _messagePump.Stopping)
                        {
                            LogHelper.LogInfo(_messagePump._logger, "All requests drained.");
                            _messagePump._shutdownSignal.TrySetResult(0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(_messagePump._logger, "ProcessRequestAsync", ex);
                    _requestContext?.Abort();
                }

                ReturnWrapper(this);
            }

            public static IThreadPoolWorkItem RentWrapper(HttpSysListener server, NativeRequestContext nativeRequestContext, MessagePump messagePump, IHttpApplication<TContext> application)
            {
                if (!_requestContexts.TryDequeue(out var wrapper))
                {
                    wrapper = new ApplicationWrapper<TContext>();
                }

                wrapper.Initialize(server, nativeRequestContext, messagePump, application);
                return wrapper;
            }

            private static void ReturnWrapper(ApplicationWrapper<TContext> wrapper)
            {
                wrapper._requestContext.Reset();
                wrapper._messagePump = null;
                wrapper._application = null;
                _requestContexts.TryEnqueue(wrapper);
            }
        }
    }
}
