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
            foreach (var item in _project.EnumerateItems(_pagesOptions.RootDirectory))
            {
                if (item.FileName.StartsWith("_"))
                {
                    // Pages like _ViewImports should not be routable.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (IsAlreadyRegistered(context, item))
                {
                    // The CompiledPageRouteModelProvider (or another provider) already registered a PageRoute for this path.
                    // Don't register a duplicate entry for this route.
                    continue;
                }

                var routeModel = new PageRouteModel(
                    relativePath: item.CombinedPath,
                    viewEnginePath: item.FilePathWithoutExtension);
                PageSelectorModel.PopulateDefaults(routeModel, routeTemplate);

                context.RouteModels.Add(routeModel);
            }
        }

        private bool IsAlreadyRegistered(PageRouteModelProviderContext context, RazorProjectItem projectItem)
        {
            for (var i = 0; i < context.RouteModels.Count; i++)
            {
                var routeModel = context.RouteModels[i];
                if (string.Equals(routeModel.ViewEnginePath, projectItem.FilePathWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(routeModel.RelativePath, projectItem.CombinedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
