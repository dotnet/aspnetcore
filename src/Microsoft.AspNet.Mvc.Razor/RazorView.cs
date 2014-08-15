// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IView"/> that executes one or more <see cref="RazorPage"/> instances as part of
    /// view rendering.
    /// </summary>
    public class RazorView : IView
    {
        private readonly IRazorPageFactory _pageFactory;
        private readonly IRazorPageActivator _pageActivator;
        private readonly IViewStartProvider _viewStartProvider;
        private readonly IRazorPage _page;

        /// <summary>
        /// Initializes a new instance of RazorView
        /// </summary>
        /// <param name="page">The page to execute</param>
        /// <param name="pageFactory">The view factory used to instantiate layout and _ViewStart pages.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        public RazorView([NotNull] IRazorPageFactory pageFactory,
                         [NotNull] IRazorPageActivator pageActivator,
                         [NotNull] IViewStartProvider viewStartProvider,
                         [NotNull] IRazorPage page)
        {
            _pageFactory = pageFactory;
            _pageActivator = pageActivator;
            _viewStartProvider = viewStartProvider;
            _page = page;
        }

        /// <summary>
        /// Gets or sets a value that determines if the view hierarchy is executed as part of
        /// executing the <see cref="IRazorPage"/> instance. The view hierarchy involves _ViewStart 
        /// and Layout pages.
        /// </summary>
        public bool ExecuteViewHierarchy { get; set; }

        /// <inheritdoc />
        public virtual async Task RenderAsync([NotNull] ViewContext context)
        {
            if (ExecuteViewHierarchy)
            {
                var bodyWriter = await RenderPageAsync(_page, context, executeViewStart: true);
                await RenderLayoutAsync(context, bodyWriter);
            }
            else
            {
                await RenderPageCoreAsync(_page, context);
            }
        }

        private async Task<RazorTextWriter> RenderPageAsync(IRazorPage page,
                                                            ViewContext context,
                                                            bool executeViewStart)
        {
            using (var bufferedWriter = new RazorTextWriter(context.Writer.Encoding))
            {
                // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
                // and ViewComponents to reference it.
                var oldWriter = context.Writer;
                context.Writer = bufferedWriter;
                try
                {
                    if (executeViewStart)
                    {
                        // Execute view starts using the same context + writer as the page to render.
                        await RenderViewStartAsync(context);
                    }

                    await RenderPageCoreAsync(page, context);
                    return bufferedWriter;
                }
                finally
                {
                    context.Writer = oldWriter;
                }
            }
        }

        private async Task RenderPageCoreAsync(IRazorPage page, ViewContext context)
        {
            page.ViewContext = context;
            _pageActivator.Activate(page, context);
            await page.ExecuteAsync();
        }

        private async Task RenderViewStartAsync(ViewContext context)
        {
            var viewStarts = _viewStartProvider.GetViewStartPages(_page.Path);

            foreach (var viewStart in viewStarts)
            {
                await RenderPageCoreAsync(viewStart, context);

                // Copy over interesting properties from the ViewStart page to the entry page.
                _page.Layout = viewStart.Layout;
            }
        }

        private async Task RenderLayoutAsync(ViewContext context,
                                             RazorTextWriter bodyWriter)
        {
            // A layout page can specify another layout page. We'll need to continue
            // looking for layout pages until they're no longer specified.
            var previousPage = _page;
            while (!string.IsNullOrEmpty(previousPage.Layout))
            {
                var layoutPage = _pageFactory.CreateInstance(previousPage.Layout);
                if (layoutPage == null)
                {
                    var message = Resources.FormatLayoutCannotBeLocated(previousPage.Layout);
                    throw new InvalidOperationException(message);
                }

                layoutPage.PreviousSectionWriters = previousPage.SectionWriters;
                layoutPage.RenderBodyDelegate = bodyWriter.CopyTo;
                bodyWriter = await RenderPageAsync(layoutPage, context, executeViewStart: false);

                // Verify that RenderBody is called, or that RenderSection is called for all sections
                layoutPage.EnsureBodyAndSectionsWereRendered();

                previousPage = layoutPage;
            }

            await bodyWriter.CopyToAsync(context.Writer);
        }
    }
}
