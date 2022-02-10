// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ViewEngines;

/// <summary>
/// Defines the contract for a view engine.
/// </summary>
public interface IViewEngine
{
    /// <summary>
    /// Finds the view with the given <paramref name="viewName"/> using view locations and information from the
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="viewName">The name or path of the view that is rendered to the response.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    /// <returns>The <see cref="ViewEngineResult"/> of locating the view.</returns>
    /// <remarks>Use <see cref="GetView(string, string, bool)"/> when the absolute or relative
    /// path of the view is known.</remarks>
    ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage);

    /// <summary>
    /// Gets the view with the given <paramref name="viewPath"/>, relative to <paramref name="executingFilePath"/>
    /// unless <paramref name="viewPath"/> is already absolute.
    /// </summary>
    /// <param name="executingFilePath">The absolute path to the currently-executing view, if any.</param>
    /// <param name="viewPath">The path to the view.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    /// <returns>The <see cref="ViewEngineResult"/> of locating the view.</returns>
    ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage);
}
