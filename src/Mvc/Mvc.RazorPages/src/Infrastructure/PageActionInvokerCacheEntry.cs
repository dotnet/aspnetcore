// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageActionInvokerCacheEntry
{
    public PageActionInvokerCacheEntry(
        CompiledPageActionDescriptor actionDescriptor,
        Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> viewDataFactory,
        Func<PageContext, ViewContext, object> pageFactory,
        Func<PageContext, ViewContext, object, ValueTask>? releasePage,
        Func<PageContext, object>? modelFactory,
        Func<PageContext, object, ValueTask>? releaseModel,
        Func<PageContext, object, Task> propertyBinder,
        PageHandlerExecutorDelegate[] handlerExecutors,
        PageHandlerBinderDelegate[] handlerBinders,
        IReadOnlyList<Func<IRazorPage>> viewStartFactories,
        FilterItem[] cacheableFilters)
    {
        ActionDescriptor = actionDescriptor;
        ViewDataFactory = viewDataFactory;
        PageFactory = pageFactory;
        ReleasePage = releasePage;
        ModelFactory = modelFactory;
        ReleaseModel = releaseModel;
        PropertyBinder = propertyBinder;
        HandlerExecutors = handlerExecutors;
        HandlerBinders = handlerBinders;
        ViewStartFactories = viewStartFactories;
        CacheableFilters = cacheableFilters;
    }

    public CompiledPageActionDescriptor ActionDescriptor { get; }

    public Func<PageContext, ViewContext, object> PageFactory { get; }

    /// <summary>
    /// The action invoked to release a page. This may be <c>null</c>.
    /// </summary>
    public Func<PageContext, ViewContext, object, ValueTask>? ReleasePage { get; }

    public Func<PageContext, object>? ModelFactory { get; }

    /// <summary>
    /// The delegate invoked to release a model. This may be <c>null</c>.
    /// </summary>
    public Func<PageContext, object, ValueTask>? ReleaseModel { get; }

    /// <summary>
    /// The delegate invoked to bind either the handler type (page or model).
    /// This may be <c>null</c>.
    /// </summary>
    public Func<PageContext, object, Task> PropertyBinder { get; }

    public PageHandlerExecutorDelegate[] HandlerExecutors { get; }

    public PageHandlerBinderDelegate[] HandlerBinders { get; }

    public Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> ViewDataFactory { get; }

    /// <summary>
    /// Gets the applicable ViewStart pages.
    /// </summary>
    public IReadOnlyList<Func<IRazorPage>> ViewStartFactories { get; }

    public FilterItem[] CacheableFilters { get; }
}
