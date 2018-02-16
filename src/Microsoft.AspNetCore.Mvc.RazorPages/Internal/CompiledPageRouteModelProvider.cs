// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
        private readonly RazorProjectEngine _razorProjectEngine;
        private readonly ILogger<CompiledPageRouteModelProvider> _logger;
        private readonly PageRouteModelFactory _routeModelFactory;

        public CompiledPageRouteModelProvider(
            ApplicationPartManager applicationManager,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            RazorProjectEngine razorProjectEngine,
            ILogger<CompiledPageRouteModelProvider> logger)
        {
            _applicationManager = applicationManager ?? throw new ArgumentNullException(nameof(applicationManager));
            _pagesOptions = pagesOptionsAccessor?.Value ?? throw new ArgumentNullException(nameof(pagesOptionsAccessor));
            _razorProjectEngine = razorProjectEngine ?? throw new ArgumentNullException(nameof(razorProjectEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(razorProjectEngine));
            _routeModelFactory = new PageRouteModelFactory(_pagesOptions, _logger);
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

        private void CreateModels(PageRouteModelProviderContext context)
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
                if (viewDescriptor.Item != null && !ChecksumValidator.IsItemValid(_razorProjectEngine.FileSystem, viewDescriptor.Item))
                {
                    // If we get here, this compiled Page has different local content, so ignore it.
                    continue;
                }

                var pageAttribute = (RazorPageAttribute)viewDescriptor.ViewAttribute;
                PageRouteModel routeModel = null;

                // When RootDirectory and AreaRootDirectory overlap (e.g. RootDirectory = '/', AreaRootDirectory = '/Areas'), we
                // only want to allow a page to be associated with the area route.
                if (_pagesOptions.AllowAreas && viewDescriptor.RelativePath.StartsWith(areaRootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    routeModel = _routeModelFactory.CreateAreaRouteModel(viewDescriptor.RelativePath, pageAttribute.RouteTemplate);
                }
                else if (viewDescriptor.RelativePath.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    routeModel = _routeModelFactory.CreateRouteModel(pageAttribute.Path, pageAttribute.RouteTemplate);
                }

                if (routeModel != null)
                {
                    context.RouteModels.Add(routeModel);
                }
            }
        }
    }
}
