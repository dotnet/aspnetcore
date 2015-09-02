// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Rendering
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
        public ViewEngineResult FindPartialView([NotNull] ActionContext context,
                                                [NotNull] string partialViewName)
        {
            return FindView(context, partialViewName, partial: true);
        }

        /// <inheritdoc />
        public ViewEngineResult FindView([NotNull] ActionContext context,
                                         [NotNull] string viewName)
        {
            return FindView(context, viewName, partial: false);
        }

        private ViewEngineResult FindView(ActionContext context,
                                          string viewName,
                                          bool partial)
        {
            var searchedLocations = Enumerable.Empty<string>();
            foreach (var engine in ViewEngines)
            {
                var result = partial ? engine.FindPartialView(context, viewName) :
                                       engine.FindView(context, viewName);

                if (result.Success)
                {
                    return result;
                }

                searchedLocations = searchedLocations.Concat(result.SearchedLocations);
            }

            return ViewEngineResult.NotFound(viewName, searchedLocations);
        }
    }
}