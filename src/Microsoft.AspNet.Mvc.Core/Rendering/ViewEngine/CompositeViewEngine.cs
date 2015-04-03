// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="optionsAccessor">The options accessor for <see cref="MvcOptions"/>.</param>
        /// <param name="typeActivatorCache">As <see cref="ITypeActivatorCache"/> instance that creates
        /// an instance of type <see cref="IViewEngine"/>.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance that retrieves services from the
        /// service collection.</param>
        public CompositeViewEngine(
            IOptions<MvcOptions> optionsAccessor,
            ITypeActivatorCache typeActivatorCache,
            IServiceProvider serviceProvider)
        {
            var viewEngines = new List<IViewEngine>();
            foreach (var descriptor in optionsAccessor.Options.ViewEngines)
            {
                IViewEngine viewEngine;
                if (descriptor.ViewEngine != null)
                {
                    viewEngine = descriptor.ViewEngine;
                }
                else
                {
                    viewEngine = typeActivatorCache.CreateInstance<IViewEngine>(
                        serviceProvider, 
                        descriptor.ViewEngineType);
                }

                viewEngines.Add(viewEngine);
            }

            ViewEngines = viewEngines;
        }

        /// <summary>
        /// Gets the list of <see cref="IViewEngine"/> this instance of <see cref="CompositeViewEngine"/> delegates to.
        /// </summary>
        public IReadOnlyList<IViewEngine> ViewEngines { get;  }

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