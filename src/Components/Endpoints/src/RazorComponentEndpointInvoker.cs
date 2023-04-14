// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointInvoker
{
    private readonly HttpContext _context;
    private readonly EndpointHtmlRenderer _renderer;
    private readonly Type _rootComponent;
    private readonly Type _componentType;

    public RazorComponentEndpointInvoker(HttpContext context, Type rootComponent, Type componentType)
    {
        _context = context;
        _renderer = _context.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        _rootComponent = rootComponent;
        _componentType = componentType;
    }

    public Task RenderComponent()
    {
        return _renderer.Dispatcher.InvokeAsync(RenderComponentCore);
    }

    private async Task RenderComponentCore()
    {
        _context.Response.ContentType = RazorComponentResultExecutor.DefaultContentType;
        var data = _context.GetRouteData();
        await using var writer = CreateResponseWriter(_context.Response.Body);

        // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
        // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
        // component takes care of switching into your desired render mode when it produces its own output.
        var htmlContent = await _renderer.RenderEndpointComponent(
            _context,
            _rootComponent,
            ParameterView.Empty,
            waitForQuiescence: false);

        // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
        // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
        // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
        // renderer sync context and cause a batch that would get missed.
        htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

        if (!htmlContent.QuiescenceTask.IsCompleted)
        {
            await _renderer.SendStreamingUpdatesAsync(_context, htmlContent.QuiescenceTask, writer);
        }

        // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
