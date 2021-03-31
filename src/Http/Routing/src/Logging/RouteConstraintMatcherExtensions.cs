// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class RouteConstraintMatcherExtensions
    {
        private static readonly Action<ILogger, object, string, IRouteConstraint, Exception?> _constraintNotMatched;

        static RouteConstraintMatcherExtensions()
        {
            _constraintNotMatched = LoggerMessage.Define<object, string, IRouteConstraint>(
                LogLevel.Debug,
                new EventId(1, "ConstraintNotMatched"),
                "Route value '{RouteValue}' with key '{RouteKey}' did not match " +
                            "the constraint '{RouteConstraint}'");
        }

        public static void ConstraintNotMatched(
            this ILogger logger,
            object routeValue,
            string routeKey,
            IRouteConstraint routeConstraint)
        {
            _constraintNotMatched(logger, routeValue, routeKey, routeConstraint, null);
        }
    }
}
