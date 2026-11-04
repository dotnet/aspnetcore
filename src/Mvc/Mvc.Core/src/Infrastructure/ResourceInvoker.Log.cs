// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal partial class ResourceInvoker
{
    // Internal for unit testing
    internal static partial class Log
    {
        public static void ExecutingAction(ILogger logger, ActionDescriptor action)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append('{');
                var index = 0;
                var count = action.RouteValues.Count;
                foreach (var (key, value) in action.RouteValues)
                {
                    if (index == count - 1)
                    {
                        stringBuilder.Append(CultureInfo.InvariantCulture, $"{key} = \"{value}\"");
                    }
                    else
                    {
                        stringBuilder.Append(CultureInfo.InvariantCulture, $"{key} = \"{value}\", ");
                    }
                    index++;
                }
                stringBuilder.Append('}');

                if (action.RouteValues.TryGetValue("page", out var page) && page != null)
                {
                    PageExecuting(logger, stringBuilder.ToString(), action.DisplayName);
                }
                else
                {
                    if (action is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        var controllerType = controllerActionDescriptor.ControllerTypeInfo.AsType();
                        var controllerName = TypeNameHelper.GetTypeDisplayName(controllerType);
                        ControllerActionExecuting(
                            logger,
                            stringBuilder.ToString(),
                            controllerActionDescriptor.MethodInfo,
                            controllerName,
                            controllerType.Assembly.GetName().Name);
                    }
                    else
                    {
                        ActionExecuting(logger, stringBuilder.ToString(), action.DisplayName);
                    }
                }
            }
        }

        [LoggerMessage(101, LogLevel.Information, "Route matched with {RouteData}. Executing action {ActionName}", EventName = "ActionExecuting", SkipEnabledCheck = true)]
        private static partial void ActionExecuting(ILogger logger, string routeData, string? actionName);

        [LoggerMessage(102, LogLevel.Information, "Route matched with {RouteData}. Executing controller action with signature {MethodInfo} on controller {Controller} ({AssemblyName}).", EventName = "ControllerActionExecuting", SkipEnabledCheck = true)]
        private static partial void ControllerActionExecuting(ILogger logger, string routeData, MethodInfo methodInfo, string controller, string? assemblyName);

        [LoggerMessage(103, LogLevel.Information, "Route matched with {RouteData}. Executing page {PageName}", EventName = "PageExecuting", SkipEnabledCheck = true)]
        private static partial void PageExecuting(ILogger logger, string routeData, string? pageName);

        [LoggerMessage(3, LogLevel.Information, "Authorization failed for the request at filter '{AuthorizationFilter}'.", EventName = "AuthorizationFailure")]
        public static partial void AuthorizationFailure(ILogger logger, IFilterMetadata authorizationFilter);

        [LoggerMessage(4, LogLevel.Debug, "Request was short circuited at resource filter '{ResourceFilter}'.", EventName = "ResourceFilterShortCircuit")]
        public static partial void ResourceFilterShortCircuited(ILogger logger, IFilterMetadata resourceFilter);

        [LoggerMessage(5, LogLevel.Trace, "Before executing action result {ActionResult}.", EventName = "BeforeExecutingActionResult")]
        private static partial void BeforeExecutingActionResult(ILogger logger, Type actionResult);

        public static void BeforeExecutingActionResult(ILogger logger, IActionResult actionResult)
        {
            BeforeExecutingActionResult(logger, actionResult.GetType());
        }

        [LoggerMessage(6, LogLevel.Trace, "After executing action result {ActionResult}.", EventName = "AfterExecutingActionResult")]
        private static partial void AfterExecutingActionResult(ILogger logger, Type actionResult);

        public static void AfterExecutingActionResult(ILogger logger, IActionResult actionResult)
        {
            AfterExecutingActionResult(logger, actionResult.GetType());
        }

        public static void ExecutedAction(ILogger logger, ActionDescriptor action, TimeSpan timeSpan)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (action.RouteValues.TryGetValue("page", out var page) && page != null)
                {
                    PageExecuted(logger, action.DisplayName, timeSpan.TotalMilliseconds);
                }
                else
                {
                    ActionExecuted(logger, action.DisplayName, timeSpan.TotalMilliseconds);
                }
            }
        }

        [LoggerMessage(104, LogLevel.Information, "Executed page {PageName} in {ElapsedMilliseconds}ms", EventName = "PageExecuted", SkipEnabledCheck = true)]
        private static partial void PageExecuted(ILogger logger, string? pageName, double elapsedMilliseconds);

        [LoggerMessage(105, LogLevel.Information, "Executed action {ActionName} in {ElapsedMilliseconds}ms", EventName = "ActionExecuted", SkipEnabledCheck = true)]
        private static partial void ActionExecuted(ILogger logger, string? actionName, double elapsedMilliseconds);
    }
}
