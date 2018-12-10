// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal class CompiledPageRouteModelProvider : IPageRouteModelProvider
    {
        private static readonly string RazorPageDocumentKind = "mvc.1.0.razor-page";
        private static readonly string RouteTemplateKey = "RouteTemplate";
        private readonly RazorPagesOptions _pagesOptions;
        private readonly PageRouteModelFactory _routeModelFactory;

        public CompiledPageRouteModelProvider(
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILogger<CompiledPageRouteModelProvider> logger)
        {
            _pagesOptions = pagesOptionsAccessor?.Value ?? throw new ArgumentNullException(nameof(pagesOptionsAccessor));
            _routeModelFactory = new PageRouteModelFactory(_pagesOptions, logger);
        }

        public int Order => -1000;

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CreateModels(context);
        }

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        private void CreateModels(PageRouteModelProviderContext context)
        {
            var rootDirectory = _pagesOptions.RootDirectory;
            if (!rootDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                rootDirectory = rootDirectory + "/";
            }

            var areaRootDirectory = "/Areas/";
            
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < context.CompiledItems.Count; i++)
            {
                var compiledItem = context.CompiledItems[i];
                var viewDescriptor = new CompiledViewDescriptor(compiledItem, attribute: null);

                var relativePath = viewDescriptor.RelativePath;
                if (!visited.Add(relativePath))
                {
                    // Already seen an descriptor with a higher "order"
                    continue;
                }

                if (!IsRazorPage(viewDescriptor))
                {
                    // Not a page
                    continue;
                }

                var routeTemplate = GetRouteTemplate(viewDescriptor);
                PageRouteModel routeModel = null;

                // When RootDirectory and AreaRootDirectory overlap (e.g. RootDirectory = '/', AreaRootDirectory = '/Areas'), we
                // only want to allow a page to be associated with the area route.
                if (relativePath.StartsWith(areaRootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    routeModel = _routeModelFactory.CreateAreaRouteModel(relativePath, routeTemplate);
                }
                else if (relativePath.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    routeModel = _routeModelFactory.CreateRouteModel(relativePath, routeTemplate);
                }

                if (routeModel != null)
                {
                    context.RouteModels.Add(routeModel);
                }
            }

        }

        internal static string GetRouteTemplate(CompiledViewDescriptor viewDescriptor)
        {
            if (viewDescriptor.Item != null)
            {
                return viewDescriptor.Item.Metadata
                    .OfType<RazorCompiledItemMetadataAttribute>()
                    .FirstOrDefault(f => f.Key == RouteTemplateKey)
                    ?.Value;
            }

            return null;
        }

        private static bool IsRazorPage(CompiledViewDescriptor viewDescriptor)
        {
            if (viewDescriptor.Item != null)
            {
                return viewDescriptor.Item.Kind == RazorPageDocumentKind;
            }

            return false;
        }
    }
}
