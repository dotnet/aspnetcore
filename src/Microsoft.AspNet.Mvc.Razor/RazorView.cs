// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation for <see cref="IView"/> that executes one or more <see cref="IRazorPage"/>
    /// as parts of its exeuction.
    /// </summary>
    public class RazorView : IView
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IRazorPageActivator _pageActivator;
        private readonly IViewStartProvider _viewStartProvider;
        private IPageExecutionListenerFeature _pageExecutionFeature;

        /// <summary>
        /// Initializes a new instance of <see cref="RazorView"/>
        /// </summary>
        /// <param name="viewEngine">The <see cref="IRazorViewEngine"/> used to locate Layout pages.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="viewStartProvider">The <see cref="IViewStartProvider"/> used for discovery of _ViewStart
        /// <param name="razorPage">The <see cref="IRazorPage"/> instance to execute.</param>
        /// <param name="isPartial">Determines if the view is to be executed as a partial.</param>
        /// pages</param>
        public RazorView(IRazorViewEngine viewEngine,
                         IRazorPageActivator pageActivator,
                         IViewStartProvider viewStartProvider,
                         IRazorPage razorPage,
                         bool isPartial)
        {
            _viewEngine = viewEngine;
            _pageActivator = pageActivator;
            _viewStartProvider = viewStartProvider;
            RazorPage = razorPage;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Gets <see cref="IRazorPage"/> instance that the views executes on.
        /// </summary>
        public IRazorPage RazorPage { get; }

        /// <summary>
        /// Gets a value that determines if the view is executed as a partial.
        /// </summary>
        public bool IsPartial { get; }

        private bool EnableInstrumentation
        {
            get { return _pageExecutionFeature != null; }
        }

        /// <inheritdoc />
        public virtual async Task RenderAsync([NotNull] ViewContext context)
        {
            _pageExecutionFeature = context.HttpContext.GetFeature<IPageExecutionListenerFeature>();

            if (IsPartial)
            {
                await RenderPartialAsync(context);
            }
            else
            {
                var bodyWriter = await RenderPageAsync(RazorPage, context, executeViewStart: true);
                await RenderLayoutAsync(context, bodyWriter);
            }
        }

        private async Task RenderPartialAsync(ViewContext context)
        {
            if (EnableInstrumentation)
            {
                // When instrmenting, we need to Decorate the output in an instrumented writer which
                // RenderPageAsync does.
                var bodyWriter = await RenderPageAsync(RazorPage, context, executeViewStart: false);
                await bodyWriter.CopyToAsync(context.Writer);
            }
            else
            {
                // For the non-instrumented case, we don't need to buffer contents. For Html.Partial, the writer is
                // an in memory writer and for Partial views, we directly write to the Response.
                await RenderPageCoreAsync(RazorPage, context);
            }
        }

        private async Task<IBufferedTextWriter> RenderPageAsync(IRazorPage page,
                                                                ViewContext context,
                                                                bool executeViewStart)
        {
            var razorTextWriter = new RazorTextWriter(context.Writer, context.Writer.Encoding);
            var writer = (TextWriter)razorTextWriter;
            var bufferedWriter = (IBufferedTextWriter)razorTextWriter;

            if (EnableInstrumentation)
            {
                writer = _pageExecutionFeature.DecorateWriter(razorTextWriter);
                bufferedWriter = writer as IBufferedTextWriter;
                if (bufferedWriter == null)
                {
                    var message = Resources.FormatInstrumentation_WriterMustBeBufferedTextWriter(
                        nameof(TextWriter),
                        _pageExecutionFeature.GetType().FullName,
                        typeof(IBufferedTextWriter).FullName);
                    throw new InvalidOperationException(message);
                }
            }

            // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
            // and ViewComponents to reference it.
            var oldWriter = context.Writer;
            context.Writer = writer;

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
                writer.Dispose();
            }
        }

        private async Task RenderPageCoreAsync(IRazorPage page, ViewContext context)
        {
            page.IsPartial = IsPartial;
            page.ViewContext = context;
            if (EnableInstrumentation)
            {
                page.PageExecutionContext = _pageExecutionFeature.GetContext(page.Path, context.Writer);
            }

            _pageActivator.Activate(page, context);
            await page.ExecuteAsync();
        }

        private async Task RenderViewStartAsync(ViewContext context)
        {
            var viewStarts = _viewStartProvider.GetViewStartPages(RazorPage.Path);

            string layout = null;
            foreach (var viewStart in viewStarts)
            {
                // Copy the layout value from the previous view start (if any) to the current.
                viewStart.Layout = layout;
                await RenderPageCoreAsync(viewStart, context);
                layout = viewStart.Layout;
            }

            // Copy over interesting properties from the ViewStart page to the entry page.
            RazorPage.Layout = layout;
        }

        private async Task RenderLayoutAsync(ViewContext context,
                                             IBufferedTextWriter bodyWriter)
        {
            // A layout page can specify another layout page. We'll need to continue
            // looking for layout pages until they're no longer specified.
            var previousPage = RazorPage;
            while (!string.IsNullOrEmpty(previousPage.Layout))
            {
                if (!bodyWriter.IsBuffering)
                {
                    // Once a call to RazorPage.FlushAsync is made, we can no longer render Layout pages - content has
                    // already been written to the client and the layout content would be appended rather than surround
                    // the body content. Throwing this exception wouldn't return a 500 (since content has already been
                    // written), but a diagnostic component should be able to capture it.

                    var message = Resources.FormatLayoutCannotBeRendered("FlushAsync");
                    throw new InvalidOperationException(message);
                }

                var layoutPage = GetLayoutPage(context, previousPage.Layout);

                // Notify the previous page that any writes that are performed on it are part of sections being written
                // in the layout.
                previousPage.IsLayoutBeingRendered = true;
                layoutPage.PreviousSectionWriters = previousPage.SectionWriters;
                layoutPage.RenderBodyDelegate = bodyWriter.CopyTo;
                bodyWriter = await RenderPageAsync(layoutPage, context, executeViewStart: false);

                // Verify that RenderBody is called, or that RenderSection is called for all sections
                layoutPage.EnsureBodyAndSectionsWereRendered();

                previousPage = layoutPage;
            }

            if (bodyWriter.IsBuffering)
            {
                // Only copy buffered content to the Output if we're currently buffering.
                await bodyWriter.CopyToAsync(context.Writer);
            }
        }

        private IRazorPage GetLayoutPage(ViewContext context, string layoutPath)
        {
            var layoutPageResult = _viewEngine.FindPage(context, layoutPath);
            if (layoutPageResult.Page == null)
            {
                var locations = Environment.NewLine +
                                string.Join(Environment.NewLine, layoutPageResult.SearchedLocations);
                throw new InvalidOperationException(Resources.FormatLayoutCannotBeLocated(layoutPath, locations));
            }

            var layoutPage = layoutPageResult.Page;
            return layoutPage;
        }
    }
}
