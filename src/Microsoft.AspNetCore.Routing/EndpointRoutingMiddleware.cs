// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class EndpointRoutingMiddleware
    {
        private readonly MatcherFactory _matcherFactory;
        private readonly ILogger _logger;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly RequestDelegate _next;

        private Task<Matcher> _initializationTask;

        public EndpointRoutingMiddleware(
            MatcherFactory matcherFactory,
            EndpointDataSource endpointDataSource,
            ILogger<EndpointRoutingMiddleware> logger,
            RequestDelegate next)
        {
            if (matcherFactory == null)
            {
                throw new ArgumentNullException(nameof(matcherFactory));
            }

            if (endpointDataSource == null)
            {
                throw new ArgumentNullException(nameof(endpointDataSource));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _matcherFactory = matcherFactory;
            _endpointDataSource = endpointDataSource;
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var feature = new EndpointSelectorContext();

            // There's an inherent race condition between waiting for init and accessing the matcher
            // this is OK because once `_matcher` is initialized, it will not be set to null again.
            var matcher = await InitializeAsync();

            await matcher.MatchAsync(httpContext, feature);
            if (feature.Endpoint != null)
            {
                // Set the endpoint feature only on success. This means we won't overwrite any
                // existing state for related features unless we did something.
                SetFeatures(httpContext, feature);

                Log.MatchSuccess(_logger, feature);
            }
            else
            {
                Log.MatchFailure(_logger);
            }

            await _next(httpContext);
        }

        private static void SetFeatures(HttpContext httpContext, EndpointSelectorContext context)
        {
            // For back-compat EndpointSelectorContext implements IEndpointFeature,
            // IRouteValuesFeature and IRoutingFeature
            httpContext.Features.Set<IRoutingFeature>(context);
            httpContext.Features.Set<IRouteValuesFeature>(context);
            httpContext.Features.Set<IEndpointFeature>(context);
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

                // Now replace the initialization task with one created with the default execution context.
                // This is important because capturing the execution context will leak memory in ASP.NET Core.
                using (ExecutionContext.SuppressFlow())
                {
                    _initializationTask = Task.FromResult(matcher);
                }

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

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _matchSuccess = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "MatchSuccess"),
                "Request matched endpoint '{EndpointName}'");

            private static readonly Action<ILogger, Exception> _matchFailure = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(2, "MatchFailure"),
                "Request did not match any endpoints");

            public static void MatchSuccess(ILogger logger, EndpointSelectorContext context)
            {
                _matchSuccess(logger, context.Endpoint.DisplayName, null);
            }

            public static void MatchFailure(ILogger logger)
            {
                _matchFailure(logger, null);
            }
        }
    }
}