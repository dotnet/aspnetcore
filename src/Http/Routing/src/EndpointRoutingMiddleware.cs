// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.ShortCircuit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed partial class EndpointRoutingMiddleware
{
    private const string DiagnosticsEndpointMatchedKey = "Microsoft.AspNetCore.Routing.EndpointMatched";

    private readonly MatcherFactory _matcherFactory;
    private readonly ILogger _logger;
    private readonly EndpointDataSource _endpointDataSource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly RoutingMetrics _metrics;
    private readonly RequestDelegate _next;
    private readonly RouteOptions _routeOptions;
    private Task<Matcher>? _initializationTask;

    public EndpointRoutingMiddleware(
        MatcherFactory matcherFactory,
        ILogger<EndpointRoutingMiddleware> logger,
        IEndpointRouteBuilder endpointRouteBuilder,
        EndpointDataSource rootCompositeEndpointDataSource,
        DiagnosticListener diagnosticListener,
        IOptions<RouteOptions> routeOptions,
        RoutingMetrics metrics,
        RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        _matcherFactory = matcherFactory ?? throw new ArgumentNullException(nameof(matcherFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));
        _metrics = metrics;
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeOptions = routeOptions.Value;

        // rootCompositeEndpointDataSource is a constructor parameter only so it always gets disposed by DI. This ensures that any
        // disposable EndpointDataSources also get disposed. _endpointDataSource is a component of rootCompositeEndpointDataSource.
        _ = rootCompositeEndpointDataSource;
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
            _metrics.MatchFailure();
        }
        else
        {
            // Raise an event if the route matched
            if (_diagnosticListener.IsEnabled() && _diagnosticListener.IsEnabled(DiagnosticsEndpointMatchedKey))
            {
                Write(_diagnosticListener, httpContext);
            }

            if (_logger.IsEnabled(LogLevel.Debug) || _metrics.MatchSuccessCounterEnabled)
            {
                var isFallback = endpoint.Metadata.GetMetadata<FallbackMetadata>() is not null;

                Log.MatchSuccess(_logger, endpoint);

                if (isFallback)
                {
                    Log.FallbackMatch(_logger, endpoint);
                }

                // It shouldn't be possible for a route to be matched via the route matcher and not have a route.
                // Just in case, add a special (missing) value as the route tag to metrics.
                var route = endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()?.Route ?? "(missing)";
                _metrics.MatchSuccess(route, isFallback);
            }

            // Map RequestSizeLimitMetadata to IHttpMaxRequestBodySizeFeature if present on the endpoint.
            // We do this during endpoint routing to ensure that successive middlewares in the pipeline
            // can access the feature with the correct value.
            SetMaxRequestBodySize(httpContext);

            var shortCircuitMetadata = endpoint.Metadata.GetMetadata<ShortCircuitMetadata>();
            if (shortCircuitMetadata is not null)
            {
                return ExecuteShortCircuit(shortCircuitMetadata, endpoint, httpContext);
            }
        }

        return _next(httpContext);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "The values being passed into Write are being consumed by the application already.")]
        static void Write(DiagnosticListener diagnosticListener, HttpContext httpContext)
        {
            // We're just going to send the HttpContext since it has all of the relevant information
            diagnosticListener.Write(DiagnosticsEndpointMatchedKey, httpContext);
        }
    }

    private Task ExecuteShortCircuit(ShortCircuitMetadata shortCircuitMetadata, Endpoint endpoint, HttpContext httpContext)
    {
        // This check should be kept in sync with the one in EndpointMiddleware
        if (!_routeOptions.SuppressCheckForUnhandledSecurityMetadata)
        {
            if (endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null)
            {
                ThrowCannotShortCircuitAnAuthRouteException(endpoint);
            }

            if (endpoint.Metadata.GetMetadata<ICorsMetadata>() is not null)
            {
                ThrowCannotShortCircuitACorsRouteException(endpoint);
            }

            if (endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: true } &&
                httpContext.Request.Method is {} method &&
                HttpExtensions.IsValidHttpMethodForForm(method))
            {
                ThrowCannotShortCircuitAnAntiforgeryRouteException(endpoint);
            }
        }

        if (shortCircuitMetadata.StatusCode.HasValue)
        {
            httpContext.Response.StatusCode = shortCircuitMetadata.StatusCode.Value;
        }

        if (endpoint.RequestDelegate is not null)
        {
            if (!_logger.IsEnabled(LogLevel.Information))
            {
                // Avoid the AwaitRequestTask state machine allocation if logging is disabled.
                return endpoint.RequestDelegate(httpContext);
            }

            Log.ExecutingEndpoint(_logger, endpoint);

            try
            {
                var requestTask = endpoint.RequestDelegate(httpContext);
                if (!requestTask.IsCompletedSuccessfully)
                {
                    return AwaitRequestTask(endpoint, requestTask, _logger);
                }
            }
            catch
            {
                Log.ExecutedEndpoint(_logger, endpoint);
                throw;
            }

            Log.ExecutedEndpoint(_logger, endpoint);

            return Task.CompletedTask;

            static async Task AwaitRequestTask(Endpoint endpoint, Task requestTask, ILogger logger)
            {
                try
                {
                    await requestTask;
                }
                finally
                {
                    Log.ExecutedEndpoint(logger, endpoint);
                }
            }

        }
        else
        {
            Log.ShortCircuitedEndpoint(_logger, endpoint);
        }
        return Task.CompletedTask;
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

    private static void ThrowCannotShortCircuitAnAuthRouteException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains authorization metadata, " +
            "but this endpoint is marked with short circuit and it will execute on Routing Middleware.");
    }

    private static void ThrowCannotShortCircuitACorsRouteException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains CORS metadata, " +
            "but this endpoint is marked with short circuit and it will execute on Routing Middleware.");
    }

    private static void ThrowCannotShortCircuitAnAntiforgeryRouteException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains anti-forgery metadata, " +
            "but this endpoint is marked with short circuit and it will execute on Routing Middleware.");
    }

    private void SetMaxRequestBodySize(HttpContext context)
    {
        var sizeLimitMetadata = context.GetEndpoint()?.Metadata?.GetMetadata<IRequestSizeLimitMetadata>();
        if (sizeLimitMetadata == null)
        {
            Log.RequestSizeLimitMetadataNotFound(_logger);
            return;
        }

        var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature == null)
        {
            Log.RequestSizeFeatureNotFound(_logger);
        }
        else if (maxRequestBodySizeFeature.IsReadOnly)
        {
            Log.RequestSizeFeatureIsReadOnly(_logger);
        }
        else
        {
            var maxRequestBodySize = sizeLimitMetadata.MaxRequestBodySize;
            maxRequestBodySizeFeature.MaxRequestBodySize = maxRequestBodySize;

            if (maxRequestBodySize.HasValue)
            {
                Log.MaxRequestBodySizeSet(_logger,
                    maxRequestBodySize.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Log.MaxRequestBodySizeDisabled(_logger);
            }
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

        [LoggerMessage(4, LogLevel.Information, "The endpoint '{EndpointName}' is being executed without running additional middleware.", EventName = "ExecutingEndpoint")]
        public static partial void ExecutingEndpoint(ILogger logger, Endpoint endpointName);

        [LoggerMessage(5, LogLevel.Information, "The endpoint '{EndpointName}' has been executed without running additional middleware.", EventName = "ExecutedEndpoint")]
        public static partial void ExecutedEndpoint(ILogger logger, Endpoint endpointName);

        [LoggerMessage(6, LogLevel.Information, "The endpoint '{EndpointName}' is being short circuited without running additional middleware or producing a response.", EventName = "ShortCircuitedEndpoint")]
        public static partial void ShortCircuitedEndpoint(ILogger logger, Endpoint endpointName);

        [LoggerMessage(7, LogLevel.Debug, "Matched endpoint '{EndpointName}' is a fallback endpoint.", EventName = "FallbackMatch")]
        public static partial void FallbackMatch(ILogger logger, Endpoint endpointName);

        [LoggerMessage(8, LogLevel.Trace, $"The endpoint does not specify the {nameof(IRequestSizeLimitMetadata)}.", EventName = "RequestSizeLimitMetadataNotFound")]
        public static partial void RequestSizeLimitMetadataNotFound(ILogger logger);

        [LoggerMessage(9, LogLevel.Warning, $"A request body size limit could not be applied. This server does not support the {nameof(IHttpMaxRequestBodySizeFeature)}.", EventName = "RequestSizeFeatureNotFound")]
        public static partial void RequestSizeFeatureNotFound(ILogger logger);

        [LoggerMessage(10, LogLevel.Warning, $"A request body size limit could not be applied. The {nameof(IHttpMaxRequestBodySizeFeature)} for the server is read-only.", EventName = "RequestSizeFeatureIsReadOnly")]
        public static partial void RequestSizeFeatureIsReadOnly(ILogger logger);

        [LoggerMessage(11, LogLevel.Debug, "The maximum request body size has been set to {RequestSize}.", EventName = "MaxRequestBodySizeSet")]
        public static partial void MaxRequestBodySizeSet(ILogger logger, string requestSize);

        [LoggerMessage(12, LogLevel.Debug, "The maximum request body size has been disabled.", EventName = "MaxRequestBodySizeDisabled")]
        public static partial void MaxRequestBodySizeDisabled(ILogger logger);
    }
}
