// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing;

internal sealed partial class EndpointRoutingMiddleware
{
    private const string DiagnosticsEndpointMatchedKey = "Microsoft.AspNetCore.Routing.EndpointMatched";

    private readonly MatcherFactory _matcherFactory;
    private readonly ILogger _logger;
    private readonly EndpointDataSource _endpointDataSource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly RequestDelegate _next;

    private Task<Matcher>? _initializationTask;

    public EndpointRoutingMiddleware(
        MatcherFactory matcherFactory,
        ILogger<EndpointRoutingMiddleware> logger,
        IEndpointRouteBuilder endpointRouteBuilder,
        DiagnosticListener diagnosticListener,
        RequestDelegate next)
    {
        if (endpointRouteBuilder == null)
        {
            throw new ArgumentNullException(nameof(endpointRouteBuilder));
        }

        _matcherFactory = matcherFactory ?? throw new ArgumentNullException(nameof(matcherFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));
        _next = next ?? throw new ArgumentNullException(nameof(next));

        _endpointDataSource = new CompositeEndpointDataSource(endpointRouteBuilder.DataSources);
    }

    public Task Invoke(HttpContext httpContext)
    {
        // There's already an endpoint, skip matching completely
        var endpoint = httpContext.GetEndpoint();
        if (endpoint != null)
        {
            Log.MatchSkipped(_logger, endpoint);
            return _next(httpContext);
        }

        // There's an inherent race condition between waiting for init and accessing the matcher
        // this is OK because once `_matcher` is initialized, it will not be set to null again.
        var matcherTask = InitializeAsync();
        if (!matcherTask.IsCompletedSuccessfully)
        {
            return AwaitMatcher(this, httpContext, matcherTask);
        }

        var matchTask = matcherTask.Result.MatchAsync(httpContext);
        if (!matchTask.IsCompletedSuccessfully)
        {
            return AwaitMatch(this, httpContext, matchTask);
        }

        return SetRoutingAndContinue(httpContext);

        // Awaited fallbacks for when the Tasks do not synchronously complete
        static async Task AwaitMatcher(EndpointRoutingMiddleware middleware, HttpContext httpContext, Task<Matcher> matcherTask)
        {
            var matcher = await matcherTask;
            await matcher.MatchAsync(httpContext);
            await middleware.SetRoutingAndContinue(httpContext);
        }

        static async Task AwaitMatch(EndpointRoutingMiddleware middleware, HttpContext httpContext, Task matchTask)
        {
            await matchTask;
            await middleware.SetRoutingAndContinue(httpContext);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task SetRoutingAndContinue(HttpContext httpContext)
    {
        // If there was no mutation of the endpoint then log failure
        var endpoint = httpContext.GetEndpoint();
        if (endpoint == null)
        {
            Log.MatchFailure(_logger);
        }
        else
        {
            // Raise an event if the route matched
            if (_diagnosticListener.IsEnabled() && _diagnosticListener.IsEnabled(DiagnosticsEndpointMatchedKey))
            {
                // We're just going to send the HttpContext since it has all of the relevant information
                _diagnosticListener.Write(DiagnosticsEndpointMatchedKey, httpContext);
            }

            Log.MatchSuccess(_logger, endpoint);
        }

        return _next(httpContext);
    }

    // Initialization is async to avoid blocking threads while reflection and things
    // of that nature take place.
    //
    // We've seen cases where startup is very slow if we  allow multiple threads to race
    // while initializing the set of endpoints/routes. Doing CPU intensive work is a
    // blocking operation if you have a low core count and enough work to do.
    private Task<Matcher> InitializeAsync()
    {
        var initializationTask = _initializationTask;
        if (initializationTask != null)
        {
            return initializationTask;
        }

        return InitializeCoreAsync();
    }

    private Task<Matcher> InitializeCoreAsync()
    {
        var initialization = new TaskCompletionSource<Matcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initializationTask = Interlocked.CompareExchange(ref _initializationTask, initialization.Task, null);
        if (initializationTask != null)
        {
            // This thread lost the race, join the existing task.
            return initializationTask;
        }

        // This thread won the race, do the initialization.
        try
        {
            var matcher = _matcherFactory.CreateMatcher(_endpointDataSource);

            _initializationTask = Task.FromResult(matcher);

            // Complete the task, this will unblock any requests that came in while initializing.
            initialization.SetResult(matcher);
            return initialization.Task;
        }
        catch (Exception ex)
        {
            // Allow initialization to occur again. Since DataSources can change, it's possible
            // for the developer to correct the data causing the failure.
            _initializationTask = null;

            // Complete the task, this will throw for any requests that came in while initializing.
            initialization.SetException(ex);
            return initialization.Task;
        }
    }

    private static partial class Log
    {
        public static void MatchSuccess(ILogger logger, Endpoint endpoint)
            => MatchSuccess(logger, endpoint.DisplayName);

        [LoggerMessage(1, LogLevel.Debug, "Request matched endpoint '{EndpointName}'", EventName = "MatchSuccess")]
        private static partial void MatchSuccess(ILogger logger, string? endpointName);

        [LoggerMessage(2, LogLevel.Debug, "Request did not match any endpoints", EventName = "MatchFailure")]
        public static partial void MatchFailure(ILogger logger);

        public static void MatchSkipped(ILogger logger, Endpoint endpoint)
            => MatchingSkipped(logger, endpoint.DisplayName);

        [LoggerMessage(3, LogLevel.Debug, "Endpoint '{EndpointName}' already set, skipping route matching.", EventName = "MatchingSkipped")]
        private static partial void MatchingSkipped(ILogger logger, string? endpointName);
    }
}
