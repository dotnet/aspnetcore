// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Mvc.ViewEngines
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
        public ViewEngineResult FindView(ActionContext context, string viewName, bool isPartial)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewName));
            }

            // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
            IEnumerable<string> searchedLocations = null;
            List<string> searchedList = null;
            for (var index = 0; index < ViewEngines.Count; index++)
            {
                var result = ViewEngines[index].FindView(context, viewName, isPartial);
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
        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isPartial)
        {
            if (string.IsNullOrEmpty(viewPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewPath));
            }

            // Do not allocate in the common cases: ViewEngines contains one entry or initial attempt is successful.
            IEnumerable<string> searchedLocations = null;
            List<string> searchedList = null;
            for (var index = 0; index < ViewEngines.Count; index++)
            {
                var result = ViewEngines[index].GetView(executingFilePath, viewPath, isPartial);
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