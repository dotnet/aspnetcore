// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation for <see cref="IView"/> that executes one or more <see cref="IRazorPage"/>
    /// as parts of its execution.
    /// </summary>
    public class RazorView : IView
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IRazorPageActivator _pageActivator;
        private readonly HtmlEncoder _htmlEncoder;
        private IPageExecutionListenerFeature _pageExecutionFeature;

        /// <summary>
        /// Initializes a new instance of <see cref="RazorView"/>
        /// </summary>
        /// <param name="viewEngine">The <see cref="IRazorViewEngine"/> used to locate Layout pages.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="viewStartPages">The sequence of <see cref="IRazorPage" /> instances executed as _ViewStarts.
        /// </param>
        /// <param name="razorPage">The <see cref="IRazorPage"/> instance to execute.</param>
        /// <param name="htmlEncoder">The HTML encoder.</param>
        public RazorView(
            IRazorViewEngine viewEngine,
            IRazorPageActivator pageActivator,
            IReadOnlyList<IRazorPage> viewStartPages,
            IRazorPage razorPage,
            HtmlEncoder htmlEncoder)
        {
            if (viewEngine == null)
            {
                throw new ArgumentNullException(nameof(viewEngine));
            }

            if (pageActivator == null)
            {
                throw new ArgumentNullException(nameof(pageActivator));
            }

            if (viewStartPages == null)
            {
                throw new ArgumentNullException(nameof(viewStartPages));
            }

            if (razorPage == null)
            {
                throw new ArgumentNullException(nameof(razorPage));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            _viewEngine = viewEngine;
            _pageActivator = pageActivator;
            ViewStartPages = viewStartPages;
            RazorPage = razorPage;
            _htmlEncoder = htmlEncoder;
        }

        /// <inheritdoc />
        public string Path
        {
            get { return RazorPage.Path; }
        }

        /// <summary>
        /// Gets <see cref="IRazorPage"/> instance that the views executes on.
        /// </summary>
        public IRazorPage RazorPage { get; }

        /// <summary>
        /// Gets the sequence of _ViewStart <see cref="IRazorPage"/> instances that are executed by this view.
        /// </summary>
        public IReadOnlyList<IRazorPage> ViewStartPages { get; }

        private bool EnableInstrumentation
        {
            get { return _pageExecutionFeature != null; }
        }

        /// <inheritdoc />
        public virtual async Task RenderAsync(ViewContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _pageExecutionFeature = context.HttpContext.Features.Get<IPageExecutionListenerFeature>();

            var bodyWriter = await RenderPageAsync(RazorPage, context, ViewStartPages);
            await RenderLayoutAsync(context, bodyWriter);
        }

        private async Task<IBufferedTextWriter> RenderPageAsync(
            IRazorPage page,
            ViewContext context,
            IReadOnlyList<IRazorPage> viewStartPages)
        {
            var razorTextWriter = new RazorTextWriter(context.Writer, context.Writer.Encoding, _htmlEncoder);
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
            var oldFilePath = context.ExecutingFilePath;
            context.Writer = writer;
            context.ExecutingFilePath = page.Path;

            try
            {
                if (viewStartPages != null)
                {
                    // Execute view starts using the same context + writer as the page to render.
                    await RenderViewStartsAsync(context, viewStartPages);
                }

                await RenderPageCoreAsync(page, context);
                return bufferedWriter;
            }
            finally
            {
                context.Writer = oldWriter;
                context.ExecutingFilePath = oldFilePath;
                writer.Dispose();
            }
        }

        private Task RenderPageCoreAsync(IRazorPage page, ViewContext context)
        {
            page.ViewContext = context;
            if (EnableInstrumentation)
            {
                page.PageExecutionContext = _pageExecutionFeature.GetContext(page.Path, context.Writer);
            }

            _pageActivator.Activate(page, context);
            return page.ExecuteAsync();
        }

        private async Task RenderViewStartsAsync(ViewContext context, IReadOnlyList<IRazorPage> viewStartPages)
        {
            string layout = null;
            var oldFilePath = context.ExecutingFilePath;
            try
            {
                for (var i = 0; i < viewStartPages.Count; i++)
                {
                    var viewStart = ViewStartPages[i];
                    context.ExecutingFilePath = viewStart.Path;

                    // Copy the layout value from the previous view start (if any) to the current.
                    viewStart.Layout = layout;

                    await RenderPageCoreAsync(viewStart, context);

                    // Pass correct absolute path to next layout or the entry page if this view start set Layout to a
                    // relative path.
                    layout = _viewEngine.GetAbsolutePath(viewStart.Path, viewStart.Layout);
                }
            }
            finally
            {
                context.ExecutingFilePath = oldFilePath;
            }

            // Copy the layout value from the view start page(s) (if any) to the entry page.
            RazorPage.Layout = layout;
        }

        private async Task RenderLayoutAsync(
            ViewContext context,
            IBufferedTextWriter bodyWriter)
        {
            // A layout page can specify another layout page. We'll need to continue
            // looking for layout pages until they're no longer specified.
            var previousPage = RazorPage;
            var renderedLayouts = new List<IRazorPage>();
            while (!string.IsNullOrEmpty(previousPage.Layout))
            {
                if (!bodyWriter.IsBuffering)
                {
                    // Once a call to RazorPage.FlushAsync is made, we can no longer render Layout pages - content has
                    // already been written to the client and the layout content would be appended rather than surround
                    // the body content. Throwing this exception wouldn't return a 500 (since content has already been
                    // written), but a diagnostic component should be able to capture it.

                    var message = Resources.FormatLayoutCannotBeRendered(Path, nameof(Razor.RazorPage.FlushAsync));
                    throw new InvalidOperationException(message);
                }

                var layoutPage = GetLayoutPage(context, previousPage.Path, previousPage.Layout);

                if (renderedLayouts.Count > 0 &&
                    renderedLayouts.Any(l => string.Equals(l.Path, layoutPage.Path, StringComparison.Ordinal)))
                {
                    // If the layout has been previously rendered as part of this view, we're potentially in a layout
                    // rendering cycle.
                    throw new InvalidOperationException(
                        Resources.FormatLayoutHasCircularReference(previousPage.Path, layoutPage.Path));
                }

                // Notify the previous page that any writes that are performed on it are part of sections being written
                // in the layout.
                previousPage.IsLayoutBeingRendered = true;
                layoutPage.PreviousSectionWriters = previousPage.SectionWriters;
                layoutPage.RenderBodyDelegateAsync = bodyWriter.CopyToAsync;
                bodyWriter = await RenderPageAsync(layoutPage, context, viewStartPages: null);

                renderedLayouts.Add(layoutPage);
                previousPage = layoutPage;
            }

            // Ensure all defined sections were rendered or RenderBody was invoked for page without defined sections.
            foreach (var layoutPage in renderedLayouts)
            {
                layoutPage.EnsureRenderedBodyOrSections();
            }

            if (bodyWriter.IsBuffering)
            {
                // Only copy buffered content to the Output if we're currently buffering.
                await bodyWriter.CopyToAsync(context.Writer);
            }
        }

        private IRazorPage GetLayoutPage(ViewContext context, string executingFilePath, string layoutPath)
        {
            var layoutPageResult = _viewEngine.GetPage(executingFilePath, layoutPath);
            var originalLocations = layoutPageResult.SearchedLocations;
            if (layoutPageResult.Page == null)
            {
                layoutPageResult = _viewEngine.FindPage(context, layoutPath);
            }

            if (layoutPageResult.Page == null)
            {
                var locations = string.Empty;
                if (originalLocations.Any())
                {
                    locations = Environment.NewLine + string.Join(Environment.NewLine, originalLocations);
                }

                if (layoutPageResult.SearchedLocations.Any())
                {
                    locations +=
                        Environment.NewLine + string.Join(Environment.NewLine, layoutPageResult.SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatLayoutCannotBeLocated(layoutPath, locations));
            }

            var layoutPage = layoutPageResult.Page;
            return layoutPage;
        }
    }
}
