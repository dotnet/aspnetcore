// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ControllerActionInvokerLoggerExtensions
    {
        private static readonly Action<ILogger, string, string[], ModelValidationState, Exception> _actionMethodExecuting;
        private static readonly Action<ILogger, string, string, Exception> _actionMethodExecuted;

        static ControllerActionInvokerLoggerExtensions()
        {
            _actionMethodExecuting = LoggerMessage.Define<string, string[], ModelValidationState>(
                LogLevel.Information,
                1,
                "Executing action method {ActionName} with arguments ({Arguments}) - ModelState is {ValidationState}'");

            _actionMethodExecuted = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "Executed action method {ActionName}, returned result {ActionResult}.'");
        }

        public static void ActionMethodExecuting(this ILogger logger, ActionExecutingContext context, object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var actionName = context.ActionDescriptor.DisplayName;

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

                _actionMethodExecuting(logger, actionName, convertedArguments, validationState, null);
            }
        }

        public static void ActionMethodExecuted(this ILogger logger, ActionExecutingContext context, IActionResult result)
        {
            var actionName = context.ActionDescriptor.DisplayName;
            _actionMethodExecuted(logger, actionName, Convert.ToString(result), null);
        }
    }
}
