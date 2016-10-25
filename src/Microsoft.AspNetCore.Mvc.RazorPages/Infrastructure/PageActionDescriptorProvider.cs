// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptorProvider : IActionDescriptorProvider
    {
        private static readonly string IndexFileName = "Index.cshtml";
        private readonly RazorProject _project;
        private readonly MvcOptions _mvcOptions;
        private readonly RazorPagesOptions _pagesOptions;

        public PageActionDescriptorProvider(
            RazorProject project,
            IOptions<MvcOptions> mvcOptionsAccessor,
            IOptions<RazorPagesOptions> pagesOptionsAccessor)
        {
            _project = project;
            _mvcOptions = mvcOptionsAccessor.Value;
            _pagesOptions = pagesOptionsAccessor.Value;
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            foreach (var item in _project.EnumerateItems("/"))
            {
                if (item.Filename.StartsWith("_"))
                {
                    // Pages like _PageImports should not be routable.
                    continue;
                }

                string template;
                if (!PageDirectiveFeature.TryGetRouteTemplate(item, out template))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (AttributeRouteModel.IsOverridePattern(template))
                {
                    throw new InvalidOperationException(string.Format(
                        Resources.PageActionDescriptorProvider_RouteTemplateCannotBeOverrideable,
                        item.Path));
                }

                AddActionDescriptors(context.Results, item, template);
            }
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }

        private void AddActionDescriptors(IList<ActionDescriptor> actions, RazorProjectItem item, string template)
        {
            var model = new PageModel(item.CombinedPath, item.PathWithoutExtension);
            var routePrefix = item.BasePath == "/" ? item.PathWithoutExtension : item.BasePath + item.PathWithoutExtension;
            model.Selectors.Add(CreateSelectorModel(routePrefix, template));

            if (string.Equals(IndexFileName, item.Filename, StringComparison.OrdinalIgnoreCase))
            {
                model.Selectors.Add(CreateSelectorModel(item.BasePath, template));
            }

            for (var i = 0; i < _pagesOptions.Conventions.Count; i++)
            {
                _pagesOptions.Conventions[i].Apply(model);
            }

            var filters = new List<FilterDescriptor>(_mvcOptions.Filters.Count + model.Filters.Count);
            for (var i = 0; i < _mvcOptions.Filters.Count; i++)
            {
                filters.Add(new FilterDescriptor(_mvcOptions.Filters[i], FilterScope.Global));
            }

            for (var i = 0; i < model.Filters.Count; i++)
            {
                filters.Add(new FilterDescriptor(model.Filters[i], FilterScope.Action));
            }

            foreach (var selector in model.Selectors)
            {
                actions.Add(new PageActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Name = selector.AttributeRouteModel.Name,
                        Order = selector.AttributeRouteModel.Order ?? 0,
                        Template = selector.AttributeRouteModel.Template,
                    },
                    DisplayName = $"Page: {item.Path}",
                    FilterDescriptors = filters,
                    Properties = new Dictionary<object, object>(model.Properties),
                    RelativePath = item.CombinedPath,
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "page", item.PathWithoutExtension },
                    },
                    ViewEnginePath = item.Path,
                });
            }
        }

        private static SelectorModel CreateSelectorModel(string prefix, string template)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = AttributeRouteModel.CombineTemplates(prefix, template),
                }
            };
        }
    }
}