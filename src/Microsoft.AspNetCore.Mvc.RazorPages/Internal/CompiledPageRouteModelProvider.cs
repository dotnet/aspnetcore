// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class CompiledPageRouteModelProvider : IPageRouteModelProvider
    {
        private readonly ApplicationPartManager _applicationManager;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly RazorTemplateEngine _templateEngine;
        private readonly ILogger<CompiledPageRouteModelProvider> _logger;

        public CompiledPageRouteModelProvider(
            ApplicationPartManager applicationManager,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            RazorTemplateEngine templateEngine,
            ILoggerFactory loggerFactory)
        {
            if (applicationManager == null)
            {
                throw new ArgumentNullException(nameof(applicationManager));
            }

            if (pagesOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(pagesOptionsAccessor));
            }

            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _applicationManager = applicationManager;
            _pagesOptions = pagesOptionsAccessor.Value;
            _templateEngine = templateEngine;
            _logger = loggerFactory.CreateLogger<CompiledPageRouteModelProvider>();
        }

        public int Order => -1000;

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CreateModels(context.RouteModels);
        }

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        private void CreateModels(IList<PageRouteModel> results)
        {
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

            foreach (var viewDescriptor in GetViewDescriptors(_applicationManager))
            {
                if (viewDescriptor.Item != null && !ChecksumValidator.IsItemValid(_templateEngine.Project, viewDescriptor.Item))
                {
                    // If we get here, this compiled Page has different local content, so ignore it.
                    continue;
                }

                PageRouteModel model = null;
                // When RootDirectory and AreaRootDirectory overlap (e.g. RootDirectory = '/', AreaRootDirectory = '/Areas'), we
                // only want to allow a page to be associated with the area route.
                if (_pagesOptions.AllowAreas && viewDescriptor.RelativePath.StartsWith(areaRootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    model = GetAreaPageRouteModel(areaRootDirectory, viewDescriptor);
                }
                else if (viewDescriptor.RelativePath.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    model = GetPageRouteModel(rootDirectory, viewDescriptor);
                }

                if (model != null)
                {
                    results.Add(model);
                }
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
