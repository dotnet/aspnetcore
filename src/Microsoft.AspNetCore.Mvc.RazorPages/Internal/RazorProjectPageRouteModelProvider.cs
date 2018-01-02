// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorProjectPageRouteModelProvider : IPageRouteModelProvider
    {
        private readonly RazorProject _project;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly ILogger _logger;

        public RazorProjectPageRouteModelProvider(
            RazorProject razorProject,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _project = razorProject;
            _pagesOptions = pagesOptionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<RazorProjectPageRouteModelProvider>();
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
            var normalizedAreaRootDirectory = _pagesOptions.AreaRootDirectory;
            if (!normalizedAreaRootDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedAreaRootDirectory += "/";
            }

            foreach (var item in _project.EnumerateItems(_pagesOptions.RootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (IsAlreadyRegistered(context, relativePath))
                {
                    // A route for this file was already registered either by the CompiledPageRouteModel or as an area route.
                    // by this provider. Skip registering an additional entry.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (_pagesOptions.AllowAreas && relativePath.StartsWith(normalizedAreaRootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    // Ignore Razor pages that are under the area root directory when AllowAreas is enabled. 
                    // Conforming page paths will be added by AddAreaPageModels.
                    _logger.UnsupportedAreaPath(_pagesOptions, relativePath);
                    continue;
                }

                var routeModel = new PageRouteModel(relativePath, viewEnginePath: item.FilePathWithoutExtension);
                PageSelectorModel.PopulateDefaults(routeModel, routeModel.ViewEnginePath, routeTemplate);
                context.RouteModels.Add(routeModel);
            }
        }

        private void AddAreaPageModels(PageRouteModelProviderContext context)
        {
            foreach (var item in _project.EnumerateItems(_pagesOptions.AreaRootDirectory))
            {
                if (!IsRouteable(item))
                {
                    continue;
                }

                var relativePath = item.CombinedPath;
                if (IsAlreadyRegistered(context, relativePath))
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

                if (!PageSelectorModel.TryParseAreaPath(_pagesOptions, item.FilePath, _logger, out var areaResult))
                {
                    continue;
                }

                var routeModel = new PageRouteModel(relativePath, viewEnginePath: areaResult.viewEnginePath)
                {
                    RouteValues =
                    {
                        ["area"] = areaResult.areaName,
                    },
                };

                PageSelectorModel.PopulateDefaults(routeModel, areaResult.pageRoute, routeTemplate);
                context.RouteModels.Add(routeModel);
            }
        }

        private bool IsAlreadyRegistered(PageRouteModelProviderContext context, string relativePath)
        {
            for (var i = 0; i < context.RouteModels.Count; i++)
            {
                var existingRouteModel = context.RouteModels[i];
                if (string.Equals(relativePath, existingRouteModel.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRouteable(RazorProjectItem item)
        {
            // Pages like _ViewImports should not be routable.
            return !item.FileName.StartsWith("_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
