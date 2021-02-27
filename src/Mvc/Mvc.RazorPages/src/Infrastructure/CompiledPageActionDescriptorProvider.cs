// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// A <see cref="IActionDescriptorProvider"/> for build-time compiled Razor Pages.
    /// </summary>
    public sealed class CompiledPageActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly PageActionDescriptorProvider _pageActionDescriptorProvider;
        private readonly ApplicationPartManager _applicationPartManager;
        private readonly CompiledPageActionDescriptorFactory _compiledPageActionDescriptorFactory;

        /// <summary>
        /// Initializes a new isntance of <see cref="CompiledPageActionDescriptorProvider"/>.
        /// </summary>
        /// <param name="pageRouteModelProviders">The sequence of <see cref="IPageRouteModelProvider"/>.</param>
        /// <param name="applicationModelProviders">The sequence of <see cref="IPageRouteModelProvider"/>.</param>
        /// <param name="applicationPartManager">The <see cref="ApplicationPartManager"/>.</param>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        /// <param name="pageOptions">Accessor to <see cref="RazorPagesOptions"/>.</param>
        public CompiledPageActionDescriptorProvider(
            IEnumerable<IPageRouteModelProvider> pageRouteModelProviders,
            IEnumerable<IPageApplicationModelProvider> applicationModelProviders,
            ApplicationPartManager applicationPartManager,
            IOptions<MvcOptions> mvcOptions,
            IOptions<RazorPagesOptions> pageOptions)
        {
            _pageActionDescriptorProvider = new PageActionDescriptorProvider(pageRouteModelProviders, mvcOptions, pageOptions);
            _applicationPartManager = applicationPartManager;
            _compiledPageActionDescriptorFactory = new CompiledPageActionDescriptorFactory(applicationModelProviders, mvcOptions.Value, pageOptions.Value);
        }

        /// <inheritdoc/>
        public int Order => _pageActionDescriptorProvider.Order;

        /// <inheritdoc/>
        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            var newContext = new ActionDescriptorProviderContext();
            _pageActionDescriptorProvider.OnProvidersExecuting(newContext);
            _pageActionDescriptorProvider.OnProvidersExecuted(newContext);

            var feature = new ViewsFeature();
            _applicationPartManager.PopulateFeature(feature);

            var lookup = new Dictionary<string, CompiledViewDescriptor>(feature.ViewDescriptors.Count, StringComparer.Ordinal);

            foreach (var viewDescriptor in feature.ViewDescriptors)
            {
                // View ordering has precedence semantics, a view with a higher precedence was not
                // already added to the list.
                lookup.TryAdd(ViewPath.NormalizePath(viewDescriptor.RelativePath), viewDescriptor);
            }

            foreach (var item in newContext.Results)
            {
                var pageActionDescriptor = (PageActionDescriptor)item;
                if (!lookup.TryGetValue(pageActionDescriptor.RelativePath, out var compiledViewDescriptor))
                {
                    throw new InvalidOperationException($"A descriptor for '{pageActionDescriptor.RelativePath}' was not found.");
                }

                var compiledPageActionDescriptor = _compiledPageActionDescriptorFactory.CreateCompiledDescriptor(
                    pageActionDescriptor,
                    compiledViewDescriptor);
                context.Results.Add(compiledPageActionDescriptor);
            }
        }

        /// <inheritdoc/>
        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }
    }
}
