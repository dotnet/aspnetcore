// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    internal static class PageLoggerExtensions
    {
        public const string PageFilter = "Page Filter";

        private static readonly Action<ILogger, string, ModelValidationState, Exception> _handlerMethodExecuting;
        private static readonly Action<ILogger, string, string[], Exception> _handlerMethodExecutingWithArguments;
        private static readonly Action<ILogger, string, string, Exception> _handlerMethodExecuted;
        private static readonly Action<ILogger, object, Exception> _pageFilterShortCircuit;
        private static readonly Action<ILogger, string, string[], Exception> _malformedPageDirective;
        private static readonly Action<ILogger, string, Exception> _unsupportedAreaPath;
        private static readonly Action<ILogger, Type, Exception> _notMostEffectiveFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _beforeExecutingMethodOnFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _afterExecutingMethodOnFilter;

        static PageLoggerExtensions()
        {
            // These numbers start at 101 intentionally to avoid conflict with the IDs used by ResourceInvoker.

            _handlerMethodExecuting = LoggerMessage.Define<string, ModelValidationState>(
                LogLevel.Information,
                101,
                "Executing handler method {HandlerName} - ModelState is {ValidationState}");

            _handlerMethodExecutingWithArguments = LoggerMessage.Define<string, string[]>(
                LogLevel.Trace,
                103,
                "Executing handler method {HandlerName} with arguments ({Arguments})");

            _handlerMethodExecuted = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                102,
                "Executed handler method {HandlerName}, returned result {ActionResult}.");

            _pageFilterShortCircuit = LoggerMessage.Define<object>(
               LogLevel.Debug,
               3,
               "Request was short circuited at page filter '{PageFilter}'.");

            _malformedPageDirective = LoggerMessage.Define<string, string[]>(
                LogLevel.Warning,
                104,
                "The page directive at '{FilePath}' is malformed. Please fix the following issues: {Diagnostics}");

            _notMostEffectiveFilter = LoggerMessage.Define<Type>(
               LogLevel.Debug,
               1,
               "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.");

            _beforeExecutingMethodOnFilter = LoggerMessage.Define<string, string, string>(
                LogLevel.Trace,
                1,
                "{FilterType}: Before executing {Method} on filter {Filter}.");

            _afterExecutingMethodOnFilter = LoggerMessage.Define<string, string, string>(
                LogLevel.Trace,
                2,
                "{FilterType}: After executing {Method} on filter {Filter}.");

            _unsupportedAreaPath = LoggerMessage.Define<string>(
                LogLevel.Warning,
                1,
                "The page at '{FilePath}' is located under the area root directory '/Areas/' but does not follow the path format '/Areas/AreaName/Pages/Directory/FileName.cshtml");
        }

        public static void ExecutingHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var handlerName = handler.MethodInfo.Name;

                var validationState = context.ModelState.ValidationState;
                _handlerMethodExecuting(logger, handlerName, validationState, null);

                if (arguments != null && logger.IsEnabled(LogLevel.Trace))
                {
                    var convertedArguments = new string[arguments.Length];
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        convertedArguments[i] = Convert.ToString(arguments[i]);
                    }

                    _handlerMethodExecutingWithArguments(logger, handlerName, convertedArguments, null);
                }
            }
        }

        public static void ExecutedHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, IActionResult result)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var handlerName = handler.MethodInfo.Name;
                _handlerMethodExecuted(logger, handlerName, Convert.ToString(result), null);
            }
        }

        public static void BeforeExecutingMethodOnFilter(this ILogger logger, string filterType, string methodName, IFilterMetadata filter)
        {
            _beforeExecutingMethodOnFilter(logger, filterType, methodName, filter.GetType().ToString(), null);
        }

        public static void AfterExecutingMethodOnFilter(this ILogger logger, string filterType, string methodName, IFilterMetadata filter)
        {
            _afterExecutingMethodOnFilter(logger, filterType, methodName, filter.GetType().ToString(), null);
        }

        public static void PageFilterShortCircuited(
            this ILogger logger,
            IFilterMetadata filter)
        {
            _pageFilterShortCircuit(logger, filter, null);
        }

        public static void MalformedPageDirective(this ILogger logger, string filePath, IList<RazorDiagnostic> diagnostics)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                var messages = new string[diagnostics.Count];
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    messages[i] = diagnostics[i].GetMessage();
                }

                _malformedPageDirective(logger, filePath, messages, null);
            }
        }

        public static void NotMostEffectiveFilter(this ILogger logger, Type policyType)
        {
            _notMostEffectiveFilter(logger, policyType, null);
        }

        public static void UnsupportedAreaPath(this ILogger logger, string filePath)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                _unsupportedAreaPath(logger, filePath, null);
            }
        }
    }
}
