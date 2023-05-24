// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointInvoker
{
    private readonly HttpContext _context;
    private readonly EndpointHtmlRenderer _renderer;
    private readonly Type _rootComponentType;
    private readonly Type _componentType;

    public RazorComponentEndpointInvoker(HttpContext context, Type rootComponentType, Type componentType)
    {
        _context = context;
        _renderer = _context.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        _rootComponentType = rootComponentType;
        _componentType = componentType;
    }

    public Task RenderComponent()
    {
        return _renderer.Dispatcher.InvokeAsync(RenderComponentCore);
    }

    private async Task RenderComponentCore()
    {
        _context.Response.ContentType = RazorComponentResultExecutor.DefaultContentType;

        if (!await TryValidateRequestAsync(out var isPost, out var handler))
        {
            // If the request is not valid we've already set the response to a 400 or similar
            // and we can just exit early.
            return;
        }

        await EndpointHtmlRenderer.InitializeStandardComponentServicesAsync(
            _context,
            componentType: _componentType,
            handler: handler,
            form: handler != null && _context.Request.HasFormContentType ? await _context.Request.ReadFormAsync() : null);

        await using var writer = CreateResponseWriter(_context.Response.Body);

        // Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
        // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
        // component takes care of switching into your desired render mode when it produces its own output.
        var htmlContent = await _renderer.RenderEndpointComponent(
            _context,
            _rootComponentType,
            ParameterView.Empty,
            waitForQuiescence: isPost);

        if (isPost && !_renderer.HasCapturedEvent())
        {
            _context.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        var quiesceTask = isPost ? _renderer.DispatchCapturedEvent() : htmlContent.QuiescenceTask;

        if (isPost)
        {
            await Task.WhenAll(_renderer.NonStreamingPendingTasks);
        }

        // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
        // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
        // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
        // renderer sync context and cause a batch that would get missed.
        htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

        if (!quiesceTask.IsCompleted)
        {
            await _renderer.SendStreamingUpdatesAsync(_context, quiesceTask, writer);
        }

        // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        await writer.FlushAsync();
    }

    private Task<bool> TryValidateRequestAsync(out bool isPost, out string? handler)
    {
        handler = null;
        isPost = HttpMethods.IsPost(_context.Request.Method);
        if (isPost)
        {
            return Task.FromResult(TrySetFormHandler(out handler));
        }

        return Task.FromResult(true);
    }

    private bool TrySetFormHandler([NotNullWhen(true)] out string? handler)
    {
        handler = "";
        if (_context.Request.Query.TryGetValue("handler", out var value))
        {
            if (value.Count != 1)
            {
                _context.Response.StatusCode = StatusCodes.Status400BadRequest;
                handler = null;
                return false;
            }
            else
            {
                handler = value[0]!;
            }
        }

        _renderer.SetFormHandlerName(handler!);
        return true;
    }

    private static TextWriter CreateResponseWriter(Stream bodyStream)
    {
        // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
        const int DefaultBufferSize = 16 * 1024;
        return new HttpResponseStreamWriter(bodyStream, Encoding.UTF8, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
    }
}
