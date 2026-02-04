// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class MessagePump : IServer, IServerDelegationFeature
{
    private readonly ILogger _logger;
    private readonly HttpSysOptions _options;

    private readonly int _maxAccepts;

    // Shutdown coordination - _acceptLoopCount and _acceptLoopsTcs protected by _shutdownLock
    private readonly Lock _shutdownLock = new();
    private volatile bool _stopping; // Volatile for lock-free reads in hot path
    private int _acceptLoopCount;
    private readonly TaskCompletionSource _acceptLoopsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _stopTask;

    // Request counting - lock-free for performance (hot path)
    private int _outstandingRequests;
    private readonly TaskCompletionSource _requestsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly ServerAddressesFeature _serverAddresses;

    public MessagePump(IOptions<HttpSysOptions> options, IMemoryPoolFactory<byte> memoryPoolFactory,
        ILoggerFactory loggerFactory, IAuthenticationSchemeProvider authentication)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _options = options.Value;
        Listener = new HttpSysListener(_options, memoryPoolFactory, loggerFactory);
        _logger = loggerFactory.CreateLogger<MessagePump>();

        if (_options.Authentication.Schemes != AuthenticationSchemes.None)
        {
            authentication.AddScheme(new AuthenticationScheme(HttpSysDefaults.AuthenticationScheme, displayName: _options.Authentication.AuthenticationDisplayName, handlerType: typeof(AuthenticationHandler)));
        }

        Features = new FeatureCollection();
        _serverAddresses = new ServerAddressesFeature();
        Features.Set<IServerAddressesFeature>(_serverAddresses);

        if (HttpApi.SupportsDelegation)
        {
            Features.Set<IServerDelegationFeature>(this);
        }

        _maxAccepts = _options.MaxAccepts;
    }

    internal HttpSysListener Listener { get; }

    internal IRequestContextFactory? RequestContextFactory { get; set; }

    public IFeatureCollection Features { get; }

    internal bool Stopping => _stopping;

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        ArgumentNullException.ThrowIfNull(application);

        var hostingUrlsPresent = _serverAddresses.Addresses.Count > 0;
        var serverAddressCopy = _serverAddresses.Addresses.ToList();
        _serverAddresses.Addresses.Clear();

        if (_serverAddresses.PreferHostingUrls && hostingUrlsPresent)
        {
            if (_options.UrlPrefixes.Count > 0)
            {
                Log.ClearedPrefixes(_logger, _serverAddresses.Addresses);

                Listener.Options.UrlPrefixes.Clear();
            }

            UpdateUrlPrefixes(serverAddressCopy);
        }
        else if (_options.UrlPrefixes.Count > 0)
        {
            if (hostingUrlsPresent)
            {
                Log.ClearedAddresses(_logger, _serverAddresses.Addresses);

                _serverAddresses.Addresses.Clear();
            }
        }
        else if (hostingUrlsPresent)
        {
            UpdateUrlPrefixes(serverAddressCopy);
        }
        else if (Listener.RequestQueue.Created)
        {
            Log.BindingToDefault(_logger);

            Listener.Options.UrlPrefixes.Add(Constants.DefaultServerAddress);
        }
        // else // Attaching to an existing queue, don't add a default.

        // Can't start twice
        Debug.Assert(RequestContextFactory == null, "Start called twice!");

        Debug.Assert(application != null);

        RequestContextFactory = new ApplicationRequestContextFactory<TContext>(application, this);

        Listener.Start();

        // Update server addresses after we start listening as port 0
        // needs to be selected at the point of binding.
        foreach (var prefix in _options.UrlPrefixes)
        {
            _serverAddresses.Addresses.Add(prefix.FullPrefix);
        }

        // Dispatch to get off the SynchronizationContext and use UnsafeQueueUserWorkItem to avoid capturing the ExecutionContext
        ThreadPool.UnsafeQueueUserWorkItem(state => state.ActivateRequestProcessingLimits(), this, preferLocal: false);

        return Task.CompletedTask;
    }

    private void ActivateRequestProcessingLimits()
    {
        for (var i = 0; i < _maxAccepts; i++)
        {
            ProcessRequestsWorker();
        }
    }

    private void UpdateUrlPrefixes(IList<string> serverAddressCopy)
    {
        foreach (var value in serverAddressCopy)
        {
            Listener.Options.UrlPrefixes.Add(value);
        }
    }

    internal int IncrementOutstandingRequest()
    {
        return Interlocked.Increment(ref _outstandingRequests);
    }

    internal int DecrementOutstandingRequest()
    {
        var count = Interlocked.Decrement(ref _outstandingRequests);

        // Only signal after accept loops have completed to prevent early signaling
        // while new requests can still be accepted.
        if (count == 0 && _acceptLoopsCompleted.Task.IsCompleted)
        {
            _requestsDrained.TrySetResult();
        }

        return count;
    }

    // The message pump.
    // When we start listening for the next request on one thread, we may need to be sure that the
    // completion continues on another thread as to not block the current request processing.
    // The awaits will manage stack depth for us.
    private void ProcessRequestsWorker()
    {
        Debug.Assert(RequestContextFactory != null);

        lock (_shutdownLock)
        {
            if (_stopping)
            {
                return;
            }
            _acceptLoopCount++;
        }

        // Allocate and accept context per loop and reuse it for all accepts
        var acceptContext = new AsyncAcceptContext(Listener, RequestContextFactory, _logger);

        var loop = new AcceptLoop(acceptContext, this);

        ThreadPool.UnsafeQueueUserWorkItem(loop, preferLocal: false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task shutdownTask;
        lock (_shutdownLock)
        {
            if (_stopTask is not null)
            {
                shutdownTask = _stopTask;
            }
            else
            {
                _stopping = true;

                // If no accept loops were started, signal completion immediately
                if (_acceptLoopCount == 0)
                {
                    _acceptLoopsCompleted.TrySetResult();
                }

                _stopTask = shutdownTask = StopAsyncCore();
            }
        }

        // Register cancellation for all callers (not just the first).
        // Any caller's canceled token should unblock the shutdown.
        using var registration = cancellationToken.Register(static state =>
        {
            var pump = (MessagePump)state!;
            Log.StopCancelled(pump._logger, pump._outstandingRequests);
            pump._requestsDrained.TrySetResult();
        }, this);

        await shutdownTask.ConfigureAwait(false);
    }

    private async Task StopAsyncCore()
    {
        // Shutdown the request queue to cancel pending accept operations.
        // This will cause the accept loops to wake up with an error and exit.
        Listener.RequestQueue.StopProcessingRequests();

        // Wait for accept loops to complete before disposing the listener.
        // This prevents a race where the BoundHandle is disposed while
        // AsyncAcceptContext is still trying to use it for cleanup.
        // After this completes, DecrementOutstandingRequest can signal _requestsDrained
        // (it checks _acceptLoopsTcs.Task.IsCompleted).
        await _acceptLoopsCompleted.Task.ConfigureAwait(false);

        // Signal request drain completion if no requests are outstanding,
        // otherwise the last request to complete will signal it.
        // Important to do this after accept loops complete to avoid hanging until cancellation
        if (Interlocked.CompareExchange(ref _outstandingRequests, 0, 0) == 0)
        {
            _requestsDrained.TrySetResult();
        }
        else
        {
            Log.WaitingForRequestsToDrain(_logger, _outstandingRequests);
        }

        await _requestsDrained.Task.ConfigureAwait(false);

        Listener.Dispose();
    }

    public DelegationRule CreateDelegationRule(string queueName, string uri)
    {
        var rule = new DelegationRule(Listener.UrlGroup, queueName, uri, _logger);
        Listener.UrlGroup.SetDelegationProperty(rule.Queue);
        return rule;
    }

    // Ungraceful shutdown
    public void Dispose()
    {
        StopAsync(new CancellationToken(canceled: true)).GetAwaiter().GetResult();
    }

    private void AcceptLoopCompleted()
    {
        lock (_shutdownLock)
        {
            if (--_acceptLoopCount == 0)
            {
                _acceptLoopsCompleted.TrySetResult();
            }
        }
    }

    private sealed class AcceptLoop : IThreadPoolWorkItem
    {
        private readonly AsyncAcceptContext _asyncAcceptContext;
        private readonly MessagePump _messagePump;
        private readonly bool _preferInlineScheduling;

        public AcceptLoop(AsyncAcceptContext asyncAcceptContext,
                          MessagePump messagePump)
        {
            _asyncAcceptContext = asyncAcceptContext;
            _messagePump = messagePump;
            _preferInlineScheduling = _messagePump._options.UnsafePreferInlineScheduling;
        }

        public void Execute()
        {
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            while (!_messagePump.Stopping)
            {
                // Receive a request
                RequestContext requestContext;
                try
                {
                    requestContext = await _messagePump.Listener.AcceptAsync(_asyncAcceptContext);

                    if (!_messagePump.Listener.ValidateRequest(requestContext))
                    {
                        // Dispose the request
                        requestContext.ReleasePins();
                        requestContext.Dispose();

                        // If either of these is false then a response has already been sent to the client, so we can accept the next request
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(_messagePump.Stopping);
                    if (_messagePump.Stopping)
                    {
                        Log.AcceptErrorStopping(_messagePump._logger, ex);
                    }
                    else
                    {
                        Log.AcceptError(_messagePump._logger, ex);
                    }
                    continue;
                }

                // Increment BEFORE queuing to prevent race where the queued accept loop
                // exits and signals completion before we've counted this request.
                _messagePump.IncrementOutstandingRequest();

                if (_preferInlineScheduling)
                {
                    await HandleRequest(requestContext);
                }
                else
                {
                    // Queue another accept before we execute the request
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

                    await HandleRequest(requestContext);

                    // We're done with this thread, accept loop was continued via ThreadPool.UnsafeQueueUserWorkItem
                    return;
                }
            }

            // Only dispose and signal completion when the loop is actually done (not re-queued)
            _asyncAcceptContext.Dispose();
            _messagePump.AcceptLoopCompleted();

            async Task HandleRequest(RequestContext requestContext)
            {
                try
                {
                    // Use this thread to start the execution of the request (avoid the double threadpool dispatch)
                    await requestContext.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    // Request processing failed
                    // Log the error message, release throttle and move on
                    Log.RequestListenerProcessError(_messagePump._logger, ex);
                }
                finally
                {
                    _messagePump.DecrementOutstandingRequest();
                }
            }
        }
    }
}
