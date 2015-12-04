// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class FilterActionInvokerLoggerExtensions
    {
        private static readonly Action<ILogger, object, Exception> _authorizationFailure;
        private static readonly Action<ILogger, object, Exception> _resourceFilterShortCircuit;
        private static readonly Action<ILogger, object, Exception> _actionFilterShortCircuit;
        private static readonly Action<ILogger, object, Exception> _exceptionFilterShortCircuit;

        static FilterActionInvokerLoggerExtensions()
        {
            _authorizationFailure = LoggerMessage.Define<object>(
                LogLevel.Warning,
                1,
                "Authorization failed for the request at filter '{AuthorizationFilter}'.");
            _resourceFilterShortCircuit = LoggerMessage.Define<object>(
                LogLevel.Debug,
                2,
                "Request was short circuited at resource filter '{ResourceFilter}'.");
            _actionFilterShortCircuit = LoggerMessage.Define<object>(
                LogLevel.Debug,
                3,
                "Request was short circuited at action filter '{ActionFilter}'.");
            _exceptionFilterShortCircuit = LoggerMessage.Define<object>(
                LogLevel.Debug,
                4,
                "Request was short circuited at exception filter '{ExceptionFilter}'.");
        }

        public static void AuthorizationFailure(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _authorizationFailure(logger, filter, null);
        }

        public static void ResourceFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _resourceFilterShortCircuit(logger, filter, null);
        }

        public static void ExceptionFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _exceptionFilterShortCircuit(logger, filter, null);
        }

        public static void ActionFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _actionFilterShortCircuit(logger, filter, null);
        }
    }
}
