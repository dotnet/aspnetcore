// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;

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
        private const string ControllerKey = "controller";
        private const string AreaKey = "area";
        private static readonly ViewLocationCacheItem[] EmptyViewStartLocationCacheItems =
            new ViewLocationCacheItem[0];
        private static readonly TimeSpan _cacheExpirationDuration = TimeSpan.FromMinutes(20);

        private readonly IRazorPageFactoryProvider _pageFactory;
        private readonly IList<IViewLocationExpander> _viewLocationExpanders;
        private readonly IRazorPageActivator _pageActivator;
        private readonly HtmlEncoder _htmlEncoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine" />.
        /// </summary>
        public RazorViewEngine(
            IRazorPageFactoryProvider pageFactory,
            IRazorPageActivator pageActivator,
            HtmlEncoder htmlEncoder,
            IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            _pageFactory = pageFactory;
            _pageActivator = pageActivator;
            _viewLocationExpanders = optionsAccessor.Value.ViewLocationExpanders;
            _htmlEncoder = htmlEncoder;
            ViewLookupCache = new MemoryCache(new MemoryCacheOptions
            {
                CompactOnMemoryPressure = false
            });
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
        public virtual IEnumerable<string> ViewLocationFormats { get; } = new[]
        {
            "/Views/{1}/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

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
        public virtual IEnumerable<string> AreaViewLocationFormats { get; } = new[]
        {
            "/Areas/{2}/Views/{1}/{0}" + ViewExtension,
            "/Areas/{2}/Views/Shared/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

        /// <summary>
        /// A cache for results of view lookups.
        /// </summary>
        protected IMemoryCache ViewLookupCache { get; }

        /// <inheritdoc />
        public ViewEngineResult FindView(ActionContext context, string viewName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewName));
            }

            var pageResult = GetViewLocationCacheResult(context, viewName, isPartial: false);
            return CreateViewEngineResult(pageResult, viewName, isPartial: false);
        }

        /// <inheritdoc />
        public ViewEngineResult FindPartialView(ActionContext context, string partialViewName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(partialViewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(partialViewName));
            }

            var pageResult = GetViewLocationCacheResult(context, partialViewName, isPartial: true);
            return CreateViewEngineResult(pageResult, partialViewName, isPartial: true);
        }

        /// <inheritdoc />
        public RazorPageResult FindPage(ActionContext context, string pageName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pageName));
            }

            var cacheResult = GetViewLocationCacheResult(context, pageName, isPartial: true);
            if (cacheResult.Success)
            {
                var razorPage = cacheResult.ViewEntry.PageFactory();
                return new RazorPageResult(pageName, razorPage);
            }
            else
            {
                return new RazorPageResult(pageName, cacheResult.SearchedLocations);
            }
        }

        /// <summary>
        /// Gets the case-normalized route value for the specified route <paramref name="key"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="key">The route key to lookup.</param>
        /// <returns>The value corresponding to the key.</returns>
        /// <remarks>
        /// The casing of a route value in <see cref="ActionContext.RouteData"/> is determined by the client.
        /// This making constructing paths for view locations in a case sensitive file system unreliable. Using the
        /// <see cref="Abstractions.ActionDescriptor.RouteValueDefaults"/> for attribute routes and
        /// <see cref="Abstractions.ActionDescriptor.RouteConstraints"/> for traditional routes to get route values
        /// produces consistently cased results.
        /// </remarks>
        public static string GetNormalizedRouteValue(ActionContext context, string key)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            object routeValue;
            if (!context.RouteData.Values.TryGetValue(key, out routeValue))
            {
                return null;
            }

            var actionDescriptor = context.ActionDescriptor;
            string normalizedValue = null;
            if (actionDescriptor.AttributeRouteInfo != null)
            {
                object match;
                if (actionDescriptor.RouteValueDefaults.TryGetValue(key, out match))
                {
                    normalizedValue = match?.ToString();
                }
            }
            else
            {
                // Perf: Avoid allocations
                for (var i = 0; i < actionDescriptor.RouteConstraints.Count; i++)
                {
                    var constraint = actionDescriptor.RouteConstraints[i];
                    if (string.Equals(constraint.RouteKey, key, StringComparison.Ordinal))
                    {
                        if (constraint.KeyHandling == RouteKeyHandling.DenyKey)
                        {
                            return null;
                        }
                        else if (constraint.KeyHandling == RouteKeyHandling.RequireKey)
                        {
                            normalizedValue = constraint.RouteValue;
                        }

                        // Duplicate keys in RouteConstraints are not allowed.
                        break;
                    }
                }
            }

            var stringRouteValue = routeValue?.ToString();
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }

        private ViewLocationCacheResult GetViewLocationCacheResult(
            ActionContext context,
            string pageName,
            bool isPartial)
        {
            if (IsApplicationRelativePath(pageName))
            {
                return LocatePageFromPath(pageName, isPartial);
            }
            else
            {
                return LocatePageFromViewLocations(context, pageName, isPartial);
            }
        }

        private ViewLocationCacheResult LocatePageFromPath(string pageName, bool isPartial)
        {
            var applicationRelativePath = pageName;
            if (!pageName.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                applicationRelativePath += ViewExtension;
            }

            var cacheKey = new ViewLocationCacheKey(applicationRelativePath, isPartial);
            ViewLocationCacheResult cacheResult;
            if (!ViewLookupCache.TryGetValue(cacheKey, out cacheResult))
            {
                var expirationTokens = new HashSet<IChangeToken>();
                cacheResult = CreateCacheResult(cacheKey, expirationTokens, applicationRelativePath, isPartial);

                var cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.SetSlidingExpiration(_cacheExpirationDuration);
                foreach (var expirationToken in expirationTokens)
                {
                    cacheEntryOptions.AddExpirationToken(expirationToken);
                }

                // No views were found at the specified location. Create a not found result.
                if (cacheResult == null)
                {
                    cacheResult = new ViewLocationCacheResult(new[] { pageName });
                }

                cacheResult = ViewLookupCache.Set<ViewLocationCacheResult>(
                    cacheKey,
                    cacheResult,
                    cacheEntryOptions);
            }

            return cacheResult;
        }

        private ViewLocationCacheResult LocatePageFromViewLocations(
            ActionContext actionContext,
            string pageName,
            bool isPartial)
        {
            var controllerName = GetNormalizedRouteValue(actionContext, ControllerKey);
            var areaName = GetNormalizedRouteValue(actionContext, AreaKey);
            var expanderContext = new ViewLocationExpanderContext(
                actionContext,
                pageName,
                controllerName,
                areaName,
                isPartial);
            Dictionary<string, string> expanderValues = null;

            if (_viewLocationExpanders.Count > 0)
            {
                expanderValues = new Dictionary<string, string>(StringComparer.Ordinal);
                expanderContext.Values = expanderValues;

                // Perf: Avoid allocations
                for (var i = 0; i < _viewLocationExpanders.Count; i++)
                {
                    _viewLocationExpanders[i].PopulateValues(expanderContext);
                }
            }

            var cacheKey = new ViewLocationCacheKey(
                expanderContext.ViewName,
                expanderContext.ControllerName,
                expanderContext.ViewName,
                expanderContext.IsPartial,
                expanderValues);

            ViewLocationCacheResult cacheResult;
            if (!ViewLookupCache.TryGetValue(cacheKey, out cacheResult))
            {
                cacheResult = OnCacheMiss(expanderContext, cacheKey);
            }

            return cacheResult;
        }

        private ViewLocationCacheResult OnCacheMiss(
            ViewLocationExpanderContext expanderContext,
            ViewLocationCacheKey cacheKey)
        {
            // Only use the area view location formats if we have an area token.
            var viewLocations = !string.IsNullOrEmpty(expanderContext.AreaName) ?
                AreaViewLocationFormats :
                ViewLocationFormats;

            for (var i = 0; i < _viewLocationExpanders.Count; i++)
            {
                viewLocations = _viewLocationExpanders[i].ExpandViewLocations(expanderContext, viewLocations);
            }

            ViewLocationCacheResult cacheResult = null;
            var searchedLocations = new List<string>();
            var expirationTokens = new HashSet<IChangeToken>();
            foreach (var location in viewLocations)
            {
                var path = string.Format(
                    CultureInfo.InvariantCulture,
                    location,
                    expanderContext.ViewName,
                    expanderContext.ControllerName,
                    expanderContext.AreaName);

                cacheResult = CreateCacheResult(cacheKey, expirationTokens, path, expanderContext.IsPartial);
                if (cacheResult != null)
                {
                    break;
                }

                searchedLocations.Add(path);
            }

            // No views were found at the specified location. Create a not found result.
            if (cacheResult == null)
            {
                cacheResult = new ViewLocationCacheResult(searchedLocations);
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.SetSlidingExpiration(_cacheExpirationDuration);
            foreach (var expirationToken in expirationTokens)
            {
                cacheEntryOptions.AddExpirationToken(expirationToken);
            }

            return ViewLookupCache.Set<ViewLocationCacheResult>(cacheKey, cacheResult, cacheEntryOptions);
        }

        private ViewLocationCacheResult CreateCacheResult(
            ViewLocationCacheKey cacheKey,
            HashSet<IChangeToken> expirationTokens,
            string relativePath,
            bool isPartial)
        {
            var factoryResult = _pageFactory.CreateFactory(relativePath);
            if (factoryResult.ExpirationTokens != null)
            {
                for (var i = 0; i < factoryResult.ExpirationTokens.Count; i++)
                {
                    expirationTokens.Add(factoryResult.ExpirationTokens[i]);
                }
            }

            if (factoryResult.Success)
            {
                // Don't need to lookup _ViewStarts for partials.
                var viewStartPages = isPartial ?
                    EmptyViewStartLocationCacheItems :
                    GetViewStartPages(relativePath, expirationTokens);

                return new ViewLocationCacheResult(
                    new ViewLocationCacheItem(factoryResult.RazorPageFactory, relativePath),
                    viewStartPages);
            }

            return null;
        }

        private IReadOnlyList<ViewLocationCacheItem> GetViewStartPages(
            string path,
            HashSet<IChangeToken> expirationTokens)
        {
            var viewStartPages = new List<ViewLocationCacheItem>();
            foreach (var viewStartPath in ViewHierarchyUtility.GetViewStartLocations(path))
            {
                var result = _pageFactory.CreateFactory(viewStartPath);
                if (result.ExpirationTokens != null)
                {
                    for (var i = 0; i < result.ExpirationTokens.Count; i++)
                    {
                        expirationTokens.Add(result.ExpirationTokens[i]);
                    }
                }

                if (result.Success)
                {
                    // Populate the viewStartPages list so that _ViewStarts appear in the order the need to be
                    // executed (closest last, furthest first). This is the reverse order in which
                    // ViewHierarchyUtility.GetViewStartLocations returns _ViewStarts.
                    viewStartPages.Insert(0, new ViewLocationCacheItem(result.RazorPageFactory, viewStartPath));
                }
            }

            return viewStartPages;
        }

        private ViewEngineResult CreateViewEngineResult(
            ViewLocationCacheResult result,
            string viewName,
            bool isPartial)
        {
            if (!result.Success)
            {
                return ViewEngineResult.NotFound(viewName, result.SearchedLocations);
            }

            var page = result.ViewEntry.PageFactory();
            var viewStarts = new IRazorPage[result.ViewStartEntries.Count];
            for (var i = 0; i < viewStarts.Length; i++)
            {
                var viewStartItem = result.ViewStartEntries[i];
                viewStarts[i] = result.ViewStartEntries[i].PageFactory();
            }

            var view = new RazorView(
                this,
                _pageActivator,
                viewStarts,
                page,
                _htmlEncoder,
                isPartial);
            return ViewEngineResult.Found(viewName, view);
        }

        private static bool IsApplicationRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            return name[0] == '~' || name[0] == '/';
        }
    }
}
