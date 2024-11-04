// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
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

    private volatile int _stopping;
    private int _outstandingRequests;
    private readonly TaskCompletionSource _shutdownSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _shutdownSignalCompleted;

    private readonly ServerAddressesFeature _serverAddresses;

    public MessagePump(IOptions<HttpSysOptions> options, ILoggerFactory loggerFactory, IAuthenticationSchemeProvider authentication)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _options = options.Value;
        Listener = new HttpSysListener(_options, loggerFactory);
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

    internal bool Stopping => _stopping == 1;

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
        return Interlocked.Decrement(ref _outstandingRequests);
    }

    internal void SetShutdownSignal()
    {
        _shutdownSignal.TrySetResult();
    }

    // The message pump.
    // When we start listening for the next request on one thread, we may need to be sure that the
    // completion continues on another thread as to not block the current request processing.
    // The awaits will manage stack depth for us.
    private void ProcessRequestsWorker()
    {
        Debug.Assert(RequestContextFactory != null);

        // Allocate and accept context per loop and reuse it for all accepts
        var acceptContext = new AsyncAcceptContext(Listener, RequestContextFactory, _logger);

        var loop = new AcceptLoop(acceptContext, this);

        ThreadPool.UnsafeQueueUserWorkItem(loop, preferLocal: false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        void RegisterCancelation()
        {
            cancellationToken.Register(() =>
            {
                if (Interlocked.Exchange(ref _shutdownSignalCompleted, 1) == 0)
                {
                    Log.StopCancelled(_logger, _outstandingRequests);
                    _shutdownSignal.TrySetResult();
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
                Log.WaitingForRequestsToDrain(_logger, _outstandingRequests);
                RegisterCancelation();
            }
            else
            {
                _shutdownSignal.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            _shutdownSignal.TrySetException(ex);
        }

        return _shutdownSignal.Task;
    }

    public DelegationRule CreateDelegationRule(string queueName, string uri)
    {
        var rule = new DelegationRule(Listener.UrlGroup, queueName, uri, _logger);
        Listener.UrlGroup.SetDelegationProperty(rule.Queue);
        return rule;
    }

    public void Dispose()
    {
        _stopping = 1;
        _shutdownSignal.TrySetResult();

        Listener.Dispose();
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

                if (_preferInlineScheduling)
                {
                    try
                    {
                        await requestContext.ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        // Request processing failed
                        // Log the error message, release throttle and move on
                        Log.RequestListenerProcessError(_messagePump._logger, ex);
                    }
                }
                else
                {
                    try
                    {
                        // Queue another accept before we execute the request
                        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

                        // Use this thread to start the execution of the request (avoid the double threadpool dispatch)
                        await requestContext.ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        // Request processing failed
                        // Log the error message, release throttle and move on
                        Log.RequestListenerProcessError(_messagePump._logger, ex);
                    }

                    // We're done with this thread, accept loop was continued via ThreadPool.UnsafeQueueUserWorkItem
                    return;
                }
            }

            _asyncAcceptContext.Dispose();
        }
    }
}
