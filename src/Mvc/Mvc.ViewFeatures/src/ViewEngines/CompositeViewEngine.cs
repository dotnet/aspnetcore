// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewName));
            }

            if (ViewEngines.Count == 0)
            {
                throw new InvalidOperationException(Resources.FormatViewEnginesAreRequired(
                    typeof(MvcViewOptions).FullName,
                    nameof(MvcViewOptions.ViewEngines),
                    typeof(IViewEngine).FullName));
            }

            // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
            IEnumerable<string> searchedLocations = null;
            List<string> searchedList = null;
            for (var i = 0; i < ViewEngines.Count; i++)
            {
                var result = ViewEngines[i].FindView(context, viewName, isMainPage);
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

                    searchedList.AddRange(result.SearchedLocations);
                }
            }

            return ViewEngineResult.NotFound(viewName, searchedLocations ?? Enumerable.Empty<string>());
        }

        /// <inheritdoc />
        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            if (string.IsNullOrEmpty(viewPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewPath));
            }

            if (ViewEngines.Count == 0)
            {
                throw new InvalidOperationException(Resources.FormatViewEnginesAreRequired(
                    typeof(MvcViewOptions).FullName,
                    nameof(MvcViewOptions.ViewEngines),
                    typeof(IViewEngine).FullName));
            }

            // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
            IEnumerable<string> searchedLocations = null;
            List<string> searchedList = null;
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

                    searchedList.AddRange(result.SearchedLocations);
                }
            }

            return ViewEngineResult.NotFound(viewPath, searchedLocations ?? Enumerable.Empty<string>());
        }
    }
}