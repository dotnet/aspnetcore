// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorProjectPageRouteModelProvider : IPageRouteModelProvider
    {
        private const string AreaRootDirectory = "/Areas";
        private readonly RazorProjectFileSystem _razorFileSystem;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly PageRouteModelFactory _routeModelFactory;
        private readonly ILogger<RazorProjectPageRouteModelProvider> _logger;

        public RazorProjectPageRouteModelProvider(
            RazorProjectFileSystem razorFileSystem,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _razorFileSystem = razorFileSystem;
            _pagesOptions = pagesOptionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<RazorProjectPageRouteModelProvider>();
            _routeModelFactory = new PageRouteModelFactory(_pagesOptions, _logger);
        }

        /// <remarks>
        /// Ordered to execute after <see cref="CompiledPageRouteModelProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            // When RootDirectory and AreaRootDirectory overlap, e.g. RootDirectory = /, AreaRootDirectoryy = /Areas;
            // we need to ensure that the page is only route-able via the area route. By adding area routes first,
            // we'll ensure non area routes get skipped when it encounters an IsAlreadyRegistered check.

            if (_pagesOptions.AllowAreas)
            {
                AddAreaPageModels(context);
            }

            AddPageModels(context);
        }

        private void AddPageModels(PageRouteModelProviderContext context)
        {
            foreach (var item in _razorFileSystem.EnumerateItems(_pagesOptions.RootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (context.RouteModels.Any(m => string.Equals(relativePath, m.RelativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    // A route for this file was already registered either by the CompiledPageRouteModel or as an area route.
                    // by this provider. Skip registering an additional entry.

                    // Note: We're comparing duplicates based on root-relative paths. This eliminates a page from being discovered
                    // by overlapping area and non-area routes where ViewEnginePath would be different.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (_pagesOptions.AllowAreas && relativePath.StartsWith(AreaRootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    // Ignore Razor pages that are under the area root directory when AllowAreas is enabled. 
                    // Conforming page paths will be added by AddAreaPageModels.
                    _logger.UnsupportedAreaPath(relativePath);
                    continue;
                }

                var routeModel = _routeModelFactory.CreateRouteModel(relativePath, routeTemplate);
                if (routeModel != null)
                {
                    context.RouteModels.Add(routeModel);
                }
            }
        }

        private void AddAreaPageModels(PageRouteModelProviderContext context)
        {
            foreach (var item in _razorFileSystem.EnumerateItems(AreaRootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (context.RouteModels.Any(m => string.Equals(relativePath, m.RelativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    // A route for this file was already registered either by the CompiledPageRouteModel.
                    // Skip registering an additional entry.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                var routeModel = _routeModelFactory.CreateAreaRouteModel(relativePath, routeTemplate);
                if (routeModel != null)
                {
                    context.RouteModels.Add(routeModel);
                }
            }
        }

        private static bool IsRouteable(RazorProjectItem item)
        {
            // Pages like _ViewImports should not be routable.
            return !item.FileName.StartsWith("_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
