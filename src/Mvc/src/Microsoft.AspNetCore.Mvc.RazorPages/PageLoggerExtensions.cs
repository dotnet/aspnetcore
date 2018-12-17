// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal static class PageLoggerExtensions
    {
        public const string PageFilter = "Page Filter";

        private static readonly Action<ILogger, string, string[], ModelValidationState, Exception> _handlerMethodExecuting;
        private static readonly Action<ILogger, ModelValidationState, Exception> _implicitHandlerMethodExecuting;
        private static readonly Action<ILogger, string, string, Exception> _handlerMethodExecuted;
        private static readonly Action<ILogger, string, Exception> _implicitHandlerMethodExecuted;
        private static readonly Action<ILogger, object, Exception> _pageFilterShortCircuit;
        private static readonly Action<ILogger, string, string[], Exception> _malformedPageDirective;
        private static readonly Action<ILogger, string, Exception> _unsupportedAreaPath;
        private static readonly Action<ILogger, Type, Exception> _notMostEffectiveFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _beforeExecutingMethodOnFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _afterExecutingMethodOnFilter;

        static PageLoggerExtensions()
        {
            // These numbers start at 101 intentionally to avoid conflict with the IDs used by ResourceInvoker.

            _handlerMethodExecuting = LoggerMessage.Define<string, string[], ModelValidationState>(
                LogLevel.Information,
                new EventId(101, "ExecutingHandlerMethod"),
                "Executing handler method {HandlerName} with arguments ({Arguments}) - ModelState is {ValidationState}");

            _handlerMethodExecuted = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(102, "ExecutedHandlerMethod"),
                "Executed handler method {HandlerName}, returned result {ActionResult}.");

            _implicitHandlerMethodExecuting = LoggerMessage.Define<ModelValidationState>(
                LogLevel.Information,
                new EventId(103, "ExecutingImplicitHandlerMethod"),
                "Executing an implicit handler method - ModelState is {ValidationState}");

            _implicitHandlerMethodExecuted = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(104, "ExecutedImplicitHandlerMethod"),
                "Executed an implicit handler method, returned result {ActionResult}.");

            _pageFilterShortCircuit = LoggerMessage.Define<object>(
               LogLevel.Debug,
                new EventId(3, "PageFilterShortCircuited"),
               "Request was short circuited at page filter '{PageFilter}'.");

            _malformedPageDirective = LoggerMessage.Define<string, string[]>(
                LogLevel.Warning,
                new EventId(104, "MalformedPageDirective"),
                "The page directive at '{FilePath}' is malformed. Please fix the following issues: {Diagnostics}");

            _notMostEffectiveFilter = LoggerMessage.Define<Type>(
               LogLevel.Debug,
                new EventId(1, "NotMostEffectiveFilter"),
               "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.");

            _beforeExecutingMethodOnFilter = LoggerMessage.Define<string, string, string>(
                LogLevel.Trace,
                new EventId(1, "BeforeExecutingMethodOnFilter"),
                "{FilterType}: Before executing {Method} on filter {Filter}.");

            _afterExecutingMethodOnFilter = LoggerMessage.Define<string, string, string>(
                LogLevel.Trace,
                new EventId(2, "AfterExecutingMethodOnFilter"),
                "{FilterType}: After executing {Method} on filter {Filter}.");

            _unsupportedAreaPath = LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, "UnsupportedAreaPath"),
                "The page at '{FilePath}' is located under the area root directory '/Areas/' but does not follow the path format '/Areas/AreaName/Pages/Directory/FileName.cshtml");
        }

        public static void ExecutingHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var handlerName = handler.MethodInfo.DeclaringType.FullName + "." + handler.MethodInfo.Name;

                string[] convertedArguments;
                if (arguments == null)
                {
                    convertedArguments = null;
                }
                else
                {
                    convertedArguments = new string[arguments.Length];
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        convertedArguments[i] = Convert.ToString(arguments[i]);
                    }
                }

                var validationState = context.ModelState.ValidationState;

                _handlerMethodExecuting(logger, handlerName, convertedArguments, validationState, null);
            }
        }

        public static void ExecutingImplicitHandlerMethod(this ILogger logger, PageContext context)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var validationState = context.ModelState.ValidationState;

                _implicitHandlerMethodExecuting(logger, validationState, null);
            }
        }

        public static void ExecutedHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, IActionResult result)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var handlerName = handler.MethodInfo.Name;
                _handlerMethodExecuted(logger, handlerName, Convert.ToString(result), null);
            }
        }

        public static void ExecutedImplicitHandlerMethod(this ILogger logger, IActionResult result)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _implicitHandlerMethodExecuted(logger, Convert.ToString(result), null);
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
