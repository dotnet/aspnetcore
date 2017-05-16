// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class CompiledPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private readonly object _cacheLock = new object();
        private readonly ApplicationPartManager _applicationManager;
        private readonly RazorPagesOptions _pagesOptions;
        private List<PageApplicationModel> _cachedApplicationModels;

        public CompiledPageApplicationModelProvider(
            ApplicationPartManager applicationManager,
            IOptions<RazorPagesOptions> pagesOptionsAccessor)
        {
            _applicationManager = applicationManager;
            _pagesOptions = pagesOptionsAccessor.Value;
        }

        public int Order => -1000;

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            EnsureCache();
            for (var i = 0; i < _cachedApplicationModels.Count; i++)
            {
                var pageModel = _cachedApplicationModels[i];
                context.Results.Add(new PageApplicationModel(pageModel));
            }
        }

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        private void EnsureCache()
        {
            lock (_cacheLock)
            {
                if (_cachedApplicationModels != null)
                {
                    return;
                }

                var rootDirectory = _pagesOptions.RootDirectory;
                if (!rootDirectory.EndsWith("/", StringComparison.Ordinal))
                {
                    rootDirectory = rootDirectory + "/";
                }

                var cachedApplicationModels = new List<PageApplicationModel>();
                foreach (var pageDescriptor in GetCompiledPageDescriptors())
                {
                    var pageAttribute = (RazorPageAttribute)pageDescriptor.ViewAttribute;

                    if (!pageDescriptor.RelativePath.StartsWith(rootDirectory))
                    {
                        continue;
                    }

                    var viewEnginePath = GetViewEnginePath(rootDirectory, pageDescriptor.RelativePath);
                    var model = new PageApplicationModel(pageDescriptor.RelativePath, viewEnginePath);
                    PageSelectorModel.PopulateDefaults(model, pageAttribute.RouteTemplate);

                    cachedApplicationModels.Add(model);
                }

                _cachedApplicationModels = cachedApplicationModels;
            }
        }

        protected virtual IEnumerable<CompiledViewDescriptor> GetCompiledPageDescriptors()
            => CompiledPageFeatureProvider.GetCompiledPageDescriptors(_applicationManager.ApplicationParts);

        private string GetViewEnginePath(string rootDirectory, string path)
        {
            var endIndex = path.LastIndexOf('.');
            if (endIndex == -1)
            {
                endIndex = path.Length;
            }

            // rootDirectory = "/Pages/AllMyPages/"
            // path = "/Pages/AllMyPages/Home.cshtml"
            // Result = "/Home"
            var startIndex = rootDirectory.Length - 1;

            return path.Substring(startIndex, endIndex - startIndex);
        }
    }
}
