// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    internal static class MvcViewFeaturesLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private static readonly Action<ILogger, string, string[], Exception> _viewComponentExecuting;
        private static readonly Action<ILogger, string, double, string, Exception> _viewComponentExecuted;

        private static readonly Action<ILogger, string, Exception> _partialViewFound;
        private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _partialViewNotFound;
        private static readonly Action<ILogger, string, Exception> _partialViewResultExecuting;

        private static readonly Action<ILogger, string, Exception> _antiforgeryTokenInvalid;

        private static readonly Action<ILogger, string, Exception> _viewComponentResultExecuting;

        private static readonly Action<ILogger, string, Exception> _viewResultExecuting;
        private static readonly Action<ILogger, string, Exception> _viewFound;
        private static readonly Action<ILogger, string, IEnumerable<string>, Exception> _viewNotFound;

        private static readonly Action<ILogger, string, Exception> _tempDataCookieNotFound;
        private static readonly Action<ILogger, string, Exception> _tempDataCookieLoadSuccess;
        private static readonly Action<ILogger, string, Exception> _tempDataCookieLoadFailure;

        static MvcViewFeaturesLoggerExtensions()
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

            _partialViewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing PartialViewResult, running view at path {Path}.");

            _partialViewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                2,
                "The partial view '{PartialViewName}' was found.");

            _partialViewNotFound = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Error,
                3,
                "The partial view '{PartialViewName}' was not found. Searched locations: {SearchedViewLocations}");

            _antiforgeryTokenInvalid = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Antiforgery token validation failed. {Message}");

            _viewComponentResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ViewComponentResult, running {ViewComponentName}.");

            _viewResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ViewResult, running view at path {Path}.");

            _viewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                2,
                "The view '{ViewName}' was found.");

            _viewNotFound = LoggerMessage.Define<string, IEnumerable<string>>(
                LogLevel.Error,
                3,
                "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}");

            _tempDataCookieNotFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "The temp data cookie {CookieName} was not found.");

            _tempDataCookieLoadSuccess = LoggerMessage.Define<string>(
                LogLevel.Debug,
                2,
                "The temp data cookie {CookieName} was used to successfully load temp data.");

            _tempDataCookieLoadFailure = LoggerMessage.Define<string>(
                LogLevel.Warning,
                3,
                "The temp data cookie {CookieName} could not be loaded.");
        }

        public static IDisposable ViewComponentScope(this ILogger logger, ViewComponentContext context)
        {
            return logger.BeginScope(new ViewComponentLogScope(context.ViewComponentDescriptor));
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
                return Array.Empty<string>();
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

        public static void PartialViewFound(
            this ILogger logger,
            string partialViewName)
        {
            _partialViewFound(logger, partialViewName, null);
        }

        public static void PartialViewNotFound(
            this ILogger logger,
            string partialViewName,
            IEnumerable<string> searchedLocations)
        {
            _partialViewNotFound(logger, partialViewName, searchedLocations, null);
        }

        public static void PartialViewResultExecuting(this ILogger logger, IView view)
        {
            _partialViewResultExecuting(logger, view.Path, null);
        }

        public static void AntiforgeryTokenInvalid(this ILogger logger, string message, Exception exception)
        {
            _antiforgeryTokenInvalid(logger, message, exception);
        }

        public static void ViewComponentResultExecuting(this ILogger logger, string viewComponentName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _viewComponentResultExecuting(logger, viewComponentName, null);
            }
        }

        public static void ViewComponentResultExecuting(this ILogger logger, Type viewComponentType)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _viewComponentResultExecuting(logger, viewComponentType.Name, null);
            }
        }

        public static void ViewResultExecuting(this ILogger logger, IView view)
        {
            _viewResultExecuting(logger, view.Path, null);
        }

        public static void ViewFound(this ILogger logger, string viewName)
        {
            _viewFound(logger, viewName, null);
        }

        public static void ViewNotFound(this ILogger logger, string viewName,
            IEnumerable<string> searchedLocations)
        {
            _viewNotFound(logger, viewName, searchedLocations, null);
        }

        public static void TempDataCookieNotFound(this ILogger logger, string cookieName)
        {
            _tempDataCookieNotFound(logger, cookieName, null);
        }

        public static void TempDataCookieLoadSuccess(this ILogger logger, string cookieName)
        {
            _tempDataCookieLoadSuccess(logger, cookieName, null);
        }

        public static void TempDataCookieLoadFailure(this ILogger logger, string cookieName, Exception exception)
        {
            _tempDataCookieLoadFailure(logger, cookieName, exception);
        }

        private class ViewComponentLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly ViewComponentDescriptor _descriptor;

            public ViewComponentLogScope(ViewComponentDescriptor descriptor)
            {
                _descriptor = descriptor;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("ViewComponentName", _descriptor.DisplayName);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("ViewComponentId", _descriptor.Id);
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
                return _descriptor.DisplayName;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
