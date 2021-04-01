// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal static class CorsLoggerExtensions
    {
        private static readonly Action<ILogger, Type, Exception> _notMostEffectiveFilter;

        static CorsLoggerExtensions()
        {
            _notMostEffectiveFilter = LoggerMessage.Define<Type>(
               LogLevel.Debug,
               new EventId(1, "NotMostEffectiveFilter"),
               "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.");
        }

        public static void NotMostEffectiveFilter(this ILogger logger, Type policyType)
        {
            _notMostEffectiveFilter(logger, policyType, null);
        }
    }
}
