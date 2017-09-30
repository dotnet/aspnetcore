// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Dispatcher
{
    internal static class DispatcherLoggerExtensions
    {
        // Too many matches
        private static readonly Action<ILogger, string, Exception> _ambiguousEndpoints = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, "AmbiguousEndpoints"),
            "Request matched multiple endpoints resulting in ambiguity. Matching endpoints: {AmbiguousEndpoints}");

        // Unique match found
        private static readonly Action<ILogger, string, Exception> _endpointMatched = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "EndpointMatched"),
            "Request matched endpoint: {endpointName}");

        private static readonly Action<ILogger, PathString, Exception> _noEndpointsMatched = LoggerMessage.Define<PathString>(
            LogLevel.Debug,
            new EventId(2, "NoEndpointsMatched"),
            "No endpoints matched the current request path: {PathString}");

        public static void AmbiguousEndpoints(this ILogger logger, string ambiguousEndpoints)
        {
            _ambiguousEndpoints(logger, ambiguousEndpoints, null);
        }

        public static void EndpointMatched(this ILogger logger, string endpointName)
        {
            _endpointMatched(logger, endpointName ?? "Unnamed endpoint", null);
        }

        public static void NoEndpointsMatched(this ILogger logger, PathString pathString)
        {
            _noEndpointsMatched(logger, pathString, null);
        }
    }
}
