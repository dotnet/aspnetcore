// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherMiddleware
    {
        private readonly ILogger _logger;
        private readonly DispatcherOptions _options;
        private readonly RequestDelegate _next;

        public DispatcherMiddleware(IOptions<DispatcherOptions> options, ILogger<DispatcherMiddleware> logger, RequestDelegate next)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _options = options.Value;
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var feature = new DispatcherFeature();
            httpContext.Features.Set<IDispatcherFeature>(feature);

            var context = new MatcherContext(httpContext);
            foreach (var entry in _options.Matchers)
            {
                await entry.Matcher.MatchAsync(context);

                if (context.ShortCircuit != null)
                {
                    feature.Endpoint = context.Endpoint;
                    feature.Values = context.Values;

                    await context.ShortCircuit(httpContext);

                    _logger.RequestShortCircuitedDispatcherMiddleware(context);
                    return;
                }

                if (context.Endpoint != null)
                {
                    _logger.EndpointMatchedDispatcherMiddleware(context.Endpoint);
                    feature.Endpoint = context.Endpoint;
                    feature.Values = context.Values;

                    feature.Handler = entry.HandlerFactory.CreateHandler(feature.Endpoint);
                    if (feature.Handler == null)
                    {
                        _logger.HandlerNotCreated(entry);
                        throw new InvalidOperationException("Couldn't create a handler, that's bad.");
                    }

                    break;
                }

                _logger.NoEndpointsMatchedMatcher(entry.Matcher);
            }

            await _next(httpContext);
        }
    }
}
