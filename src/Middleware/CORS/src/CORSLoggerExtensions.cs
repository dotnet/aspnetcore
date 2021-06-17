// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Cors
{
    internal static partial class CORSLoggerExtensions
    {
        [LoggerMessage(EventId = 1, EventName = "IsPreflightRequest", Level = LogLevel.Debug, Message = "The request is a preflight request.")]
        public static partial void IsPreflightRequest(this ILogger logger);

        [LoggerMessage(EventId = 2, EventName = "RequestHasOriginHeader", Level = LogLevel.Debug, Message = "The request has an origin header: '{origin}'.")]
        public static partial void RequestHasOriginHeader(this ILogger logger, string origin);

        [LoggerMessage(EventId = 3, EventName = "RequestDoesNotHaveOriginHeader", Level = LogLevel.Debug, Message = "The request does not have an origin header.")]
        public static partial void RequestDoesNotHaveOriginHeader(this ILogger logger);

        [LoggerMessage(EventId = 4, EventName = "PolicySuccess", Level = LogLevel.Information, Message = "CORS policy execution successful.")]
        public static partial void PolicySuccess(this ILogger logger);

        [LoggerMessage(EventId = 5, EventName = "PolicyFailure", Level = LogLevel.Information, Message = "CORS policy execution failed.")]
        public static partial void PolicyFailure(this ILogger logger);

        [LoggerMessage(EventId = 6, EventName = "OriginNotAllowed", Level = LogLevel.Information, Message = "Request origin {origin} does not have permission to access the resource.")]
        public static partial void OriginNotAllowed(this ILogger logger, string origin);

        [LoggerMessage(EventId = 7, EventName = "AccessControlMethodNotAllowed", Level = LogLevel.Information, Message = "Request method {accessControlRequestMethod} not allowed in CORS policy.")]
        public static partial void AccessControlMethodNotAllowed(this ILogger logger, string accessControlMethod);

        [LoggerMessage(EventId = 8, EventName = "RequestHeaderNotAllowed", Level = LogLevel.Information, Message = "Request header '{requestHeader}' not allowed in CORS policy.")]
        public static partial void RequestHeaderNotAllowed(this ILogger logger, string requestHeader);

        [LoggerMessage(EventId = 9, EventName = "FailedToSetCorsHeaders", Level = LogLevel.Warning, Message = "Failed to apply CORS Response headers.")]
        public static partial void FailedToSetCorsHeaders(this ILogger logger, Exception? exception);

        [LoggerMessage(EventId = 10, EventName = "NoCorsPolicyFound", Level = LogLevel.Information, Message = "No CORS policy found for the specified request.")]
        public static partial void NoCorsPolicyFound(this ILogger logger);

        [LoggerMessage(EventId = 12, EventName = "IsNotPreflightRequest", Level = LogLevel.Debug,
            Message = "This request uses the HTTP OPTIONS method but does not have an Access-Control-Request-Method header. This request will not be treated as a CORS preflight request.")]
        public static partial void IsNotPreflightRequest(this ILogger logger);
    }
}
