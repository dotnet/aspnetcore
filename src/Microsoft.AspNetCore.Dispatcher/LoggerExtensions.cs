// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Dispatcher
{
    internal static class LoggerExtensions
    {
        // MatcherBase
        private static readonly Action<ILogger, string, Exception> _ambiguousEndpoints = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(0, "AmbiguousEndpoints"),
            "Request matched multiple endpoints resulting in ambiguity. Matching endpoints: {AmbiguousEndpoints}");

        private static readonly Action<ILogger, PathString, Exception> _noEndpointsMatched = LoggerMessage.Define<PathString>(
            LogLevel.Debug,
            new EventId(1, "NoEndpointsMatched"),
            "No endpoints matched the current request path '{PathString}'.");

        private static readonly Action<ILogger, string, Exception> _requestShortCircuitedMatcherBase = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, "RequestShortCircuited_MatcherBase"),
            "The current request '{RequestPath}' was short circuited.");

        private static readonly Action<ILogger, string, Exception> _endpointMatchedMatcherBase = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3, "EndpointMatched_MatcherBase"),
            "Request matched endpoint '{endpointName}'.");

        // DispatcherMiddleware
        private static readonly Action<ILogger, Type, Exception> _handlerNotCreated = LoggerMessage.Define<Type>(
            LogLevel.Error,
            new EventId(0, "HandlerNotCreated"),
            "A handler could not be created for '{MatcherType}'.");

        private static readonly Action<ILogger, string, Exception> _requestShortCircuitedDispatcherMiddleware = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "RequestShortCircuited_DispatcherMiddleware"),
            "The current request '{RequestPath}' was short circuited.");

        private static readonly Action<ILogger, string, Exception> _endpointMatchedDispatcherMiddleware = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, "EndpointMatched_DispatcherMiddleware"),
            "Request matched endpoint '{endpointName}'.");

        private static readonly Action<ILogger, IMatcher, Exception> _noEndpointsMatchedMatcher = LoggerMessage.Define<IMatcher>(
            LogLevel.Debug,
            new EventId(3, "NoEndpointsMatchedMatcher"),
            "No endpoints matched matcher '{Matcher}'.");

        //EndpointMiddleware
        private static readonly Action<ILogger, string, Exception> _executingEndpoint = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(0, "ExecutingEndpoint"),
            "Executing endpoint '{EndpointName}'.");

        private static readonly Action<ILogger, string, Exception> _executedEndpoint = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "ExecutedEndpoint"),
            "Executed endpoint '{EndpointName}'.");

        // HttpMethodEndpointSelector
        private static readonly Action<ILogger, string, Exception> _noHttpMethodFound = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(0, "NoHttpMethodFound"),
            "No HTTP method specified for endpoint '{EndpointName}'.");

        private static readonly Action<ILogger, string, string, Exception> _requestMethodMatchedEndpointMethod = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "RequestMethodMatchedEndpointMethod"),
            "Request method matched HTTP method '{Method}' for endpoint '{EndpointName}'.");

        private static readonly Action<ILogger, string, string, string, Exception> _requestMethodDidNotMatchEndpointMethod = LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2, "RequestMethodDidNotMatchEndpointMethod"),
            "Request method '{RequestMethod}' did not match HTTP method '{EndpointMethod}' for endpoint '{EndpointName}'.");

        private static readonly Action<ILogger, string, Exception> _noEndpointMatchedRequestMethod = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3, "NoEndpointMatchedRequestMethod"),
            "No endpoint matched request method '{Method}'.");

        // TreeMatcher
        private static readonly Action<ILogger, string, Exception> _requestShortCircuited = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3, "RequestShortCircuited"),
            "The current request '{RequestPath}' was short circuited.");

        private static readonly Action<ILogger, string, Exception> _matchedRoute = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Request successfully matched the route pattern '{RoutePattern}'.");

        private static readonly Action<ILogger, object, string, IDispatcherValueConstraint, Exception> _routeValueDoesNotMatchConstraint = LoggerMessage.Define<object, string, IDispatcherValueConstraint>(
                LogLevel.Debug,
                1,
                "Route value '{RouteValue}' with key '{RouteKey}' did not match the constraint '{RouteConstraint}'.");

        public static void RouteValueDoesNotMatchConstraint(
            this ILogger logger,
            object routeValue,
            string routeKey,
            IDispatcherValueConstraint routeConstraint)
        {
            _routeValueDoesNotMatchConstraint(logger, routeValue, routeKey, routeConstraint, null);
        }

        public static void RequestShortCircuited(this ILogger logger, MatcherContext matcherContext)
        {
            var requestPath = matcherContext.HttpContext.Request.Path;
            _requestShortCircuited(logger, requestPath, null);
        }

        public static void MatchedRoute(
            this ILogger logger,
            string routePattern)
        {
            _matchedRoute(logger, routePattern, null);
        }

        public static void AmbiguousEndpoints(this ILogger logger, string ambiguousEndpoints)
        {
            _ambiguousEndpoints(logger, ambiguousEndpoints, null);
        }

        public static void EndpointMatchedMatcherBase(this ILogger logger, Endpoint endpoint)
        {
            _endpointMatchedMatcherBase(logger, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void NoEndpointsMatched(this ILogger logger, PathString pathString)
        {
            _noEndpointsMatched(logger, pathString, null);
        }

        public static void RequestShortCircuitedMatcherBase(this ILogger logger, MatcherContext matcherContext)
        {
            var requestPath = matcherContext.HttpContext.Request.Path;
            _requestShortCircuitedMatcherBase(logger, requestPath, null);
        }

        public static void EndpointMatchedDispatcherMiddleware(this ILogger logger, Endpoint endpoint)
        {
            _endpointMatchedDispatcherMiddleware(logger, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void RequestShortCircuitedDispatcherMiddleware(this ILogger logger, MatcherContext matcherContext)
        {
            var requestPath = matcherContext.HttpContext.Request.Path;
            _requestShortCircuitedDispatcherMiddleware(logger, requestPath, null);
        }

        public static void HandlerNotCreated(this ILogger logger, MatcherEntry matcher)
        {
            var matcherType = matcher.GetType();
            _handlerNotCreated(logger, matcherType, null);
        }

        public static void NoEndpointsMatchedMatcher(this ILogger logger, IMatcher matcher)
        {
            _noEndpointsMatchedMatcher(logger, matcher, null);
        }

        public static void ExecutingEndpoint(this ILogger logger, Endpoint endpoint)
        {
            _executingEndpoint(logger, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void ExecutedEndpoint(this ILogger logger, Endpoint endpoint)
        {
            _executedEndpoint(logger, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void NoHttpMethodFound(this ILogger logger, Endpoint endpoint)
        {
            _noHttpMethodFound(logger, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void RequestMethodMatchedEndpointMethod(this ILogger logger, string httpMethod, Endpoint endpoint)
        {
            _requestMethodMatchedEndpointMethod(logger, httpMethod, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void RequestMethodDidNotMatchEndpointMethod(this ILogger logger, string requestMethod, string endpointMethod, Endpoint endpoint)
        {
            _requestMethodDidNotMatchEndpointMethod(logger, requestMethod, endpointMethod, endpoint.DisplayName ?? "Unnamed endpoint", null);
        }

        public static void NoEndpointMatchedRequestMethod(this ILogger logger, string requestMethod)
        {
            _noEndpointMatchedRequestMethod(logger, requestMethod, null);
        }
    }
}
