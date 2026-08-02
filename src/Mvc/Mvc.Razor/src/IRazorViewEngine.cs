// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// An <see cref="IViewEngine"/> used to render pages that use the Razor syntax.
/// </summary>
public interface IRazorViewEngine : IViewEngine
{
    /// <summary>
    /// Finds the page with the given <paramref name="pageName"/> using view locations and information from the
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="pageName">The name or path of the page.</param>
    /// <returns>The <see cref="RazorPageResult"/> of locating the page.</returns>
    /// <remarks>
    /// Use <see cref="GetPage(string, string)"/> when the absolute or relative path of the page is known.
    /// <seealso cref="IViewEngine.FindView"/>.
    /// </remarks>
    RazorPageResult FindPage(ActionContext context, string pageName);

    /// <summary>
    /// Gets the page with the given <paramref name="pagePath"/>, relative to <paramref name="executingFilePath"/>
    /// unless <paramref name="pagePath"/> is already absolute.
    /// </summary>
    /// <param name="executingFilePath">The absolute path to the currently-executing page, if any.</param>
    /// <param name="pagePath">The path to the page.</param>
    /// <returns>The <see cref="RazorPageResult"/> of locating the page.</returns>
    /// <remarks><seealso cref="IViewEngine.GetView"/>.</remarks>
    RazorPageResult GetPage(string executingFilePath, string pagePath);

    /// <summary>
    /// Converts the given <paramref name="pagePath"/> to be absolute, relative to
    /// <paramref name="executingFilePath"/> unless <paramref name="pagePath"/> is already absolute.
    /// </summary>
    /// <param name="executingFilePath">The absolute path to the currently-executing page, if any.</param>
    /// <param name="pagePath">The path to the page.</param>
    /// <returns>
    /// The combination of <paramref name="executingFilePath"/> and <paramref name="pagePath"/> if
    /// <paramref name="pagePath"/> is a relative path. The <paramref name="pagePath"/> value (unchanged)
    /// otherwise.
    /// </returns>
    string? GetAbsolutePath(string? executingFilePath, string? pagePath);
}
