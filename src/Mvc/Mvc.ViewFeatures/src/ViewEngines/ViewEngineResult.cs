// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewEngines;

/// <summary>
/// Represents the result of an <see cref="IViewEngine" />.
/// </summary>
public class ViewEngineResult
{
    private ViewEngineResult(string viewName)
    {
        ViewName = viewName;
    }

    /// <summary>
    /// The list of locations searched.
    /// </summary>
    public IEnumerable<string> SearchedLocations { get; private init; } = Enumerable.Empty<string>();

    /// <summary>
    /// The <see cref="IView"/>.
    /// </summary>
    public IView? View { get; private init; }

    /// <summary>
    /// Gets or sets the name of the view.
    /// </summary>
    public string ViewName { get; private set; }

    /// <summary>
    /// Whether the result was successful
    /// </summary>
    [MemberNotNullWhen(true, nameof(View))]
    public bool Success => View != null;

    /// <summary>
    /// Returns a result that represents when a view is not found.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <param name="searchedLocations">The locations searched.</param>
    /// <returns>The not found result.</returns>
    public static ViewEngineResult NotFound(
        string viewName,
        IEnumerable<string> searchedLocations)
    {
        ArgumentNullException.ThrowIfNull(viewName);
        ArgumentNullException.ThrowIfNull(searchedLocations);

        return new ViewEngineResult(viewName)
        {
            SearchedLocations = searchedLocations,
        };
    }

    /// <summary>
    /// Returns a result when a view is found.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <param name="view">The <see cref="IView"/>.</param>
    /// <returns>The found result.</returns>
    public static ViewEngineResult Found(string viewName, IView view)
    {
        ArgumentNullException.ThrowIfNull(viewName);
        ArgumentNullException.ThrowIfNull(view);

        return new ViewEngineResult(viewName)
        {
            View = view,
        };
    }

    /// <summary>
    /// Ensure this <see cref="ViewEngineResult"/> was successful.
    /// </summary>
    /// <param name="originalLocations">
    /// Additional <see cref="SearchedLocations"/> to include in the thrown <see cref="InvalidOperationException"/>
    /// if <see cref="Success"/> is <c>false</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="Success"/> is <c>false</c>.
    /// </exception>
    /// <returns>This <see cref="ViewEngineResult"/> if <see cref="Success"/> is <c>true</c>.</returns>
    [MemberNotNull(nameof(View))]
    public ViewEngineResult EnsureSuccessful(IEnumerable<string>? originalLocations)
    {
        if (!Success)
        {
            var locations = string.Empty;
            if (originalLocations != null && originalLocations.Any())
            {
                locations = Environment.NewLine + string.Join(Environment.NewLine, originalLocations);
            }

            if (SearchedLocations.Any())
            {
                locations += Environment.NewLine + string.Join(Environment.NewLine, SearchedLocations);
            }

            throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(ViewName, locations));
        }

        return this;
    }
}
