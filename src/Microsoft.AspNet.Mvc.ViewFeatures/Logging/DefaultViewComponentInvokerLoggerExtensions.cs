// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Logging
{
    public static class DefaultViewComponentInvokerLoggerExtensions
    {
        private static readonly double TimestampToTicks = Stopwatch.Frequency / 10000000.0;
        private static readonly string[] EmptyArguments =
#if NET451
            new string[0];
#else
            Array.Empty<string>();
#endif
        private static readonly Action<ILogger, string, string[], Exception> _viewComponentExecuting;
        private static readonly Action<ILogger, string, double, string, Exception> _viewComponentExecuted;

        static DefaultViewComponentInvokerLoggerExtensions()
        {
            _viewComponentExecuting = LoggerMessage.Define<string, string[]>(
                LogLevel.Debug,
                1,
                "Executing view component {ViewComponentName} with arguments ({Arguments}).");

            _viewComponentExecuted = LoggerMessage.Define<string, double, string>(
                LogLevel.Debug,
                2,
                "Executed view component {ViewComponentName} in {ElapsedMilliseconds}ms and returned " +
                "{ViewComponentResult}");
        }

        public static IDisposable ViewComponentScope(this ILogger logger, ViewComponentContext context)
        {
            return logger.BeginScopeImpl(new ViewComponentLogScope(context.ViewComponentDescriptor));
        }

        public static void ViewComponentExecuting(
            this ILogger logger,
            ViewComponentContext context,
            object[] arguments)
        {
            var formattedArguments = GetFormattedArguments(arguments);
            _viewComponentExecuting(logger, context.ViewComponentDescriptor.DisplayName, formattedArguments, null);
        }

        private static string[] GetFormattedArguments(object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return EmptyArguments;
            }

            var formattedArguments = new string[arguments.Length];
            for (var i = 0; i < formattedArguments.Length; i++)
            {
                formattedArguments[i] = Convert.ToString(arguments[i]);
            }

            return formattedArguments;
        }

        public static void ViewComponentExecuted(
            this ILogger logger,
            ViewComponentContext context,
            long startTimestamp,
            object result)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));

                _viewComponentExecuted(
                    logger,
                    context.ViewComponentDescriptor.DisplayName,
                    elapsed.TotalMilliseconds,
                    Convert.ToString(result),
                    null);
            }
        }

        private class ViewComponentLogScope : ILogValues
        {
            private readonly ViewComponentDescriptor _descriptor;

            public ViewComponentLogScope(ViewComponentDescriptor descriptor)
            {
                _descriptor = descriptor;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                return new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("ViewComponentName", _descriptor.DisplayName),
                    new KeyValuePair<string, object>("ViewComponentId", _descriptor.Id),
                };
            }

            public override string ToString()
            {
                return _descriptor.DisplayName;
            }
        }
    }
}
