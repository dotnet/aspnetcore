// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

internal static partial class PageLoggerExtensions
{
    public const string PageFilter = "Page Filter";

    [LoggerMessage(101, LogLevel.Debug, "Executing page model factory for page {Page} ({AssemblyName})", EventName = "ExecutingModelFactory", SkipEnabledCheck = true)]
    private static partial void ExecutingPageModelFactory(this ILogger logger, string page, string assemblyName);

    public static void ExecutingPageModelFactory(this ILogger logger, PageContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
        var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
        ExecutingPageModelFactory(logger, pageName, pageType.Assembly.GetName().Name);
    }

    [LoggerMessage(102, LogLevel.Debug, "Executed page model factory for page {Page} ({AssemblyName})", EventName = "ExecutedModelFactory", SkipEnabledCheck = true)]
    private static partial void ExecutedPageModelFactory(this ILogger logger, string page, string assemblyName);

    public static void ExecutedPageModelFactory(this ILogger logger, PageContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
        var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
        ExecutedPageModelFactory(logger, pageName, pageType.Assembly.GetName().Name);
    }

    [LoggerMessage(103, LogLevel.Debug, "Executing page factory for page {Page} ({AssemblyName})", EventName = "ExecutingPageFactory", SkipEnabledCheck = true)]
    private static partial void ExecutingPageFactory(this ILogger logger, string page, string assemblyName);

    public static void ExecutingPageFactory(this ILogger logger, PageContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
        var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
        ExecutingPageFactory(logger, pageName, pageType.Assembly.GetName().Name);
    }

    [LoggerMessage(104, LogLevel.Debug, "Executed page factory for page {Page} ({AssemblyName})", EventName = "ExecutedPageFactory", SkipEnabledCheck = true)]
    private static partial void ExecutedPageFactory(this ILogger logger, string page, string assemblyName);

    public static void ExecutedPageFactory(this ILogger logger, PageContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var pageType = context.ActionDescriptor.PageTypeInfo.AsType();
        var pageName = TypeNameHelper.GetTypeDisplayName(pageType);
        ExecutedPageFactory(logger, pageName, pageType.Assembly.GetName().Name);
    }

    [LoggerMessage(105, LogLevel.Information, "Executing handler method {HandlerName} - ModelState is {ValidationState}", EventName = "ExecutingHandlerMethod", SkipEnabledCheck = true)]
    private static partial void ExecutingHandlerMethod(this ILogger logger, string handlerName, ModelValidationState validationState);

    [LoggerMessage(106, LogLevel.Trace, "Executing handler method {HandlerName} with arguments ({Arguments})", EventName = "HandlerMethodExecutingWithArguments", SkipEnabledCheck = true)]
    private static partial void ExecutingHandlerMethodWithArguments(this ILogger logger, string handlerName, string[] arguments);

    public static void ExecutingHandlerMethod(this ILogger logger, PageContext context, HandlerMethodDescriptor handler, object?[]? arguments)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            var declaringTypeName = TypeNameHelper.GetTypeDisplayName(handler.MethodInfo.DeclaringType);
            var handlerName = declaringTypeName + "." + handler.MethodInfo.Name;

            var validationState = context.ModelState.ValidationState;
            ExecutingHandlerMethod(logger, handlerName, validationState);

            if (arguments != null && logger.IsEnabled(LogLevel.Trace))
            {
                var convertedArguments = new string[arguments.Length];
                for (var i = 0; i < arguments.Length; i++)
                {
                    convertedArguments[i] = Convert.ToString(arguments[i], CultureInfo.InvariantCulture);
                }

                ExecutingHandlerMethodWithArguments(logger, handlerName, convertedArguments);
            }
        }
    }

    [LoggerMessage(107, LogLevel.Information, "Executing an implicit handler method - ModelState is {ValidationState}", EventName = "ExecutingImplicitHandlerMethod", SkipEnabledCheck = true)]
    public static partial void ExecutingImplicitHandlerMethod(this ILogger logger, ModelValidationState validationState);

    public static void ExecutingImplicitHandlerMethod(this ILogger logger, PageContext context)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            var validationState = context.ModelState.ValidationState;

            ExecutingImplicitHandlerMethod(logger, validationState);
        }
    }

    [LoggerMessage(108, LogLevel.Information, "Executed handler method {HandlerName}, returned result {ActionResult}.", EventName = "ExecutedHandlerMethod")]
    public static partial void ExecutedHandlerMethod(this ILogger logger, string handlerName, string? actionResult);

    public static void ExecutedHandlerMethod(this ILogger logger, HandlerMethodDescriptor handler, IActionResult? result)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            var handlerName = handler.MethodInfo.Name;
            ExecutedHandlerMethod(logger, handlerName, Convert.ToString(result, CultureInfo.InvariantCulture));
        }
    }

    [LoggerMessage(109, LogLevel.Information, "Executed an implicit handler method, returned result {ActionResult}.", EventName = "ExecutedImplicitHandlerMethod", SkipEnabledCheck = true)]
    public static partial void ExecutedImplicitHandlerMethod(this ILogger logger, string actionResult);

    public static void ExecutedImplicitHandlerMethod(this ILogger logger, IActionResult result)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            ExecutedImplicitHandlerMethod(logger, Convert.ToString(result, CultureInfo.InvariantCulture));
        }
    }

    [LoggerMessage(1, LogLevel.Trace, "{FilterType}: Before executing {Method} on filter {Filter}.", EventName = "BeforeExecutingMethodOnFilter")]
    public static partial void BeforeExecutingMethodOnFilter(this ILogger logger, string filterType, string method, IFilterMetadata filter);

    [LoggerMessage(2, LogLevel.Trace, "{FilterType}: After executing {Method} on filter {Filter}.", EventName = "AfterExecutingMethodOnFilter")]
    public static partial void AfterExecutingMethodOnFilter(this ILogger logger, string filterType, string method, IFilterMetadata filter);

    [LoggerMessage(3, LogLevel.Debug, "Request was short circuited at page filter '{PageFilter}'.", EventName = "PageFilterShortCircuited")]
    public static partial void PageFilterShortCircuited(
        this ILogger logger,
        IFilterMetadata pageFilter);

    [LoggerMessage(4, LogLevel.Debug, "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
    public static partial void NotMostEffectiveFilter(this ILogger logger, Type filterPolicy);
}
