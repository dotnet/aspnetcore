// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Cors.Internal
{
    internal static class CORSLoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _isPreflightRequest;
        private static readonly Action<ILogger, Exception> _requestHasOriginHeader;
        private static readonly Action<ILogger, Exception> _requestDoesNotHaveOriginHeader;
        private static readonly Action<ILogger, Exception> _policySuccess;
        private static readonly Action<ILogger, string, Exception> _policyFailure;

        static CORSLoggerExtensions()
        {
            _isPreflightRequest = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "This is a preflight request.");

            _requestHasOriginHeader = LoggerMessage.Define(
                LogLevel.Debug,
                2,
                "The request has an origin header.");

            _requestDoesNotHaveOriginHeader = LoggerMessage.Define(
                LogLevel.Debug,
                3,
                "The request does not have an origin header.");

            _policySuccess = LoggerMessage.Define(
                LogLevel.Information,
                4,
                "Policy execution successful.");

            _policyFailure = LoggerMessage.Define<string>(
                LogLevel.Information,
                5,
                "Policy execution failed. {FailureReason}");
        }

        public static void IsPreflightRequest(this ILogger logger)
        {
            _isPreflightRequest(logger, null);
        }

        public static void RequestHasOriginHeader(this ILogger logger)
        {
            _requestHasOriginHeader(logger, null);
        }

        public static void RequestDoesNotHaveOriginHeader(this ILogger logger)
        {
            _requestDoesNotHaveOriginHeader(logger, null);
        }

        public static void PolicySuccess(this ILogger logger)
        {
            _policySuccess(logger, null);
        }

        public static void PolicyFailure(this ILogger logger, string failureReason)
        {
            _policyFailure(logger, failureReason, null);
        }
    }
}
