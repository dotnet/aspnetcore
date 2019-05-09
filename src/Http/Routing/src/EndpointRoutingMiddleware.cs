// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;

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
            ILogger<EndpointRoutingMiddleware> logger,
            IEndpointRouteBuilder endpointRouteBuilder,
            RequestDelegate next)
        {
            if (matcherFactory == null)
            {
                throw new ArgumentNullException(nameof(matcherFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (endpointRouteBuilder == null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _matcherFactory = matcherFactory;
            _logger = logger;
            _next = next;

            _endpointDataSource = new CompositeEndpointDataSource(endpointRouteBuilder.DataSources);
        }

        public Task Invoke(HttpContext httpContext)
        {
            // There's already an endpoint, skip maching completely
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

            private static readonly Action<ILogger, string, Exception> _matchingSkipped = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "MatchingSkipped"),
                "Endpoint '{EndpointName}' already set, skipping route matching.");

            public static void MatchSuccess(ILogger logger, Endpoint endpoint)
            {
                _matchSuccess(logger, endpoint.DisplayName, null);
            }

            public static void MatchFailure(ILogger logger)
            {
                _matchFailure(logger, null);
            }

            public static void MatchSkipped(ILogger logger, Endpoint endpoint)
            {
                _matchingSkipped(logger, endpoint.DisplayName, null);
            }
        }
    }
}
