// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IRazorViewEngine"/>.
    /// </summary>
    /// <remarks>
    /// For <c>ViewResults</c> returned from controllers, views should be located in <see cref="ViewLocationFormats"/>
    /// by default. For the controllers in an area, views should exist in <see cref="AreaViewLocationFormats"/>.
    /// </remarks>
    public class RazorViewEngine : IRazorViewEngine
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
        private readonly IRazorViewFactory _viewFactory;
        private readonly IReadOnlyList<IViewLocationExpander> _viewLocationExpanders;
        private readonly IViewLocationCache _viewLocationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine" /> class.
        /// </summary>
        /// <param name="pageFactory">The page factory used for creating <see cref="IRazorPage"/> instances.</param>
        public RazorViewEngine(IRazorPageFactory pageFactory,
                               IRazorViewFactory viewFactory,
                               IViewLocationExpanderProvider viewLocationExpanderProvider,
                               IViewLocationCache viewLocationCache)
        {
            _pageFactory = pageFactory;
            _viewFactory = viewFactory;
            _viewLocationExpanders = viewLocationExpanderProvider.ViewLocationExpanders;
            _viewLocationCache = viewLocationCache;
        }

        /// <summary>
        /// Gets the locations where this instance of <see cref="RazorViewEngine"/> will search for views.
        /// </summary>
        /// <remarks>
        /// The locations of the views returned from controllers that do not belong to an area.
        /// Locations are composite format strings (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx),
        /// which contains following indexes:
        /// {0} - Action Name
        /// {1} - Controller Name
        /// The values for these locations are case-sensitive on case-senstive file systems.
        /// For example, the view for the <c>Test</c> action of <c>HomeController</c> should be located at
        /// <c>/Views/Home/Test.cshtml</c>. Locations such as <c>/views/home/test.cshtml</c> would not be discovered
        /// </remarks>
        public virtual IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        /// <summary>
        /// Gets the locations where this instance of <see cref="RazorViewEngine"/> will search for views within an
        /// area.
        /// </summary>
        /// <remarks>
        /// The locations of the views returned from controllers that belong to an area.
        /// Locations are composite format strings (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx),
        /// which contains following indexes:
        /// {0} - Action Name
        /// {1} - Controller Name
        /// {2} - Area name
        /// The values for these locations are case-sensitive on case-senstive file systems.
        /// For example, the view for the <c>Test</c> action of <c>HomeController</c> should be located at
        /// <c>/Views/Home/Test.cshtml</c>. Locations such as <c>/views/home/test.cshtml</c> would not be discovered
        /// </remarks>
        public virtual IEnumerable<string> AreaViewLocationFormats
        {
            get { return _areaViewLocationFormats; }
        }

        /// <inheritdoc />
        public ViewEngineResult FindView([NotNull] ActionContext context,
                                         string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewName));
            }

            var pageResult = GetRazorPageResult(context, viewName);
            return CreateViewEngineResult(pageResult, _viewFactory, isPartial: false);
        }

        /// <inheritdoc />
        public ViewEngineResult FindPartialView([NotNull] ActionContext context,
                                                string partialViewName)
        {
            if (string.IsNullOrEmpty(partialViewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(partialViewName));
            }

            var pageResult = GetRazorPageResult(context, partialViewName);
            return CreateViewEngineResult(pageResult, _viewFactory, isPartial: true);
        }

        /// <inheritdoc />
        public RazorPageResult FindPage([NotNull] ActionContext context,
                                        string pageName)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            return GetRazorPageResult(context, pageName);
        }

        private RazorPageResult GetRazorPageResult(ActionContext context,
                                                   string pageName)
        {
            if (IsApplicationRelativePath(pageName))
            {
                var applicationRelativePath = pageName;
                if (!pageName.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase))
                {
                    applicationRelativePath += ViewExtension;
                }

                var page = _pageFactory.CreateInstance(applicationRelativePath);
                if (page != null)
                {
                    return new RazorPageResult(pageName, page);
                }

                return new RazorPageResult(pageName, new[] { pageName });
            }
            else
            {
                return LocatePageFromViewLocations(context, pageName);
            }
        }

        private RazorPageResult LocatePageFromViewLocations(ActionContext context,
                                                            string pageName)
        {
            // Initialize the dictionary for the typical case of having controller and action tokens.
            var routeValues = context.RouteData.Values;
            var areaName = routeValues.GetValueOrDefault<string>(AreaKey);

            // Only use the area view location formats if we have an area token.
            var viewLocations = !string.IsNullOrEmpty(areaName) ? AreaViewLocationFormats :
                                                                  ViewLocationFormats;

            var expanderContext = new ViewLocationExpanderContext(context, pageName);
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
            var pageLocation = _viewLocationCache.Get(expanderContext);
            if (!string.IsNullOrEmpty(pageLocation))
            {
                var page = _pageFactory.CreateInstance(pageLocation);

                if (page != null)
                {
                    // 2a. We found a IRazorPage at the cached location.
                    return new RazorPageResult(pageName, page);
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
                                                    pageName,
                                                    controllerName,
                                                    areaName);
                var page = _pageFactory.CreateInstance(transformedPath);
                if (page != null)
                {
                    // 3a. We found a page. Cache the set of values that produced it and return a found result.
                    _viewLocationCache.Set(expanderContext, transformedPath);
                    return new RazorPageResult(pageName, page);
                }

                searchedLocations.Add(transformedPath);
            }

            // 3b. We did not find a page for any of the paths.
            return new RazorPageResult(pageName, searchedLocations);
        }

        private ViewEngineResult CreateViewEngineResult(RazorPageResult result,
                                                        IRazorViewFactory razorViewFactory,
                                                        bool isPartial)
        {
            if (result.SearchedLocations != null)
            {
                return ViewEngineResult.NotFound(result.Name, result.SearchedLocations);
            }

            var view = razorViewFactory.GetView(this, result.Page, isPartial);
            return ViewEngineResult.Found(result.Name, view);
        }

        private static bool IsApplicationRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            return name[0] == '~' || name[0] == '/';
        }
    }
}
