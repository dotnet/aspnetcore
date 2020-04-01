// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal static class PageLoggerExtensions
    {
        public const string PageFilter = "Page Filter";

        private static readonly Action<ILogger, string, string, Exception> _pageModelFactoryExecuting;
        private static readonly Action<ILogger, string, string, Exception> _pageModelFactoryExecuted;
        private static readonly Action<ILogger, string, string, Exception> _pageFactoryExecuting;
        private static readonly Action<ILogger, string, string, Exception> _pageFactoryExecuted;
        private static readonly Action<ILogger, string, ModelValidationState, Exception> _handlerMethodExecuting;
        private static readonly Action<ILogger, ModelValidationState, Exception> _implicitHandlerMethodExecuting;
        private static readonly Action<ILogger, string, string[], Exception> _handlerMethodExecutingWithArguments;
        private static readonly Action<ILogger, string, string, Exception> _handlerMethodExecuted;
        private static readonly Action<ILogger, string, Exception> _implicitHandlerMethodExecuted;
        private static readonly Action<ILogger, object, Exception> _pageFilterShortCircuit;
        private static readonly Action<ILogger, Type, Exception> _notMostEffectiveFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _beforeExecutingMethodOnFilter;
        private static readonly Action<ILogger, string, string, string, Exception> _afterExecutingMethodOnFilter;

        static PageLoggerExtensions()
        {
            // These numbers start at 101 intentionally to avoid conflict with the IDs used by ResourceInvoker.

            _pageModelFactoryExecuting = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(101, "ExecutingModelFactory"),
               "Executing page model factory for page {Page} ({AssemblyName})");

            _pageModelFactoryExecuted = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(102, "ExecutedModelFactory"),
                "Executed page model factory for page {Page} ({AssemblyName})");

            _pageFactoryExecuting = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(101, "ExecutingPageFactory"),
               "Executing page factory for page {Page} ({AssemblyName})");

            _pageFactoryExecuted = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(102, "ExecutedPageFactory"),
                "Executed page factory for page {Page} ({AssemblyName})");

            _handlerMethodExecuting = LoggerMessage.Define<string, ModelValidationState>(
                LogLevel.Information,
                new EventId(101, "ExecutingHandlerMethod"),
                "Executing handler method {HandlerName} - ModelState is {ValidationState}");

            _handlerMethodExecutingWithArguments = LoggerMessage.Define<string, string[]>(
                LogLevel.Trace,
                new EventId(103, "HandlerMethodExecutingWithArguments"),
                "Executing handler method {HandlerName} with arguments ({Arguments})");

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
        }

        public static void ExecutingPageModelFactory(this ILogger logger, PageContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
            var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
            _pageModelFactoryExecuting(logger, pageName, pageType.Assembly.GetName().Name, null);
        }

        public static void ExecutedPageModelFactory(this ILogger logger, PageContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
            var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
            _pageModelFactoryExecuted(logger, pageName, pageType.Assembly.GetName().Name, null);
        }

        public static void ExecutingPageFactory(this ILogger logger, PageContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
            var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
            _pageFactoryExecuting(logger, pageName, pageType.Assembly.GetName().Name, null);
        }

        public static void ExecutedPageFactory(this ILogger logger, PageContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
            var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
            _pageFactoryExecuted(logger, pageName, pageType.Assembly.GetName().Name, null);
        }

        public static void ExecutingHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var declaringTypeName = TypeNameHelper.GetTypeDisplayName(handler.MethodInfo.DeclaringType);
                var handlerName = declaringTypeName + "." + handler.MethodInfo.Name;

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
    }
}
