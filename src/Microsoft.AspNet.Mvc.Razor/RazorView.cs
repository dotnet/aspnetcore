// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
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
        private readonly IRazorPage _page;
        private readonly bool _executeViewHierarchy;

        /// <summary>
        /// Initializes a new instance of RazorView
        /// </summary>
        /// <param name="page">The page to execute</param>
        /// <param name="pageFactory">The view factory used to instantiate additional views.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="executeViewHierarchy">A value that indiciates whether the view hierarchy that involves 
        /// view start and layout pages are executed as part of the executing the page.</param>
        public RazorView([NotNull] IRazorPageFactory pageFactory,
                         [NotNull] IRazorPageActivator pageActivator,
                         [NotNull] IRazorPage page,
                         bool executeViewHierarchy)
        {
            _pageFactory = pageFactory;
            _pageActivator = pageActivator;
            _page = page;
            _executeViewHierarchy = executeViewHierarchy;
        }

        /// <inheritdoc />
        public async Task RenderAsync([NotNull] ViewContext context)
        {
            if (_executeViewHierarchy)
            {
                var bodyContent = await RenderPageAsync(_page, context);
                await RenderLayoutAsync(context, bodyContent);
            }
            else
            {
                await RenderPageCoreAsync(_page, context);
            }
        }

        private async Task<string> RenderPageAsync(IRazorPage page, ViewContext context)
        {
            var contentBuilder = new StringBuilder(1024);
            using (var bodyWriter = new StringWriter(contentBuilder))
            {
                // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
                // and ViewComponents to reference it.
                var oldWriter = context.Writer;
                context.Writer = bodyWriter;
                try
                {
                    await RenderPageCoreAsync(page, context);
                }
                finally
                {
                    context.Writer = oldWriter;
                }
            }

            return contentBuilder.ToString();
        }

        private async Task RenderPageCoreAsync(IRazorPage page, ViewContext context)
        {
            // Activating a page might mutate the ViewContext (for instance ViewContext.ViewData) is mutated by 
            // RazorPageActivator. We'll instead pass in a copy of the ViewContext.
            var pageViewContext = new ViewContext(context, context.View, context.ViewData, context.Writer);
            page.ViewContext = pageViewContext;
            _pageActivator.Activate(page, pageViewContext);

            await page.ExecuteAsync();
        }

        private async Task RenderLayoutAsync(ViewContext context,
                                             string bodyContent)
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
                layoutPage.BodyContent = bodyContent;

                bodyContent = await RenderPageAsync(layoutPage, context);

                // Verify that RenderBody is called, or that RenderSection is called for all sections
                layoutPage.EnsureBodyAndSectionsWereRendered();

                previousPage = layoutPage;
            }

            await context.Writer.WriteAsync(bodyContent);
        }
    }
}