// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly List<IPageApplicationModelProvider> _applicationModelProviders;
        private readonly MvcOptions _mvcOptions;
        private readonly RazorPagesOptions _pagesOptions;

        public PageActionDescriptorProvider(
            IEnumerable<IPageApplicationModelProvider> pageMetadataProviders,
            IOptions<MvcOptions> mvcOptionsAccessor,
            IOptions<RazorPagesOptions> pagesOptionsAccessor)
        {
            _applicationModelProviders = pageMetadataProviders.OrderBy(p => p.Order).ToList();
            _mvcOptions = mvcOptionsAccessor.Value;
            _pagesOptions = pagesOptionsAccessor.Value;
        }

        public int Order { get; set; }

        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            var pageApplicationModels = BuildModel();

            for (var i = 0; i < pageApplicationModels.Count; i++)
            {
                AddActionDescriptors(context.Results, pageApplicationModels[i]);
            }
        }

        protected IList<PageApplicationModel> BuildModel()
        {
            var context = new PageApplicationModelProviderContext();

            for (var i = 0; i < _applicationModelProviders.Count; i++)
            {
                _applicationModelProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _applicationModelProviders.Count - 1; i >= 0; i--)
            {
                _applicationModelProviders[i].OnProvidersExecuted(context);
            }

            return context.Results;
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }

        private void AddActionDescriptors(IList<ActionDescriptor> actions, PageApplicationModel model)
        {
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
                    DisplayName = $"Page: {model.ViewEnginePath}",
                    FilterDescriptors = filters,
                    Properties = new Dictionary<object, object>(model.Properties),
                    RelativePath = model.RelativePath,
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "page", model.ViewEnginePath},
                    },
                    ViewEnginePath = model.ViewEnginePath,
                });
            }
        }
    }
}