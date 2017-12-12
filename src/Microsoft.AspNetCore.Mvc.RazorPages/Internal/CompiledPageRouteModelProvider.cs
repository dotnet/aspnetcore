// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class CompiledPageRouteModelProvider : IPageRouteModelProvider
    {
        private readonly object _cacheLock = new object();
        private readonly ApplicationPartManager _applicationManager;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly ILogger<CompiledPageRouteModelProvider> _logger;
        private List<PageRouteModel> _cachedModels;

        public CompiledPageRouteModelProvider(
            ApplicationPartManager applicationManager,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _applicationManager = applicationManager;
            _pagesOptions = pagesOptionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<CompiledPageRouteModelProvider>();
        }

        public int Order => -1000;

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            EnsureCache();
            for (var i = 0; i < _cachedModels.Count; i++)
            {
                var pageModel = _cachedModels[i];
                context.RouteModels.Add(new PageRouteModel(pageModel));
            }
        }

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
        }

        private void EnsureCache()
        {
            lock (_cacheLock)
            {
                if (_cachedModels != null)
                {
                    return;
                }

                var rootDirectory = _pagesOptions.RootDirectory;
                if (!rootDirectory.EndsWith("/", StringComparison.Ordinal))
                {
                    rootDirectory = rootDirectory + "/";
                }

                var areaRootDirectory = _pagesOptions.AreaRootDirectory;
                if (!areaRootDirectory.EndsWith("/", StringComparison.Ordinal))
                {
                    areaRootDirectory = areaRootDirectory + "/";
                }

                var cachedApplicationModels = new List<PageRouteModel>();
                foreach (var viewDescriptor in GetViewDescriptors(_applicationManager))
                {
                    PageRouteModel model = null;
                    if (viewDescriptor.RelativePath.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        model = GetPageRouteModel(rootDirectory, viewDescriptor);
                    }
                    else if (_pagesOptions.EnableAreas && viewDescriptor.RelativePath.StartsWith(areaRootDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        model = GetAreaPageRouteModel(areaRootDirectory, viewDescriptor);
                    }

                    if (model != null)
                    {
                        cachedApplicationModels.Add(model);
                    }
                }

                _cachedModels = cachedApplicationModels;
            }
        }

        private PageRouteModel GetPageRouteModel(string rootDirectory, CompiledViewDescriptor viewDescriptor)
        {
            var viewEnginePath = GetRootTrimmedPath(rootDirectory, viewDescriptor.RelativePath);
            if (viewEnginePath.EndsWith(RazorViewEngine.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                viewEnginePath = viewEnginePath.Substring(0, viewEnginePath.Length - RazorViewEngine.ViewExtension.Length);
            }

            var model = new PageRouteModel(viewDescriptor.RelativePath, viewEnginePath);
            var pageAttribute = (RazorPageAttribute)viewDescriptor.ViewAttribute;
            PageSelectorModel.PopulateDefaults(model, viewEnginePath, pageAttribute.RouteTemplate);
            return model;
        }

        private PageRouteModel GetAreaPageRouteModel(string areaRootDirectory, CompiledViewDescriptor viewDescriptor)
        {
            var rootTrimmedPath = GetRootTrimmedPath(areaRootDirectory, viewDescriptor.RelativePath);

            if (PageSelectorModel.TryParseAreaPath(_pagesOptions, rootTrimmedPath, _logger, out var result))
            {
                var model = new PageRouteModel(viewDescriptor.RelativePath, result.viewEnginePath)
                {
                    RouteValues = { ["area"] = result.areaName },
                };
                var pageAttribute = (RazorPageAttribute)viewDescriptor.ViewAttribute;
                PageSelectorModel.PopulateDefaults(model, result.pageRoute, pageAttribute.RouteTemplate);

                return model;
            }

            // We were unable to parse the path to match the format we expect /Areas/AreaName/Pages/PagePath.cshtml
            return null;
        }

        /// <summary>
        /// Gets the sequence of <see cref="CompiledViewDescriptor"/> from <paramref name="applicationManager"/>.
        /// </summary>
        /// <param name="applicationManager">The <see cref="ApplicationPartManager"/>s</param>
        /// <returns>The sequence of <see cref="CompiledViewDescriptor"/>.</returns>
        protected virtual IEnumerable<CompiledViewDescriptor> GetViewDescriptors(ApplicationPartManager applicationManager)
        {
            if (applicationManager == null)
            {
                throw new ArgumentNullException(nameof(applicationManager));
            }

            var viewsFeature = new ViewsFeature();
            applicationManager.PopulateFeature(viewsFeature);

            return viewsFeature.ViewDescriptors.Where(d => d.IsPrecompiled && d.ViewAttribute is RazorPageAttribute);
        }

        private string GetRootTrimmedPath(string rootDirectory, string path)
        {
            // rootDirectory = "/Pages/AllMyPages/"
            // path = "/Pages/AllMyPages/Home.cshtml"
            // Result = "/Home.cshtml"
            var startIndex = rootDirectory.Length - 1;
            return path.Substring(startIndex);
        }
    }
}
