
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal class RazorViewTemplatingSystem : IViewTemplatingSystem
    {
        private readonly RazorViewLookup _viewLookup;
        private readonly IRazorPageActivator _pageActivator;
        
        private readonly HtmlEncoder _htmlEncoder;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly IReadOnlyList<IRazorPage> _viewStarts;
        private IViewBufferScope _bufferScope;

        public RazorViewTemplatingSystem(
            RazorViewLookup viewLookup,
            IRazorPageActivator razorPageActivator,
            HtmlEncoder htmlEncoder,
            DiagnosticListener diagnosticListener,
            IRazorPage razorPage,
            IReadOnlyList<IRazorPage> viewStarts)
        {
            _viewLookup = viewLookup;
            _pageActivator = razorPageActivator;
            _htmlEncoder = htmlEncoder;
            _diagnosticListener = diagnosticListener;

            Page = razorPage;
            _viewStarts = viewStarts;
        }

        internal Action<IRazorPage, ViewContext> OnAfterPageActivated { get; set; }

        private IRazorPage Page { get; }

        private string Path => Page.Path;

        public async Task InvokeAsync(ViewContext context)
        {
            _bufferScope = context.HttpContext.RequestServices.GetRequiredService<IViewBufferScope>();

            if (context.Writer is ViewBufferTextWriter writer)
            {
                // This means we're writing something like a partial, where the output needs to be buffered.
                // Create a new buffer, but without the ability to flush.
                var buffer = new ViewBuffer(_bufferScope, Path, ViewBuffer.ViewPageSize);
                writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding);
            }
            else
            {
                // If we get here, this is likely the top-level page (not a partial) - this means
                // that context.Writer is wrapping the output stream. We need to buffer, so create a buffered writer.
                var buffer = new ViewBuffer(_bufferScope, Path, ViewBuffer.ViewPageSize);
                writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding, _htmlEncoder, context.Writer);
            }

            var oldView = context.View;
            context.View = new View { Path = Path };
            try
            {
                await RenderViewStartsAsync(context, writer);
                await RenderPageCoreAsync(context, Page, writer);
                await RenderLayoutAsync(context, writer);
            }
            finally
            {
                context.View = oldView;
            }
        }

        private async Task RenderViewStartsAsync(ViewContext context, ViewBufferTextWriter writer)
        {
            string layout = null;
            var oldFilePath = context.ExecutingFilePath;
            try
            {
                for (var i = 0; i < _viewStarts.Count; i++)
                {
                    var viewStart = _viewStarts[i];
                    context.ExecutingFilePath = viewStart.Path;

                    // If non-null, copy the layout value from the previous view start to the current. Otherwise leave
                    // Layout default alone.
                    if (layout != null)
                    {
                        viewStart.Layout = layout;
                    }

                    await RenderPageCoreAsync(context, viewStart, writer);

                    // Pass correct absolute path to next layout or the entry page if this view start set Layout to a
                    // relative path.
                    layout = viewStart.Layout;
                    if (!string.IsNullOrEmpty(layout) && layout.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                    {
                        layout = ViewEnginePath.CombinePath(viewStart.Path, viewStart.Layout);
                    }
                }
            }
            finally
            {
                context.ExecutingFilePath = oldFilePath;
            }

            // If non-null, copy the layout value from the view start page(s) to the entry page.
            if (layout != null)
            {
                Page.Layout = layout;
            }
        }

        private async Task RenderPageCoreAsync(ViewContext context, IRazorPage page, ViewBufferTextWriter viewBufferTextWriter)
        {
            page.ViewContext = context;
            _pageActivator.Activate(page, context);

            OnAfterPageActivated?.Invoke(page, context);

            _diagnosticListener.BeforeViewPage(page, context);

            var oldWriter = context.Writer;
            var oldPath = context.ExecutingFilePath;

            // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
            // and ViewComponents to reference it.
            context.Writer = viewBufferTextWriter;
            try
            {
                context.ExecutingFilePath = page.Path;
                await page.ExecuteAsync();
            }
            finally
            {
                _diagnosticListener.AfterViewPage(page, context);
                context.ExecutingFilePath = oldPath;
                context.Writer = oldWriter;
            }
        }

        private async Task RenderLayoutAsync(ViewContext context, ViewBufferTextWriter bodyWriter)
        {
            // A layout page can specify another layout page. We'll need to continue
            // looking for layout pages until they're no longer specified.
            var previousPage = Page;
            var renderedLayouts = new List<IRazorPage>();

            // This loop will execute Layout pages from the inside to the outside. With each
            // iteration, bodyWriter is replaced with the aggregate of all the "body" content
            // (including the layout page we just rendered).
            while (!string.IsNullOrEmpty(previousPage.Layout))
            {
                if (!bodyWriter.IsBuffering)
                {
                    // Once a call to RazorPage.FlushAsync is made, we can no longer render Layout pages - content has
                    // already been written to the client and the layout content would be appended rather than surround
                    // the body content. Throwing this exception wouldn't return a 500 (since content has already been
                    // written), but a diagnostic component should be able to capture it.

                    var message = Resources.FormatLayoutCannotBeRendered(Page.Path, nameof(RazorPage.FlushAsync));
                    throw new InvalidOperationException(message);
                }

                var layoutPage = await GetLayoutPage(context, previousPage.Path, previousPage.Layout);

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

                var buffer = new ViewBuffer(_bufferScope, Page.Path, ViewBuffer.ViewPageSize);
                bodyWriter = new ViewBufferTextWriter(buffer, context.Writer.Encoding);

                await RenderPageCoreAsync(context, layoutPage, bodyWriter);

                renderedLayouts.Add(layoutPage);
                previousPage = layoutPage;
            }

            // Now we've reached and rendered the outer-most layout page. Nothing left to execute.

            // Ensure all defined sections were rendered or RenderBody was invoked for page without defined sections.
            foreach (var layoutPage in renderedLayouts)
            {
                layoutPage.EnsureRenderedBodyOrSections();
            }

            if (bodyWriter.IsBuffering)
            {
                // If IsBuffering - then we've got a bunch of content in the view buffer. How to best deal with it
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
                    using (var writer = _bufferScope.CreateWriter(context.Writer))
                    {
                        await bodyWriter.Buffer.WriteToAsync(writer, _htmlEncoder);
                    }
                }
            }
        }

        private async ValueTask<IRazorPage> GetLayoutPage(ViewContext context, string executingFilePath, string layoutPath)
        {
            var layoutPageResult = await _viewLookup.LocateViewAsync(context, layoutPath, executingFilePath, isMainPage: false);

            if (!layoutPageResult.Success)
            {
                var locations = Environment.NewLine + string.Join(Environment.NewLine, layoutPageResult.SearchedLocations);
                throw new InvalidOperationException(Resources.FormatLayoutCannotBeLocated(layoutPath, locations));
            }

            return layoutPageResult.ViewEntry.PageFactory();
        }

        private class View : IView
        {
            public string Path { get; set; }

            public Task RenderAsync(ViewContext context)
            {
                throw new NotSupportedException();
            }
        }
    }
}
