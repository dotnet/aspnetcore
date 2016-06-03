// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IRazorViewEngine"/>.
    /// </summary>
    /// <remarks>
    /// For <c>ViewResults</c> returned from controllers, views should be located in
    /// <see cref="RazorViewEngineOptions.ViewLocationFormats"/>
    /// by default. For the controllers in an area, views should exist in
    /// <see cref="RazorViewEngineOptions.AreaViewLocationFormats"/>.
    /// </remarks>
    public class RazorViewEngine : IRazorViewEngine
    {
        public static readonly string ViewExtension = ".cshtml";

        private const string ControllerKey = "controller";
        private const string AreaKey = "area";
        private static readonly ViewLocationCacheItem[] EmptyViewStartLocationCacheItems =
            new ViewLocationCacheItem[0];
        private static readonly TimeSpan _cacheExpirationDuration = TimeSpan.FromMinutes(20);

        private readonly IRazorPageFactoryProvider _pageFactory;
        private readonly IRazorPageActivator _pageActivator;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger _logger;
        private readonly RazorViewEngineOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine" />.
        /// </summary>
        public RazorViewEngine(
            IRazorPageFactoryProvider pageFactory,
            IRazorPageActivator pageActivator,
            HtmlEncoder htmlEncoder,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _options = optionsAccessor.Value;

            if (_options.ViewLocationFormats.Count == 0)
            {
                throw new ArgumentException(
                    Resources.FormatViewLocationFormatsIsRequired(nameof(RazorViewEngineOptions.ViewLocationFormats)),
                    nameof(optionsAccessor));
            }

            if (_options.AreaViewLocationFormats.Count == 0)
            {
                throw new ArgumentException(
                    Resources.FormatViewLocationFormatsIsRequired(nameof(RazorViewEngineOptions.AreaViewLocationFormats)),
                    nameof(optionsAccessor));
            }

            _pageFactory = pageFactory;
            _pageActivator = pageActivator;
            _htmlEncoder = htmlEncoder;
            _logger = loggerFactory.CreateLogger<RazorViewEngine>();
            ViewLookupCache = new MemoryCache(new MemoryCacheOptions
            {
                CompactOnMemoryPressure = false
            });
        }

        /// <summary>
        /// A cache for results of view lookups.
        /// </summary>
        protected IMemoryCache ViewLookupCache { get; }

        /// <summary>
        /// Gets the case-normalized route value for the specified route <paramref name="key"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="key">The route key to lookup.</param>
        /// <returns>The value corresponding to the key.</returns>
        /// <remarks>
        /// The casing of a route value in <see cref="ActionContext.RouteData"/> is determined by the client.
        /// This making constructing paths for view locations in a case sensitive file system unreliable. Using the
        /// <see cref="Abstractions.ActionDescriptor.RouteValues"/> to get route values
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

            string value;
            if (actionDescriptor.RouteValues.TryGetValue(key, out value) &&
                !string.IsNullOrEmpty(value))
            {
                normalizedValue = value;
            }

            var stringRouteValue = routeValue?.ToString();
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
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

            if (IsApplicationRelativePath(pageName) || IsRelativePath(pageName))
            {
                // A path; not a name this method can handle.
                return new RazorPageResult(pageName, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromViewLocations(context, pageName, isMainPage: false);
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

        /// <inheritdoc />
        public RazorPageResult GetPage(string executingFilePath, string pagePath)
        {
            if (string.IsNullOrEmpty(pagePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(pagePath));
            }

            if (!(IsApplicationRelativePath(pagePath) || IsRelativePath(pagePath)))
            {
                // Not a path this method can handle.
                return new RazorPageResult(pagePath, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromPath(executingFilePath, pagePath, isMainPage: false);
            if (cacheResult.Success)
            {
                var razorPage = cacheResult.ViewEntry.PageFactory();
                return new RazorPageResult(pagePath, razorPage);
            }
            else
            {
                return new RazorPageResult(pagePath, cacheResult.SearchedLocations);
            }
        }

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

            if (IsApplicationRelativePath(viewName) || IsRelativePath(viewName))
            {
                // A path; not a name this method can handle.
                return ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromViewLocations(context, viewName, isMainPage);
            return CreateViewEngineResult(cacheResult, viewName);
        }

        /// <inheritdoc />
        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            if (string.IsNullOrEmpty(viewPath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(viewPath));
            }

            if (!(IsApplicationRelativePath(viewPath) || IsRelativePath(viewPath)))
            {
                // Not a path this method can handle.
                return ViewEngineResult.NotFound(viewPath, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromPath(executingFilePath, viewPath, isMainPage);
            return CreateViewEngineResult(cacheResult, viewPath);
        }

        private ViewLocationCacheResult LocatePageFromPath(string executingFilePath, string pagePath, bool isMainPage)
        {
            var applicationRelativePath = GetAbsolutePath(executingFilePath, pagePath);
            var cacheKey = new ViewLocationCacheKey(applicationRelativePath, isMainPage);
            ViewLocationCacheResult cacheResult;
            if (!ViewLookupCache.TryGetValue(cacheKey, out cacheResult))
            {
                var expirationTokens = new HashSet<IChangeToken>();
                cacheResult = CreateCacheResult(expirationTokens, applicationRelativePath, isMainPage);

                var cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.SetSlidingExpiration(_cacheExpirationDuration);
                foreach (var expirationToken in expirationTokens)
                {
                    cacheEntryOptions.AddExpirationToken(expirationToken);
                }

                // No views were found at the specified location. Create a not found result.
                if (cacheResult == null)
                {
                    cacheResult = new ViewLocationCacheResult(new[] { applicationRelativePath });
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
            bool isMainPage)
        {
            var controllerName = GetNormalizedRouteValue(actionContext, ControllerKey);
            var areaName = GetNormalizedRouteValue(actionContext, AreaKey);
            var expanderContext = new ViewLocationExpanderContext(
                actionContext,
                pageName,
                controllerName,
                areaName,
                isMainPage);
            Dictionary<string, string> expanderValues = null;

            if (_options.ViewLocationExpanders.Count > 0)
            {
                expanderValues = new Dictionary<string, string>(StringComparer.Ordinal);
                expanderContext.Values = expanderValues;

                // Perf: Avoid allocations
                for (var i = 0; i < _options.ViewLocationExpanders.Count; i++)
                {
                    _options.ViewLocationExpanders[i].PopulateValues(expanderContext);
                }
            }

            var cacheKey = new ViewLocationCacheKey(
                expanderContext.ViewName,
                expanderContext.ControllerName,
                expanderContext.AreaName,
                expanderContext.IsMainPage,
                expanderValues);

            ViewLocationCacheResult cacheResult;
            if (!ViewLookupCache.TryGetValue(cacheKey, out cacheResult))
            {
                _logger.ViewLookupCacheMiss(cacheKey.ViewName, cacheKey.ControllerName);
                cacheResult = OnCacheMiss(expanderContext, cacheKey);
            }
            else
            {
                _logger.ViewLookupCacheHit(cacheKey.ViewName, cacheKey.ControllerName);
            }

            return cacheResult;
        }

        /// <inheritdoc />
        public string GetAbsolutePath(string executingFilePath, string pagePath)
        {
            if (string.IsNullOrEmpty(pagePath))
            {
                // Path is not valid; no change required.
                return pagePath;
            }

            if (IsApplicationRelativePath(pagePath))
            {
                // An absolute path already; no change required.
                return pagePath;
            }

            if (!IsRelativePath(pagePath))
            {
                // A page name; no change required.
                return pagePath;
            }

            // Given a relative path i.e. not yet application-relative (starting with "~/" or "/"), interpret
            // path relative to currently-executing view, if any.
            if (string.IsNullOrEmpty(executingFilePath))
            {
                // Not yet executing a view. Start in app root.
                return "/" + pagePath;
            }

            // Get directory name (including final slash) but do not use Path.GetDirectoryName() to preserve path
            // normalization.
            var index = executingFilePath.LastIndexOf('/');
            Debug.Assert(index >= 0);
            return executingFilePath.Substring(0, index + 1) + pagePath;
        }

        private ViewLocationCacheResult OnCacheMiss(
            ViewLocationExpanderContext expanderContext,
            ViewLocationCacheKey cacheKey)
        {
            // Only use the area view location formats if we have an area token.
            IEnumerable<string> viewLocations = !string.IsNullOrEmpty(expanderContext.AreaName) ?
                _options.AreaViewLocationFormats :
                _options.ViewLocationFormats;

            for (var i = 0; i < _options.ViewLocationExpanders.Count; i++)
            {
                viewLocations = _options.ViewLocationExpanders[i].ExpandViewLocations(expanderContext, viewLocations);
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

                cacheResult = CreateCacheResult(expirationTokens, path, expanderContext.IsMainPage);
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
            HashSet<IChangeToken> expirationTokens,
            string relativePath,
            bool isMainPage)
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
                // Only need to lookup _ViewStarts for the main page.
                var viewStartPages = isMainPage ?
                    GetViewStartPages(relativePath, expirationTokens) :
                    EmptyViewStartLocationCacheItems;

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

        private ViewEngineResult CreateViewEngineResult(ViewLocationCacheResult result, string viewName)
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
                viewStarts[i] = viewStartItem.PageFactory();
            }

            var view = new RazorView(this, _pageActivator, viewStarts, page, _htmlEncoder);
            return ViewEngineResult.Found(viewName, view);
        }

        private static bool IsApplicationRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            return name[0] == '~' || name[0] == '/';
        }

        private static bool IsRelativePath(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            // Though ./ViewName looks like a relative path, framework searches for that view using view locations.
            return name.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
