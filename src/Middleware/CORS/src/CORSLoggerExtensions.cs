// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Cors
{
    internal static class CORSLoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _isPreflightRequest;
        private static readonly Action<ILogger, string, Exception> _requestHasOriginHeader;
        private static readonly Action<ILogger, Exception> _requestDoesNotHaveOriginHeader;
        private static readonly Action<ILogger, Exception> _policySuccess;
        private static readonly Action<ILogger, Exception> _policyFailure;
        private static readonly Action<ILogger, string, Exception> _originNotAllowed;
        private static readonly Action<ILogger, string, Exception> _accessControlMethodNotAllowed;
        private static readonly Action<ILogger, string, Exception> _requestHeaderNotAllowed;
        private static readonly Action<ILogger, Exception> _failedToSetCorsHeaders;
        private static readonly Action<ILogger, Exception> _noCorsPolicyFound;
        private static readonly Action<ILogger, Exception> _isNotPreflightRequest;

        static CORSLoggerExtensions()
        {
            _isPreflightRequest = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "IsPreflightRequest"),
                "The request is a preflight request.");

            _requestHasOriginHeader = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(2, "RequestHasOriginHeader"),
                "The request has an origin header: '{origin}'.");

            _requestDoesNotHaveOriginHeader = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(3, "RequestDoesNotHaveOriginHeader"),
                "The request does not have an origin header.");

            _policySuccess = LoggerMessage.Define(
                LogLevel.Information,
                new EventId(4, "PolicySuccess"),
                "CORS policy execution successful.");

            _policyFailure = LoggerMessage.Define(
                LogLevel.Information,
                new EventId(5, "PolicyFailure"),
                "CORS policy execution failed.");

            _originNotAllowed = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(6, "OriginNotAllowed"),
                "Request origin {origin} does not have permission to access the resource.");

            _accessControlMethodNotAllowed = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(7, "AccessControlMethodNotAllowed"),
                "Request method {accessControlRequestMethod} not allowed in CORS policy.");

            _requestHeaderNotAllowed = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(8, "RequestHeaderNotAllowed"),
                "Request header '{requestHeader}' not allowed in CORS policy.");

            _failedToSetCorsHeaders = LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(9, "FailedToSetCorsHeaders"),
                "Failed to apply CORS Response headers.");

            _noCorsPolicyFound = LoggerMessage.Define(
                LogLevel.Information,
                new EventId(10, "NoCorsPolicyFound"),
                "No CORS policy found for the specified request.");

            _isNotPreflightRequest = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(12, "IsNotPreflightRequest"),
                "This request uses the HTTP OPTIONS method but does not have an Access-Control-Request-Method header. This request will not be treated as a CORS preflight request.");
        }

        public static void IsPreflightRequest(this ILogger logger)
        {
            _isPreflightRequest(logger, null);
        }

        public static void RequestHasOriginHeader(this ILogger logger, string origin)
        {
            _requestHasOriginHeader(logger, origin, null);
        }

        public static void RequestDoesNotHaveOriginHeader(this ILogger logger)
        {
            _requestDoesNotHaveOriginHeader(logger, null);
        }

        public static void PolicySuccess(this ILogger logger)
        {
            _policySuccess(logger, null);
        }

        public static void PolicyFailure(this ILogger logger)
        {
            _policyFailure(logger, null);
        }

        public static void OriginNotAllowed(this ILogger logger, string origin)
        {
            _originNotAllowed(logger, origin, null);
        }

        public static void AccessControlMethodNotAllowed(this ILogger logger, string accessControlMethod)
        {
            _accessControlMethodNotAllowed(logger, accessControlMethod, null);
        }

        public static void RequestHeaderNotAllowed(this ILogger logger, string requestHeader)
        {
            _requestHeaderNotAllowed(logger, requestHeader, null);
        }

        public static void FailedToSetCorsHeaders(this ILogger logger, Exception exception)
        {
            _failedToSetCorsHeaders(logger, exception);
        }

        public static void NoCorsPolicyFound(this ILogger logger)
        {
            _noCorsPolicyFound(logger, null);
        }

        public static void IsNotPreflightRequest(this ILogger logger)
        {
            _isNotPreflightRequest(logger, null);
        }
    }
}
