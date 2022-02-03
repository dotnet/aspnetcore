// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Key for entries in <see cref="RazorViewEngine.ViewLookupCache"/>.
/// </summary>
internal readonly struct ViewLocationCacheKey : IEquatable<ViewLocationCacheKey>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
    /// </summary>
    /// <param name="viewName">The view name or path.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    public ViewLocationCacheKey(
        string viewName,
        bool isMainPage)
        : this(
              viewName,
              controllerName: null,
              areaName: null,
              pageName: null,
              isMainPage: isMainPage,
              values: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
    /// </summary>
    /// <param name="viewName">The view name.</param>
    /// <param name="controllerName">The controller name.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    /// <param name="values">Values from <see cref="IViewLocationExpander"/> instances.</param>
    public ViewLocationCacheKey(
        string viewName,
        string? controllerName,
        string? areaName,
        string? pageName,
        bool isMainPage,
        IReadOnlyDictionary<string, string?>? values)
    {
        ViewName = viewName;
        ControllerName = controllerName;
        AreaName = areaName;
        PageName = pageName;
        IsMainPage = isMainPage;
        ViewLocationExpanderValues = values;
    }

    /// <summary>
    /// Gets the view name.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// Gets the controller name.
    /// </summary>
    public string? ControllerName { get; }

    /// <summary>
    /// Gets the area name.
    /// </summary>
    public string? AreaName { get; }

    /// <summary>
    /// Gets the page name.
    /// </summary>
    public string? PageName { get; }

    /// <summary>
    /// Determines if the page being found is the main page for an action.
    /// </summary>
    public bool IsMainPage { get; }

    /// <summary>
    /// Gets the values populated by <see cref="IViewLocationExpander"/> instances.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? ViewLocationExpanderValues { get; }

    /// <inheritdoc />
    public bool Equals(ViewLocationCacheKey y)
    {
        if (IsMainPage != y.IsMainPage ||
            !string.Equals(ViewName, y.ViewName, StringComparison.Ordinal) ||
            !string.Equals(ControllerName, y.ControllerName, StringComparison.Ordinal) ||
            !string.Equals(AreaName, y.AreaName, StringComparison.Ordinal) ||
            !string.Equals(PageName, y.PageName, StringComparison.Ordinal))
        {
            return false;
        }

        if (ReferenceEquals(ViewLocationExpanderValues, y.ViewLocationExpanderValues))
        {
            return true;
        }

        if (ViewLocationExpanderValues == null ||
            y.ViewLocationExpanderValues == null ||
            (ViewLocationExpanderValues.Count != y.ViewLocationExpanderValues.Count))
        {
            return false;
        }

        foreach (var item in ViewLocationExpanderValues)
        {
            if (!y.ViewLocationExpanderValues.TryGetValue(item.Key, out var yValue) ||
                !string.Equals(item.Value, yValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ViewLocationCacheKey cacheKey && Equals(cacheKey);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(IsMainPage ? 1 : 0);
        hashCode.Add(ViewName, StringComparer.Ordinal);
        hashCode.Add(ControllerName, StringComparer.Ordinal);
        hashCode.Add(AreaName, StringComparer.Ordinal);
        hashCode.Add(PageName, StringComparer.Ordinal);

        if (ViewLocationExpanderValues != null)
        {
            foreach (var item in ViewLocationExpanderValues)
            {
                hashCode.Add(item.Key, StringComparer.Ordinal);
                hashCode.Add(item.Value, StringComparer.Ordinal);
            }
        }

        return hashCode.ToHashCode();
    }
}
