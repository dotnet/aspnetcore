// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
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
        private readonly DiagnosticListener _diagnosticListener;
        private IViewBufferScope _bufferScope;

        /// <summary>
        /// Initializes a new instance of <see cref="RazorView"/>
        /// </summary>
        /// <param name="viewEngine">The <see cref="IRazorViewEngine"/> used to locate Layout pages.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="viewStartPages">The sequence of <see cref="IRazorPage" /> instances executed as _ViewStarts.
        /// </param>
        /// <param name="razorPage">The <see cref="IRazorPage"/> instance to execute.</param>
        /// <param name="htmlEncoder">The HTML encoder.</param>
        /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
        public RazorView(
            IRazorViewEngine viewEngine,
            IRazorPageActivator pageActivator,
            IReadOnlyList<IRazorPage> viewStartPages,
            IRazorPage razorPage,
            HtmlEncoder htmlEncoder,
            DiagnosticListener diagnosticListener)
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

            if (diagnosticListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticListener));
            }

            _viewEngine = viewEngine;
            _pageActivator = pageActivator;
            ViewStartPages = viewStartPages;
            RazorPage = razorPage;
            _htmlEncoder = htmlEncoder;
            _diagnosticListener = diagnosticListener;
        }

        /// <inheritdoc />
        public string Path => RazorPage.Path;

        /// <summary>
        /// Gets <see cref="IRazorPage"/> instance that the views executes on.
        /// </summary>
        public IRazorPage RazorPage { get; }

        /// <summary>
        /// Gets the sequence of _ViewStart <see cref="IRazorPage"/> instances that are executed by this view.
        /// </summary>
        public IReadOnlyList<IRazorPage> ViewStartPages { get; }

        internal Action<IRazorPage, ViewContext> OnAfterPageActivated { get; set; }

        /// <inheritdoc />
        public virtual async Task RenderAsync(ViewContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // This GetRequiredService call is by design. ViewBufferScope is a scoped service, RazorViewEngine
            // is the component responsible for creating RazorViews and it is a Singleton service. It doesn't
            // have access to the RequestServices so requiring the service when we render the page is the best
            // we can do.
            _bufferScope = context.HttpContext.RequestServices.GetRequiredService<IViewBufferScope>();
            var bodyWriter = await RenderPageAsync(RazorPage, context, invokeViewStarts: true);
            await RenderLayoutAsync(context, bodyWriter);
        }

        private async Task<ViewBufferTextWriter> RenderPageAsync(
            IRazorPage page,
            ViewContext context,
            bool invokeViewStarts)
        {
            var writer = context.Writer as ViewBufferTextWriter;
            if (writer == null)
            {
                Debug.Assert(_bufferScope != null);

                // If we get here, this is likely the top-level page (not a partial) - this means
                // that context.Writer is wrapping the output stream. We need to buffer, so create a buffered writer.
                var buffer = new ViewBuffer(_bufferScope, page.Path, ViewBuffer.ViewPageSize);
                writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding, _htmlEncoder, context.Writer);
            }
            else
            {
                // This means we're writing something like a partial, where the output needs to be buffered.
                // Create a new buffer, but without the ability to flush.
                var buffer = new ViewBuffer(_bufferScope, page.Path, ViewBuffer.ViewPageSize);
                writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding);
            }

            // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
            // and ViewComponents to reference it.
            var oldWriter = context.Writer;
            var oldFilePath = context.ExecutingFilePath;

            context.Writer = writer;
            context.ExecutingFilePath = page.Path;

            try
            {
                if (invokeViewStarts)
                {
                    // Execute view starts using the same context + writer as the page to render.
                    await RenderViewStartsAsync(context);
                }

                await RenderPageCoreAsync(page, context);
                return writer;
            }
            finally
            {
                context.Writer = oldWriter;
                context.ExecutingFilePath = oldFilePath;
            }
        }

        private async Task RenderPageCoreAsync(IRazorPage page, ViewContext context)
        {
            page.ViewContext = context;
            _pageActivator.Activate(page, context);

            OnAfterPageActivated?.Invoke(page, context);

            _diagnosticListener.BeforeViewPage(page, context);

            try
            {
                await page.ExecuteAsync();
            }
            finally
            {
                _diagnosticListener.AfterViewPage(page, context);
            }
        }

        private async Task RenderViewStartsAsync(ViewContext context)
        {
            string layout = null;
            var oldFilePath = context.ExecutingFilePath;
            try
            {
                for (var i = 0; i < ViewStartPages.Count; i++)
                {
                    var viewStart = ViewStartPages[i];
                    context.ExecutingFilePath = viewStart.Path;

                    // If non-null, copy the layout value from the previous view start to the current. Otherwise leave
                    // Layout default alone.
                    if (layout != null)
                    {
                        viewStart.Layout = layout;
                    }

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

            // If non-null, copy the layout value from the view start page(s) to the entry page.
            if (layout != null)
            {
                RazorPage.Layout = layout;
            }
        }

        private async Task RenderLayoutAsync(
            ViewContext context,
            ViewBufferTextWriter bodyWriter)
        {
            // A layout page can specify another layout page. We'll need to continue
            // looking for layout pages until they're no longer specified.
            var previousPage = RazorPage;
            var renderedLayouts = new List<IRazorPage>();

            // This loop will execute Layout pages from the inside to the outside. With each
            // iteration, bodyWriter is replaced with the aggregate of all the "body" content
            // (including the layout page we just rendered).
            while (!string.IsNullOrEmpty(previousPage.Layout))
            {
                if (bodyWriter.Flushed)
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
                layoutPage.BodyContent = bodyWriter.Buffer;
                bodyWriter = await RenderPageAsync(layoutPage, context, invokeViewStarts: false);

                renderedLayouts.Add(layoutPage);
                previousPage = layoutPage;
            }

            // Now we've reached and rendered the outer-most layout page. Nothing left to execute.

            // Ensure all defined sections were rendered or RenderBody was invoked for page without defined sections.
            foreach (var layoutPage in renderedLayouts)
            {
                layoutPage.EnsureRenderedBodyOrSections();
            }

            // We've got a bunch of content in the view buffer. How to best deal with it
            // really depends on whether or not we're writing directly to the output or if we're writing to
            // another buffer.
            if (context.Writer is ViewBufferTextWriter viewBufferTextWriter)
            {
                // This means we're writing to another buffer. Use MoveTo to combine them.
                bodyWriter.Buffer.MoveTo(viewBufferTextWriter.Buffer);
            }
            else
            {
                // This means we're writing to a 'real' writer, probably to the actual output stream.
                // We're using PagedBufferedTextWriter here to 'smooth' synchronous writes of IHtmlContent values.
                await using (var writer = _bufferScope.CreateWriter(context.Writer))
                {
                    await bodyWriter.Buffer.WriteToAsync(writer, _htmlEncoder);
                    await writer.FlushAsync();
                }
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
