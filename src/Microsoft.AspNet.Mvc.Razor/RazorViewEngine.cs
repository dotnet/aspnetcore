// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a view engine that is used to render a page that uses the Razor syntax.
    /// </summary>
    public class RazorViewEngine : IViewEngine
    {
        private const string ViewExtension = ".cshtml";
        internal const string ControllerKey = "controller";
        internal const string AreaKey = "area";

        private static readonly IEnumerable<string> _viewLocationFormats = new[]
        {
            "/Views/{1}/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

        private static readonly IEnumerable<string> _areaViewLocationFormats = new[]
        {
            "/Areas/{2}/Views/{1}/{0}" + ViewExtension,
            "/Areas/{2}/Views/Shared/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

        private readonly IRazorPageFactory _pageFactory;
        private readonly IReadOnlyList<IViewLocationExpander> _viewLocationExpanders;
        private readonly IViewLocationCache _viewLocationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine" /> class.
        /// </summary>
        /// <param name="pageFactory">The page factory used for creating <see cref="IRazorPage"/> instances.</param>
        public RazorViewEngine(IRazorPageFactory pageFactory,
                               IViewLocationExpanderProvider viewLocationExpanderProvider,
                               IViewLocationCache viewLocationCache)
        {
            _pageFactory = pageFactory;
            _viewLocationExpanders = viewLocationExpanderProvider.ViewLocationExpanders;
            _viewLocationCache = viewLocationCache;
        }

        /// <summary>
        /// Gets the locations where this instance of <see cref="RazorViewEngine"/> will search for views.
        /// </summary>
        public virtual IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        /// <summary>
        /// Gets the locations where this instance of <see cref="RazorViewEngine"/> will search for views within an
        /// area.
        /// </summary>
        public virtual IEnumerable<string> AreaViewLocationFormats
        {
            get { return _areaViewLocationFormats; }
        }

        /// <inheritdoc />
        public ViewEngineResult FindView([NotNull] ActionContext context,
                                         [NotNull] string viewName)
        {
            return CreateViewEngineResult(context, viewName, partial: false);
        }

        /// <inheritdoc />
        public ViewEngineResult FindPartialView([NotNull] ActionContext context,
                                                [NotNull] string partialViewName)
        {
            return CreateViewEngineResult(context, partialViewName, partial: true);
        }

        private ViewEngineResult CreateViewEngineResult(ActionContext context,
                                                        string viewName,
                                                        bool partial)
        {
            var nameRepresentsPath = IsSpecificPath(viewName);

            if (nameRepresentsPath)
            {
                if (viewName.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var page = _pageFactory.CreateInstance(viewName);
                    if (page != null)
                    {
                        return CreateFoundResult(context, page, viewName, partial);
                    }
                }
                return ViewEngineResult.NotFound(viewName, new[] { viewName });
            }
            else
            {
                return LocateViewFromViewLocations(context, viewName, partial);
            }
        }

        private ViewEngineResult LocateViewFromViewLocations(ActionContext context,
                                                             string viewName,
                                                             bool partial)
        {
            // Initialize the dictionary for the typical case of having controller and action tokens.
            var routeValues = context.RouteData.Values;
            var areaName = routeValues.GetValueOrDefault<string>(AreaKey);

            // Only use the area view location formats if we have an area token.
            var viewLocations = !string.IsNullOrEmpty(areaName) ? AreaViewLocationFormats :
                                                                  ViewLocationFormats;

            var expanderContext = new ViewLocationExpanderContext(context, viewName);
            if (_viewLocationExpanders.Count > 0)
            {
                expanderContext.Values = new Dictionary<string, string>(StringComparer.Ordinal);

                // 1. Populate values from viewLocationExpanders.
                foreach (var expander in _viewLocationExpanders)
                {
                    expander.PopulateValues(expanderContext);
                }
            }

            // 2. With the values that we've accumumlated so far, check if we have a cached result.
            var viewLocation = _viewLocationCache.Get(expanderContext);
            if (!string.IsNullOrEmpty(viewLocation))
            {
                var page = _pageFactory.CreateInstance(viewLocation);

                if (page != null)
                {
                    // 2a. We found a IRazorPage at the cached location.
                    return CreateFoundResult(context, page, viewName, partial);
                }
            }

            // 2b. We did not find a cached location or did not find a IRazorPage at the cached location.
            // The cached value has expired and we need to look up the page.
            foreach (var expander in _viewLocationExpanders)
            {
                viewLocations = expander.ExpandViewLocations(expanderContext, viewLocations);
            }

            // 3. Use the expanded locations to look up a page.
            var controllerName = routeValues.GetValueOrDefault<string>(ControllerKey);
            var searchedLocations = new List<string>();
            foreach (var path in viewLocations)
            {
                var transformedPath = string.Format(CultureInfo.InvariantCulture,
                                                    path,
                                                    viewName,
                                                    controllerName,
                                                    areaName);
                var page = _pageFactory.CreateInstance(transformedPath);
                if (page != null)
                {
                    // 3a. We found a page. Cache the set of values that produced it and return a found result.
                    _viewLocationCache.Set(expanderContext, transformedPath);
                    return CreateFoundResult(context, page, transformedPath, partial);
                }

                searchedLocations.Add(transformedPath);
            }

            // 3b. We did not find a page for any of the paths.
            return ViewEngineResult.NotFound(viewName, searchedLocations);
        }

        private ViewEngineResult CreateFoundResult(ActionContext actionContext,
                                                   IRazorPage page,
                                                   string viewName,
                                                   bool partial)
        {
            // A single request could result in creating multiple IRazorView instances (for partials, view components)
            // and might store state. We'll use the service container to create new instances as we require.

            var services = actionContext.HttpContext.RequestServices;
            var view = services.GetRequiredService<IRazorView>();

            view.Contextualize(page, partial);
            return ViewEngineResult.Found(viewName, view);
        }

        private static bool IsSpecificPath(string name)
        {
            return name[0] == '~' || name[0] == '/';
        }
    }
}
