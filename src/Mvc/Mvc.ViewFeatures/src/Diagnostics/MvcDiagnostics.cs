// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.Diagnostics;

/// <summary>
/// An <see cref="EventData"/> that occurs before a ViewComponent.
/// </summary>
public sealed class BeforeViewComponentEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeViewComponent";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeViewComponentEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="viewComponentContext">The <see cref="ViewComponentContext"/>.</param>
    /// <param name="viewComponent">The <see cref="ViewComponent"/>.</param>
    public BeforeViewComponentEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, object viewComponent)
    {
        ActionDescriptor = actionDescriptor;
        ViewComponentContext = viewComponentContext;
        ViewComponent = viewComponent;
    }

    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="ViewComponentContext"/>.
    /// </summary>
    public ViewComponentContext ViewComponentContext { get; }

    /// <summary>
    /// The view component.
    /// </summary>
    public object ViewComponent { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
        2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a ViewComponent.
/// </summary>
public sealed class AfterViewComponentEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterViewComponent";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterViewComponentEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="viewComponentContext">The <see cref="ViewComponentContext"/>.</param>
    /// <param name="viewComponentResult">The <see cref="ViewComponentResult"/>.</param>
    /// <param name="viewComponent">The <see cref="ViewComponent"/>.</param>
    public AfterViewComponentEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IViewComponentResult viewComponentResult, object viewComponent)
    {
        ActionDescriptor = actionDescriptor;
        ViewComponentContext = viewComponentContext;
        ViewComponentResult = viewComponentResult;
        ViewComponent = viewComponent;
    }

    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="ViewComponentContext"/>.
    /// </summary>
    public ViewComponentContext ViewComponentContext { get; }

    /// <summary>
    /// The <see cref="IViewComponentResult"/>.
    /// </summary>
    public IViewComponentResult ViewComponentResult { get; }

    /// <summary>
    /// The view component.
    /// </summary>
    public object ViewComponent { get; }

    /// <inheritdoc/>
    protected override int Count => 4;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
        2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
        3 => new KeyValuePair<string, object>(nameof(ViewComponentResult), ViewComponentResult),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before a view is executed.
/// </summary>
public sealed class ViewComponentBeforeViewExecuteEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "ViewComponentBeforeViewExecute";

    /// <summary>
    /// Initializes a new instance of <see cref="ViewComponentBeforeViewExecuteEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="viewComponentContext">The <see cref="ViewComponentContext"/>.</param>
    /// <param name="view">The <see cref="IView"/>.</param>
    public ViewComponentBeforeViewExecuteEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
    {
        ActionDescriptor = actionDescriptor;
        ViewComponentContext = viewComponentContext;
        View = view;
    }
    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="ViewComponentContext"/>.
    /// </summary>
    public ViewComponentContext ViewComponentContext { get; }

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView View { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
        2 => new KeyValuePair<string, object>(nameof(View), View),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a view is executed.
/// </summary>
public sealed class ViewComponentAfterViewExecuteEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "ViewComponentAfterViewExecute";

    /// <summary>
    /// Initializes a new instance of <see cref="ViewComponentAfterViewExecuteEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="viewComponentContext">The <see cref="ViewComponentContext"/>.</param>
    /// <param name="view">The <see cref="IView"/>.</param>
    public ViewComponentAfterViewExecuteEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
    {
        ActionDescriptor = actionDescriptor;
        ViewComponentContext = viewComponentContext;
        View = view;
    }

    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="ViewComponentContext"/>.
    /// </summary>
    public ViewComponentContext ViewComponentContext { get; }

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView View { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
        2 => new KeyValuePair<string, object>(nameof(View), View),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before a view.
/// </summary>
public sealed class BeforeViewEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeView";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeViewEventData"/>.
    /// </summary>
    /// <param name="view">The <see cref="IView"/>.</param>
    /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
    public BeforeViewEventData(IView view, ViewContext viewContext)
    {
        View = view;
        ViewContext = viewContext;
    }

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView View { get; }

    /// <summary>
    /// The <see cref="ViewContext"/>.
    /// </summary>
    public ViewContext ViewContext { get; }

    /// <inheritdoc/>
    protected override int Count => 2;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(View), View),
        1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a view.
/// </summary>
public sealed class AfterViewEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterView";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterViewEventData"/>.
    /// </summary>
    /// <param name="view">The <see cref="IView"/>.</param>
    /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
    public AfterViewEventData(IView view, ViewContext viewContext)
    {
        View = view;
        ViewContext = viewContext;
    }

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView View { get; }

    /// <summary>
    /// The <see cref="ViewContext"/>.
    /// </summary>
    public ViewContext ViewContext { get; }

    /// <inheritdoc/>
    protected override int Count => 2;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(View), View),
        1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that when a view is found.
/// </summary>
public sealed class ViewFoundEventData : EventData
{
    // Reuse boxed object for common values
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "ViewFound";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterViewEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="isMainPage">Whether this is a main page.</param>
    /// <param name="result">The <see cref="ActionResult"/>.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <param name="view">The <see cref="IView"/>.</param>
    public ViewFoundEventData(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IView view)
    {
        ActionContext = actionContext;
        IsMainPage = isMainPage;
        Result = result;
        ViewName = viewName;
        View = view;
    }

    /// <summary>
    /// The <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// <see langword="true"/> if a main page.
    /// </summary>
    public bool IsMainPage { get; }

    /// <summary>
    /// The <see cref="ActionResult"/>.
    /// </summary>
    public ActionResult Result { get; }

    /// <summary>
    /// The name of the view.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView View { get; }

    /// <inheritdoc/>
    protected override int Count => 5;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(IsMainPage), IsMainPage ? BoxedTrue : BoxedFalse),
        2 => new KeyValuePair<string, object>(nameof(Result), Result),
        3 => new KeyValuePair<string, object>(nameof(ViewName), ViewName),
        4 => new KeyValuePair<string, object>(nameof(View), View),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that when a view is not found.
/// </summary>
public sealed class ViewNotFoundEventData : EventData
{
    // Reuse boxed object for common values
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "ViewNotFound";

    /// <summary>
    /// Initializes a new instance of <see cref="ViewNotFoundEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="isMainPage">Whether this is a main page.</param>
    /// <param name="result">The <see cref="ActionResult"/>.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <param name="searchedLocations">The locations searched for the view.</param>
    public ViewNotFoundEventData(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IEnumerable<string> searchedLocations)
    {
        ActionContext = actionContext;
        IsMainPage = isMainPage;
        Result = result;
        ViewName = viewName;
        SearchedLocations = searchedLocations;
    }

    /// <summary>
    /// The <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// <see langword="true"/> if a main page.
    /// </summary>
    public bool IsMainPage { get; }

    /// <summary>
    /// The <see cref="ActionResult"/>.
    /// </summary>
    public ActionResult Result { get; }

    /// <summary>
    /// The name of the view.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// The locations that were searched.
    /// </summary>
    public IEnumerable<string> SearchedLocations { get; }

    /// <inheritdoc/>
    protected override int Count => 5;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(IsMainPage), IsMainPage ? BoxedTrue : BoxedFalse),
        2 => new KeyValuePair<string, object>(nameof(Result), Result),
        3 => new KeyValuePair<string, object>(nameof(ViewName), ViewName),
        4 => new KeyValuePair<string, object>(nameof(SearchedLocations), SearchedLocations),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
