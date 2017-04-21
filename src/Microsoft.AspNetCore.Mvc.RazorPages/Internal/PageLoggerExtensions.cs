// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    internal static class PageLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        private static readonly Action<ILogger, string, Exception> _pageExecuting;
        private static readonly Action<ILogger, string, double, Exception> _pageExecuted;
        private static readonly Action<ILogger, object, Exception> _exceptionFilterShortCircuit;
        private static readonly Action<ILogger, object, Exception> _pageFilterShortCircuit;

        static PageLoggerExtensions()
        {
            _pageExecuting = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Executing page {ActionName}");

            _pageExecuted = LoggerMessage.Define<string, double>(
                LogLevel.Information,
                2,
                "Executed page {ActionName} in {ElapsedMilliseconds}ms");

            _exceptionFilterShortCircuit = LoggerMessage.Define<object>(
                LogLevel.Debug,
                4,
                "Request was short circuited at exception filter '{ExceptionFilter}'.");

            _pageFilterShortCircuit = LoggerMessage.Define<object>(
               LogLevel.Debug,
               3,
               "Request was short circuited at page filter '{PageFilter}'.");
        }

        public static IDisposable PageScope(this ILogger logger, ActionDescriptor actionDescriptor)
        {
            Debug.Assert(logger != null);
            Debug.Assert(actionDescriptor != null);

            return logger.BeginScope(new PageLogScope(actionDescriptor));
        }

        public static void ExecutingPage(this ILogger logger, ActionDescriptor action)
        {
            _pageExecuting(logger, action.DisplayName, null);
        }

        public static void ExecutedAction(this ILogger logger, ActionDescriptor action, long startTimestamp)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (logger.IsEnabled(LogLevel.Information) && startTimestamp != 0)
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));

                _pageExecuted(logger, action.DisplayName, elapsed.TotalMilliseconds, null);
            }
        }

        public static void ExceptionFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _exceptionFilterShortCircuit(logger, filter, null);
        }

        public static void PageFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _pageFilterShortCircuit(logger, filter, null);
        }

        private class PageLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly ActionDescriptor _action;

            public PageLogScope(ActionDescriptor action)
            {
                _action = action;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("ActionId", _action.Id);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("PageName", _action.DisplayName);
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public int Count => 2;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            public override string ToString()
            {
                // We don't include the _action.Id here because it's just an opaque guid, and if
                // you have text logging, you can already use the requestId for correlation.
                return _action.DisplayName;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
