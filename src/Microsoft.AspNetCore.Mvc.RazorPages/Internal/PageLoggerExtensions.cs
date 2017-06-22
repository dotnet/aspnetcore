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
        private static readonly Action<ILogger, string, string[], ModelValidationState, Exception> _handlerMethodExecuting;
        private static readonly Action<ILogger, string, string, Exception> _handlerMethodExecuted;
        private static readonly Action<ILogger, object, Exception> _pageFilterShortCircuit;
        private static readonly Action<ILogger, string, string[], Exception> _malformedPageDirective;

        static PageLoggerExtensions()
        {
            // These numbers start at 101 intentionally to avoid conflict with the IDs used by ResourceInvoker.

            _handlerMethodExecuting = LoggerMessage.Define<string, string[], ModelValidationState>(
                LogLevel.Information,
                101,
                "Executing handler method {HandlerName} with arguments ({Arguments}) - ModelState is {ValidationState}");

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
        }

        public static void ExecutingHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var handlerName = handler.MethodInfo.Name;

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

        public static void ExecutedHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, IActionResult result)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var handlerName = handler.MethodInfo.Name;
                _handlerMethodExecuted(logger, handlerName, Convert.ToString(result), null);
            }
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
    }
}
