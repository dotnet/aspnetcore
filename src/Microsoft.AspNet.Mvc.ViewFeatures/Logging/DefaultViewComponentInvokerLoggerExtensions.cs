// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Logging
{
    public static class DefaultViewComponentInvokerLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _viewComponentExecuting;
        private static readonly Action<ILogger, string, double, string, Exception> _viewComponentExecuted;

        static DefaultViewComponentInvokerLoggerExtensions()
        {
            _viewComponentExecuting = LoggerMessage.Define<string>(
                LogLevel.Verbose,
                1,
                "Executing view component {ViewComponentName}");

            _viewComponentExecuted = LoggerMessage.Define<string, double, string>(
                LogLevel.Verbose,
                2,
                "Executed view component {ViewComponentName} in {ElapsedMilliseconds}ms and returned " +
                "{ViewComponentResult}");
        }

        public static IDisposable ViewComponentScope(this ILogger logger, ViewComponentContext context)
        {
            return logger.BeginScopeImpl(new ViewComponentLogScope(context.ViewComponentDescriptor));
        }

        public static void ViewComponentExecuting(this ILogger logger, ViewComponentContext context)
        {
            _viewComponentExecuting(logger, context.ViewComponentDescriptor.DisplayName, null);
        }

        public static void ViewComponentExecuted(
            this ILogger logger,
            ViewComponentContext context,
            int startTime,
            object result)
        {
            var elapsed = new TimeSpan(Environment.TickCount - startTime);
            _viewComponentExecuted(
                logger,
                context.ViewComponentDescriptor.DisplayName,
                elapsed.TotalMilliseconds,
                Convert.ToString(result),
                null);
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
