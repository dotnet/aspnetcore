// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    // This class copies many aspects of PageActionDescriptorProvider because PageActionDescriptorProvider
    // is public, and can't really change.
    internal class PageRouteModelFactory
    {
        private readonly IPageRouteModelProvider[] _providers;
        private readonly IPageRouteModelConvention[] _conventions;

        public PageRouteModelFactory(IEnumerable<IPageRouteModelProvider> providers, IOptions<RazorPagesOptions> options)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _providers = providers.OrderBy(p => p.Order).ToArray();

            _conventions = options.Value.Conventions
                .OfType<IPageRouteModelConvention>()
                .ToArray();
        }

        public IList<PageRouteModel> CreateModel(IEnumerable<RazorCompiledItem> compiledItems)
        {
            if (compiledItems == null)
            {
                throw new ArgumentNullException(nameof(compiledItems));
            }

            var context = new PageRouteModelProviderContext(compiledItems);

            var providers = _providers;
            for (var i = 0; i < providers.Length; i++)
            {
                providers[i].OnProvidersExecuting(context);
            }

            for (var i = providers.Length - 1; i >= 0; i--)
            {
                providers[i].OnProvidersExecuted(context);
            }

            var conventions = _conventions;
            for (var i = 0; i < context.RouteModels.Count; i++)
            {
                var model = context.RouteModels[i];
                for (var j = 0; j < conventions.Length; j++)
                {
                    conventions[j].Apply(model);
                }
            }

            return context.RouteModels;
        }
    }
}