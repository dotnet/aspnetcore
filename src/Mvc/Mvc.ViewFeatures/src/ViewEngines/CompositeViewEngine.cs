// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewEngines;

/// <inheritdoc />
public class CompositeViewEngine : ICompositeViewEngine
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompositeViewEngine"/>.
    /// </summary>
    /// <param name="optionsAccessor">The options accessor for <see cref="MvcViewOptions"/>.</param>
    public CompositeViewEngine(IOptions<MvcViewOptions> optionsAccessor)
    {
        ViewEngines = optionsAccessor.Value.ViewEngines.ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<IViewEngine> ViewEngines { get; }

    /// <inheritdoc />
    public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(viewName);

        if (ViewEngines.Count == 0)
        {
            throw new InvalidOperationException(Resources.FormatViewEnginesAreRequired(
                typeof(MvcViewOptions).FullName,
                nameof(MvcViewOptions.ViewEngines),
                typeof(IViewEngine).FullName));
        }

        // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
        IEnumerable<string>? searchedLocations = null;
        List<string>? searchedList = null;
        for (var i = 0; i < ViewEngines.Count; i++)
        {
            var result = ViewEngines[i].FindView(context, viewName, isMainPage);
            if (result.Success)
            {
                if (result.View is IAsyncDisposable)
                {
                    throw new InvalidOperationException(Resources.FormatAsyncDisposableViewsNotSupported(typeof(IAsyncDisposable).FullName));
                }
                return result;
            }

            if (searchedLocations == null)
            {
                // First failure.
                searchedLocations = result.SearchedLocations;
            }
            else
            {
                if (searchedList == null)
                {
                    // Second failure.
                    searchedList = new List<string>(searchedLocations);
                    searchedLocations = searchedList;
                }

                searchedList.AddRange(result.SearchedLocations);
            }
        }

        return ViewEngineResult.NotFound(viewName, searchedLocations ?? Enumerable.Empty<string>());
    }

    /// <inheritdoc />
    public ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage)
    {
        ArgumentException.ThrowIfNullOrEmpty(viewPath);

        if (ViewEngines.Count == 0)
        {
            throw new InvalidOperationException(Resources.FormatViewEnginesAreRequired(
                typeof(MvcViewOptions).FullName,
                nameof(MvcViewOptions.ViewEngines),
                typeof(IViewEngine).FullName));
        }

        // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
        IEnumerable<string>? searchedLocations = null;
        List<string>? searchedList = null;
        for (var i = 0; i < ViewEngines.Count; i++)
        {
            var result = ViewEngines[i].GetView(executingFilePath, viewPath, isMainPage);
            if (result.Success)
            {
                return result;
            }

            if (searchedLocations == null)
            {
                // First failure.
                searchedLocations = result.SearchedLocations;
            }
            else
            {
                if (searchedList == null)
                {
                    // Second failure.
                    searchedList = new List<string>(searchedLocations);
                    searchedLocations = searchedList;
                }

                if (result.SearchedLocations is not null)
                {
                    searchedList.AddRange(result.SearchedLocations);
                }
            }
        }

        return ViewEngineResult.NotFound(viewPath, searchedLocations ?? Enumerable.Empty<string>());
    }
}
