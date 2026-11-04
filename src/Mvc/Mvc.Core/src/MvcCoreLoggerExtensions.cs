// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc;

internal static partial class MvcCoreLoggerExtensions
{
    public const string ActionFilter = "Action Filter";
    private static readonly string[] _noFilters = new[] { "None" };

    public static IDisposable? ActionScope(this ILogger logger, ActionDescriptor action)
    {
        return logger.BeginScope(new ActionLogScope(action));
    }

    public static void AuthorizationFiltersExecutionPlan(this ILogger logger, IEnumerable<IFilterMetadata> filters)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var authorizationFilters = filters.Where(f => f is IAuthorizationFilter || f is IAsyncAuthorizationFilter);
        LogFilterExecutionPlan(logger, "authorization", authorizationFilters);
    }

    public static void ResourceFiltersExecutionPlan(this ILogger logger, IEnumerable<IFilterMetadata> filters)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var resourceFilters = filters.Where(f => f is IResourceFilter || f is IAsyncResourceFilter);
        LogFilterExecutionPlan(logger, "resource", resourceFilters);
    }

    public static void ActionFiltersExecutionPlan(this ILogger logger, IEnumerable<IFilterMetadata> filters)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var actionFilters = filters.Where(f => f is IActionFilter || f is IAsyncActionFilter);
        LogFilterExecutionPlan(logger, "action", actionFilters);
    }

    public static void ExceptionFiltersExecutionPlan(this ILogger logger, IEnumerable<IFilterMetadata> filters)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var exceptionFilters = filters.Where(f => f is IExceptionFilter || f is IAsyncExceptionFilter);
        LogFilterExecutionPlan(logger, "exception", exceptionFilters);
    }

    public static void ResultFiltersExecutionPlan(this ILogger logger, IEnumerable<IFilterMetadata> filters)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var resultFilters = filters.Where(f => f is IResultFilter || f is IAsyncResultFilter);
        LogFilterExecutionPlan(logger, "result", resultFilters);
    }

    [LoggerMessage(52, LogLevel.Trace, "{FilterType}: Before executing {Method} on filter {Filter}.", EventName = "BeforeExecutingMethodOnFilter")]
    public static partial void BeforeExecutingMethodOnFilter(this ILogger logger, string filterType, string method, IFilterMetadata filter);

    [LoggerMessage(53, LogLevel.Trace, "{FilterType}: After executing {Method} on filter {Filter}.", EventName = "AfterExecutingMethodOnFilter")]
    public static partial void AfterExecutingMethodOnFilter(this ILogger logger, string filterType, string method, IFilterMetadata filter);

    public static void NoActionsMatched(this ILogger logger, IDictionary<string, object?> routeValueDictionary)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            string[]? routeValues = null;
            if (routeValueDictionary is not null)
            {
                routeValues = routeValueDictionary
                    .Select(pair => pair.Key + "=" + Convert.ToString(pair.Value, CultureInfo.InvariantCulture))
                    .ToArray();
            }
            NoActionsMatched(logger, routeValues);
        }
    }

    [LoggerMessage(3, LogLevel.Debug, "No actions matched the current request. Route values: {RouteValues}", EventName = "NoActionsMatched", SkipEnabledCheck = true)]
    private static partial void NoActionsMatched(ILogger logger, string[]? routeValues);

    [LoggerMessage(5, LogLevel.Debug, "Request was short circuited at result filter '{ResultFilter}'.", EventName = "ResultFilterShortCircuit")]
    public static partial void ResultFilterShortCircuited(this ILogger logger, IFilterMetadata resultFilter);

    [LoggerMessage(4, LogLevel.Debug, "Request was short circuited at exception filter '{ExceptionFilter}'.", EventName = "ExceptionFilterShortCircuit")]
    public static partial void ExceptionFilterShortCircuited(this ILogger logger, IFilterMetadata exceptionFilter);

    [LoggerMessage(63, LogLevel.Debug, "Request was short circuited at action filter '{ActionFilter}'.", EventName = "ActionFilterShortCircuit")]
    public static partial void ActionFilterShortCircuited(this ILogger logger, IFilterMetadata actionFilter);

    public static void FoundNoValueInRequest(this ILogger logger, ModelBindingContext bindingContext)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var modelMetadata = bindingContext.ModelMetadata;
        switch (modelMetadata.MetadataKind)
        {
            case ModelMetadataKind.Parameter:
                FoundNoValueForParameterInRequest(
                    logger,
                    bindingContext.ModelName,
                    modelMetadata.ParameterName,
                    bindingContext.ModelType);
                break;
            case ModelMetadataKind.Property:
                FoundNoValueForPropertyInRequest(
                    logger,
                    bindingContext.ModelName,
                    modelMetadata.ContainerType,
                    modelMetadata.PropertyName,
                    bindingContext.ModelType);
                break;
            case ModelMetadataKind.Type:
                FoundNoValueInRequest(
                    logger,
                    bindingContext.ModelName,
                    bindingContext.ModelType);
                break;
        }
    }

    [LoggerMessage(15, LogLevel.Debug, "Could not find a value in the request with name '{ModelName}' for binding property '{PropertyContainerType}.{ModelFieldName}' of type '{ModelType}'.",
        EventName = "FoundNoValueForPropertyInRequest",
        SkipEnabledCheck = true)]
    private static partial void FoundNoValueForPropertyInRequest(ILogger logger, string modelName, Type? propertyContainerType, string? modelFieldName, Type modelType);

    [LoggerMessage(16, LogLevel.Debug, "Could not find a value in the request with name '{ModelName}' for binding parameter '{ModelFieldName}' of type '{ModelType}'.",
        EventName = "FoundNoValueForParameterInRequest",
        SkipEnabledCheck = true)]
    private static partial void FoundNoValueForParameterInRequest(ILogger logger, string modelName, string? modelFieldName, Type modelType);

    [LoggerMessage(46, LogLevel.Debug, "Could not find a value in the request with name '{ModelName}' of type '{ModelType}'.", EventName = "FoundNoValueInRequest", SkipEnabledCheck = true)]
    private static partial void FoundNoValueInRequest(ILogger logger, string modelName, Type modelType);

    public static void CannotBindToFilesCollectionDueToUnsupportedContentType(this ILogger logger, ModelBindingContext bindingContext)
        => CannotBindToFilesCollectionDueToUnsupportedContentType(logger, bindingContext.ModelName, bindingContext.ModelType);

    [LoggerMessage(19, LogLevel.Debug,
        "Could not bind to model with name '{ModelName}' and type '{ModelType}' as the request did not have a content type of either 'application/x-www-form-urlencoded' or 'multipart/form-data'.",
        EventName = "CannotBindToFilesCollectionDueToUnsupportedContentType")]
    private static partial void CannotBindToFilesCollectionDueToUnsupportedContentType(ILogger logger, string modelName, Type modelType);

    public static void AttemptingToBindModel(this ILogger logger, ModelBindingContext bindingContext)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var modelMetadata = bindingContext.ModelMetadata;
        switch (modelMetadata.MetadataKind)
        {
            case ModelMetadataKind.Parameter:
                AttemptingToBindParameterModel(
                    logger,
                    modelMetadata.ParameterName,
                    modelMetadata.ModelType,
                    bindingContext.ModelName);
                break;
            case ModelMetadataKind.Property:
                AttemptingToBindPropertyModel(
                    logger,
                    modelMetadata.ContainerType,
                    modelMetadata.PropertyName,
                    modelMetadata.ModelType,
                    bindingContext.ModelName);
                break;
            case ModelMetadataKind.Type:
                AttemptingToBindModel(logger, bindingContext.ModelType, bindingContext.ModelName);
                break;
        }
    }

    [LoggerMessage(44, LogLevel.Debug, "Attempting to bind parameter '{ParameterName}' of type '{ModelType}' using the name '{ModelName}' in request data ...", EventName = "AttemptingToBindParameterModel", SkipEnabledCheck = true)]
    private static partial void AttemptingToBindParameterModel(ILogger logger, string? parameterName, Type modelType, string modelName);

    [LoggerMessage(13, LogLevel.Debug, "Attempting to bind property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}' using the name '{ModelName}' in request data ...", EventName = "AttemptingToBindPropertyModel", SkipEnabledCheck = true)]
    private static partial void AttemptingToBindPropertyModel(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType, string modelName);

    [LoggerMessage(24, LogLevel.Debug, "Attempting to bind model of type '{ModelType}' using the name '{ModelName}' in request data ...", EventName = "AttemptingToBindModel", SkipEnabledCheck = true)]
    private static partial void AttemptingToBindModel(ILogger logger, Type modelType, string modelName);

    public static void DoneAttemptingToBindModel(this ILogger logger, ModelBindingContext bindingContext)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var modelMetadata = bindingContext.ModelMetadata;
        switch (modelMetadata.MetadataKind)
        {
            case ModelMetadataKind.Parameter:
                DoneAttemptingToBindParameterModel(
                    logger,
                    modelMetadata.ParameterName,
                    modelMetadata.ModelType);
                break;
            case ModelMetadataKind.Property:
                DoneAttemptingToBindPropertyModel(
                    logger,
                    modelMetadata.ContainerType,
                    modelMetadata.PropertyName,
                    modelMetadata.ModelType);
                break;
            case ModelMetadataKind.Type:
                DoneAttemptingToBindModel(logger, bindingContext.ModelType, bindingContext.ModelName);
                break;
        }
    }

    [LoggerMessage(14, LogLevel.Debug, "Done attempting to bind property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}'.", EventName = "DoneAttemptingToBindPropertyModel")]
    private static partial void DoneAttemptingToBindPropertyModel(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType);

    [LoggerMessage(25, LogLevel.Debug, "Done attempting to bind model of type '{ModelType}' using the name '{ModelName}'.", EventName = "DoneAttemptingToBindModel", SkipEnabledCheck = true)]
    private static partial void DoneAttemptingToBindModel(ILogger logger, Type modelType, string modelName);

    [LoggerMessage(45, LogLevel.Debug, "Done attempting to bind parameter '{ParameterName}' of type '{ModelType}'.", EventName = "DoneAttemptingToBindParameterModel", SkipEnabledCheck = true)]
    private static partial void DoneAttemptingToBindParameterModel(ILogger logger, string? parameterName, Type modelType);

    private static void LogFilterExecutionPlan(
        ILogger logger,
        string filterType,
        IEnumerable<IFilterMetadata> filters)
    {
        var filterList = _noFilters;
        if (filters.Any())
        {
            filterList = GetFilterList(filters);
        }

        LogFilterExecutionPlan(logger, filterType, filterList);
    }

    [LoggerMessage(1, LogLevel.Debug, "Execution plan of {FilterType} filters (in the following order): {Filters}", EventName = "FilterExecutionPlan", SkipEnabledCheck = true)]
    private static partial void LogFilterExecutionPlan(ILogger logger, string filterType, string[] filters);

    private static string[] GetFilterList(IEnumerable<IFilterMetadata> filters)
    {
        var filterList = new List<string>();
        foreach (var filter in filters)
        {
            if (filter is IOrderedFilter orderedFilter)
            {
                filterList.Add($"{filter.GetType()} (Order: {orderedFilter.Order})");
            }
            else
            {
                filterList.Add(filter.GetType().ToString());
            }
        }
        return filterList.ToArray();
    }

    private sealed class ActionLogScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly ActionDescriptor _action;

        public ActionLogScope(ActionDescriptor action)
        {
            ArgumentNullException.ThrowIfNull(action);

            _action = action;
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return new KeyValuePair<string, object>("ActionId", _action.Id);
                }
                else if (index == 1)
                {
                    return new KeyValuePair<string, object>("ActionName", _action.DisplayName ?? string.Empty);
                }
                throw new ArgumentOutOfRangeException(nameof(index));
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
            // We don't include the _action.Id here because it's just an opaque guid, and if
            // you have text logging, you can already use the requestId for correlation.
            return _action.DisplayName ?? string.Empty;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
