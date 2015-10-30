// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Mvc.ViewEngines
{
    /// <inheritdoc />
    public class CompositeViewEngine : ICompositeViewEngine
    {
        private const string ViewExtension = ".cshtml";

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
            List<string> searchedLocations = null;
            foreach (var engine in ViewEngines)
            {
                var result = engine.FindView(context, viewName, isPartial);
                if (result.Success)
                {
                    return result;
                }

                if (searchedLocations == null)
                {
                    searchedLocations = new List<string>(result.SearchedLocations);
                }
                else
                {
                    searchedLocations.AddRange(result.SearchedLocations);
                }
            }

            return ViewEngineResult.NotFound(viewName, searchedLocations ?? Enumerable.Empty<string>());
        }

        /// <inheritdoc />
        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isPartial)
        {
            List<string> searchedLocations = null;
            foreach (var engine in ViewEngines)
            {
                var result = engine.GetView(executingFilePath, viewPath, isPartial);
                if (result.Success)
                {
                    return result;
                }

                if (searchedLocations == null)
                {
                    searchedLocations = new List<string>(result.SearchedLocations);
                }
                else
                {
                    searchedLocations.AddRange(result.SearchedLocations);
                }
            }

            return ViewEngineResult.NotFound(viewPath, searchedLocations ?? Enumerable.Empty<string>());
        }
    }
}