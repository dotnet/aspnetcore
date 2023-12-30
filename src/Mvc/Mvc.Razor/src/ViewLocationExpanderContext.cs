// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// A context for containing information for <see cref="IViewLocationExpander"/>.
/// </summary>
public class ViewLocationExpanderContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="ViewLocationExpanderContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current executing action.</param>
    /// <param name="viewName">The view name.</param>
    /// <param name="controllerName">The controller name.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    public ViewLocationExpanderContext(
        ActionContext actionContext,
        string viewName,
        string? controllerName,
        string? areaName,
        string? pageName,
        bool isMainPage)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(viewName);

        ActionContext = actionContext;
        ViewName = viewName;
        ControllerName = controllerName;
        AreaName = areaName;
        PageName = pageName;
        IsMainPage = isMainPage;
    }

    /// <summary>
    /// Gets the <see cref="Mvc.ActionContext"/> for the current executing action.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Gets the view name.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// Gets the controller name.
    /// </summary>
    public string? ControllerName { get; }

    /// <summary>
    /// Gets the page name. This will be the value of the <c>page</c> route value when rendering a Page from the
    /// Razor Pages framework. This value will be <c>null</c> if rendering a view as the result of a controller.
    /// </summary>
    public string? PageName { get; }

    /// <summary>
    /// Gets the area name.
    /// </summary>
    public string? AreaName { get; }

    /// <summary>
    /// Determines if the page being found is the main page for an action.
    /// </summary>
    public bool IsMainPage { get; }

    /// <summary>
    /// Gets or sets the <see cref="IDictionary{TKey, TValue}"/> that is populated with values as part of
    /// <see cref="IViewLocationExpander.PopulateValues(ViewLocationExpanderContext)"/>.
    /// </summary>
    public IDictionary<string, string?> Values { get; set; } = default!;
}
